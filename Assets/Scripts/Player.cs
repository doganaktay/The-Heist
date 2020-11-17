using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ProjectileSelector))]
public class Player : MonoBehaviour
{
    // physics sim creates a copy so we need to keep track of instance count to determine signing up for events
    public static List<Player> instances = new List<Player>();

    public Projectile projectilePrefab;
    ProjectileSelector projectileSelector; // selects current projectile from an array of SOs
    public SoundBomb soundBombPrefab;
    public TouchUI touchUI;
    public Pathfinder pathfinder;
    public PhysicsSim simulation;
    public Trajectory trajectory;
    Rigidbody2D rb;
    public float walkSpeed = 1f;
    public float runSpeed = 5f;
    [Range(0.001f, 0.999f)]
    public float brakeSpeed = 0.1f;
    public int areaIndex = 0;
    public bool hitIndexChanged = false;
    public int cellState;
    
    public bool cellChanged = false;
    public Maze maze;

    Collider2D[] posHits;
    Collider2D previousHit;
    RaycastHit2D[] rayHits;
    // mask currently includes walls, placements and player
    static int projectileLayerMask = 1 << 9 | 1<<8 | 1<<13;
    static int cellLayerMask = 1 << 10;

    bool isMoving = false;
    MazeCell nextCell;
    float turnSpeed = 0.5f;
    float bufferDistance = 0.01f;
    Coroutine currentMovement;
    //List<MazeCell> currentPath = new List<MazeCell>();
    public MazeCell currentPlayerCell;
    public MazeCell lastPlayerCell;

    public static event Action MazeChange;

    public Camera cam;
    public Transform aim;
    Vector3 lastPos;
    public Vector3 aimUp;
    float spinAmount = 0;
    public float spinIncrement = 1;
    float lastSpinAmount;

    // input variables
    public bool canDrawTrajectory = false;
    public bool lineReset = true;

    Coroutine currentTask;
    public bool ShouldRun { get; set; }

    #region MonoBehaviour

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        posHits = new Collider2D[10];
        rayHits = new RaycastHit2D[10];

        cam = Camera.main;
        aim = transform.GetChild(0).transform;

        projectileSelector = GetComponent<ProjectileSelector>();

