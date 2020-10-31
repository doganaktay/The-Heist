using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ProjectileSelector))]
public class Player : MonoBehaviour
{
    public Projectile projectilePrefab;
    ProjectileSelector projectileSelector; // selects current projectile from an array of SOs
    public Pathfinder pathfinder;
    public PhysicsSim simulation;
    public Trajectory trajectory;
    Rigidbody2D rb;
    public float walkSpeed = 1f;
    public float runSpeed = 5f;
    [Range(0.001f, 0.999f)]
    public float brakeSpeed = 0.1f;
    public int areaIndex = 0;
    int lastAreaIndex;
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
    Coroutine currentAction;
    List<MazeCell> currentPath = new List<MazeCell>();
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

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        posHits = new Collider2D[10];
        rayHits = new RaycastHit2D[10];

        cam = Camera.main;
        aim = transform.GetChild(0).transform;

        projectileSelector = GetComponent<ProjectileSelector>();
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

    void TrackPosition()
    {
        if (currentAction != null)
            return;

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

    public MazeCell CurrentPlayerCell { get { return currentPlayerCell; } }

    public bool IsMoving { get { return isMoving; } }

    public void Move(MazeCell destination, bool run = false)
    {
        currentPath = pathfinder.GetAStarPath(currentPlayerCell, destination);

        if (!run)
            RestartGoToDestination(currentPath, walkSpeed);
        else
            RestartGoToDestination(currentPath, runSpeed);
    }

    void RestartGoToDestination(List<MazeCell> path, float speed)
    {
        StopGoToDestination();
        currentAction = StartCoroutine(GoToDestination(path, speed));
    }

    public void StopGoToDestination()
    {
        if (currentAction != null)
        {
            isMoving = false;
            StopCoroutine(currentAction);
        }
    }

    IEnumerator GoToDestination(List<MazeCell> path, float speed)
    {
        isMoving = true;

        // starting at 1 because index 0 is the cell it is already on
        int i = 1;
        while (i < path.Count)
        {
            nextCell = path[i];
            aim.LookAt2D(nextCell.transform, turnSpeed);
            transform.position = Vector2.MoveTowards(transform.position, nextCell.transform.position, speed * Time.deltaTime);

            if (((Vector2)nextCell.transform.position - (Vector2)transform.position).magnitude < bufferDistance)
            {
                currentPlayerCell = nextCell;
                i++;
            }

            yield return null;
        }

        isMoving = false;
    }
}