        // doing event subscription in start because reference is not available when OnEnable is called
        instances.Add(this);
        if (instances.Count > 0 && instances[0] == this)
        {
            TouchUI.PlaceOrRemoveItem += PutOrRemoveItem;
        }
    }

    void Update()
    {
        TrackPosition();
    }

    void LateUpdate()
    {
        if (canDrawTrajectory)
        {
            DrawTrajectory();
            canDrawTrajectory = false;
        }
    }

    private void OnDisable()
    {
        if(instances.Count > 0 && instances[0] == this)
        {
            TouchUI.PlaceOrRemoveItem -= PutOrRemoveItem;
        }

        if(instances.Contains(this))
            instances.Remove(this);
    }

    #endregion MonoBehaviour

    #region Projectile

    public void SetTrajectory()
    {
        lineReset = false;
        projectileSelector.selectionChanged = false;
        lastPos = transform.position;
        lastSpinAmount = spinAmount;
        aimUp = aim.transform.up;

        canDrawTrajectory = true;
    }

    public void ResetTrajectory()
    {
        canDrawTrajectory = false;
        trajectory.sharedMesh.Clear();
        lineReset = true;
    }

    public void LaunchProjectile()
    {
        var proj = Instantiate(projectilePrefab, transform.position + aimUp * (transform.localScale.x / 2f * 1.1f + projectileSelector.currentProjectile.width / 2f),
            Quaternion.identity);
        proj.GetComponent<Projectile>().Launch(projectileSelector.currentProjectile, aimUp, spinAmount);
    }

    void DrawTrajectory()
    {
        Vector2 ro = transform.position + aimUp * (transform.localScale.x / 2f * 1.1f + projectileSelector.currentProjectile.width / 2f);

        Physics2D.CircleCastNonAlloc(ro, projectileSelector.currentProjectile.width / 2f, aimUp, results: rayHits, 500f, projectileLayerMask);
        bool wallTooClose = (rayHits[0].point - ro).sqrMagnitude < projectileSelector.currentProjectile.width * projectileSelector.currentProjectile.width;

        if (wallTooClose)
        {
            trajectory.sharedMesh.Clear();
            return;
        }

        // the simulated projectile fills the trajectory lists for positions and directions
        simulation.SimulateProjectile(projectileSelector.currentProjectile, aimUp, ro, spinAmount);

        trajectory.DrawTrajectory();
    }

    #endregion Projectile

    #region Object Placement

    public void PutOrRemoveItem(PlaceableItemType itemType, MazeCell cell)
    {
        if (currentTask != null)
            StopCoroutine(currentTask);
        if (currentMovement != null)
        {
            isMoving = false;
            StopCoroutine(currentMovement);
        }

        switch (itemType)
        {
            case PlaceableItemType.SoundBomb:
                {
                    currentTask = StartCoroutine(ItemTask(PlaceableItemType.SoundBomb, soundBombPrefab, cell));
                }
                break;
        }
    }

    IEnumerator ItemTask(PlaceableItemType itemType, PlaceableItem item, MazeCell targetCell)
    {
        if (targetCell != currentPlayerCell)
        {
            Move(targetCell);
        }

        yield return null;

        while (isMoving)
            yield return null;

        PutOrRemove(itemType, item, targetCell);
    }

    void PutOrRemove(PlaceableItemType type, PlaceableItem item, MazeCell cell)
    {
        if (!cell.HasPlacedItem(type))
        {
            var go = Instantiate(item, cell.transform.position, Quaternion.identity);

            if (!cell.placedItems.ContainsKey(type))
                cell.PlaceItem(type, go);
            else
                cell.placedItems[type] = go;
        }
        else
        {
            Destroy(cell.placedItems[type].gameObject);
            cell.RemoveItem(type);
        }
    }

    public void StopTask()
    {
        if (currentTask != null)
            StopCoroutine(currentTask);
    }

    #endregion Object Placement

    void TrackPosition()
    {
        //if (currentMovement != null)
        //    return;

        int hitCount = Physics2D.OverlapCircleNonAlloc(transform.position, 1f, results: posHits, cellLayerMask);

        if (hitCount > 0)
        {

            float dist = Mathf.Infinity;
            int closestIndex = 0;
            for (int i = 0; i < hitCount; i++)
            {
                var temp = Vector2.Distance(posHits[i].transform.position, transform.position);
                if (temp < dist)
                {
                    dist = temp;
                    closestIndex = i;
                }
            }

            if (posHits[closestIndex] == previousHit) { return; }

            lastPlayerCell = currentPlayerCell;
            currentPlayerCell = posHits[closestIndex].GetComponent<MazeCell>();
        }
    }

    #region Movement

    public bool IsMoving { get { return isMoving; } }

    public void Move(MazeCell targetCell)
    {
        PathRequestManager.RequestPath(new PathRequest(OnPathFound, currentPlayerCell, targetCell));
    }

    public void OnPathFound(List<MazeCell> path)
    {
        if (path[path.Count - 1] == currentPlayerCell)
            return;

        if (!ShouldRun)
            RestartGoToDestination(path, walkSpeed);
        else
            RestartGoToDestination(path, runSpeed);

        touchUI.TouchPoint(path[path.Count - 1].transform.position);
    }

    

    void RestartGoToDestination(List<MazeCell> path, float speed)
    {
        StopGoToDestination();
        currentMovement = StartCoroutine(GoToDestination(path, speed));
    }

    public void StopGoToDestination()
    {
        if (currentMovement != null)
        {
            isMoving = false;
            StopCoroutine(currentMovement);
        }
    }

    IEnumerator GoToDestination(List<MazeCell> path, float speed)
    {
        isMoving = true;

        for(int j = 0; j < path.Count - 1; j++)
        {
            Debug.DrawLine(path[j].transform.position, path[j+1].transform.position, Color.red, 5f);
        }

        // starting at 1 because index 0 is the cell it is already on
        int i = path[0] == currentPlayerCell ? 1 : 0;
        while (i < path.Count)
        {
            nextCell = path[i];
            aim.LookAt2D(nextCell.transform, turnSpeed);
            transform.position = Vector2.MoveTowards(transform.position, nextCell.transform.position, speed * Time.deltaTime);

            if (transform.position == nextCell.transform.position)
            {
                //currentPlayerCell = nextCell;
                i++;
            }

            Debug.DrawLine(transform.position, nextCell.transform.position, Color.blue, 5f);

            yield return null;
        }

        isMoving = false;
    }

    #endregion Movement
}