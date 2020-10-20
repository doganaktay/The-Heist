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
    Collider2D[] touchHits;
    Collider2D previousHit;
    RaycastHit2D[] rayHits;
    // mask currently includes walls, placements and player
    static int projectileLayerMask = 1 << 9 | 1<<8 | 1<<13;
    static int wallLayerMask = 1 << 9;
    static int cellLayerMask = 1 << 10;

    public LayerMask touchMask;
    bool moving = false;
    MazeCell lastCellHit;
    MazeCell currentCellHit;
    MazeCell nextCell;
    float turnSpeed = 0.5f;
    float bufferDistance = 0.01f;
    Coroutine currentAction;
    Coroutine doubleTapRoutine;
    public float doubleTapDelay = 0.2f;
    int tapCount;
    const float DoubleTapTime = 0.2f;
    float lastTapTime;
    List<MazeCell> currentPath = new List<MazeCell>();
    public MazeCell currentPlayerCell;
    public MazeCell lastPlayerCell;

    public static event Action MazeChange;

    Camera cam;
    Transform aim;
    Vector2 mousePos;
    Vector2 lastMousePos;
    Vector3 lastPos;
    Vector3 aimUp;
    float spinAmount = 0;
    public float spinIncrement = 1;
    float lastSpinAmount;

    public struct TouchInfo
    {
        public Touch touch;
        public Vector3 worldPos;
        public int id;

        public TouchInfo(Touch touch, Vector3 worldPos, int id)
        {
            this.touch = touch;
            this.worldPos = worldPos;
            this.id = id;
        }
    }

    TouchInfo[] touches;

    // input variables
    bool canDrawTrajectory = false;

    bool lineReset = true;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        posHits = new Collider2D[10];
        touchHits = new Collider2D[10];
        rayHits = new RaycastHit2D[10];

        cam = Camera.main;
        aim = transform.GetChild(0).transform;

        projectileSelector = GetComponent<ProjectileSelector>();

        // initialise touch info struct array with max allowed finger count
        touches = new TouchInfo[5];
    }

    void Update()
    {
        TrackPosition();
        ProcessTouchInput();

        //// tracking mouse input pos for cell highlighting and player aim
        //mousePos = cam.ScreenToWorldPoint(Input.mousePosition);

        //TrackPosition();
        //TrackMouseLocation();
        //FaceMouse();

        //if (Input.GetMouseButton(0))
        //{
        //    if (Input.GetKey(KeyCode.Q) && spinAmount > -projectileSelector.currentProjectile.maxLaunchSpin)
        //    {
        //        spinAmount -= spinIncrement;
        //    }
        //    else if (Input.GetKey(KeyCode.E) && spinAmount < projectileSelector.currentProjectile.maxLaunchSpin)
        //    {
        //        spinAmount += spinIncrement;
        //    }

        //    // only redraw line if some condition changes
        //    if (lastMousePos != mousePos || (transform.position - lastPos).sqrMagnitude > 0.1f * 0.1f || spinAmount != lastSpinAmount
        //        || projectileSelector.selectionChanged || lineReset)
        //    {
        //        lineReset = false;
        //        projectileSelector.selectionChanged = false;
        //        lastMousePos = mousePos;
        //        lastPos = transform.position;
        //        lastSpinAmount = spinAmount;
        //        aimUp = aim.transform.up;

        //        canDrawTrajectory = true;
        //    }

        //    if (Input.GetKeyDown(KeyCode.Space))
        //    {
        //        LaunchProjectile();
        //        //spinAmount = 0;
        //    }

        //    // returning because we don't want player movement when aiming projectile with trajectory
        //    // this should become a conditional depending on whether projectile type displays predetermined trajectory
        //    return;
        //}
        //else if (!lineReset)
        //{
        //    trajectory.sharedMesh.Clear();
        //    lineReset = true;
        //    spinAmount = 0;
        //}
    }

    private void ProcessTouchInput()
    {
        if (Input.touchCount > 0)
        {
            var count = Input.touchCount;

            for (int i = 0; i < count; i++)
            {
                var touch = Input.GetTouch(i);
                touches[i] = new TouchInfo(touch, cam.ScreenToWorldPoint(touch.position), touch.fingerId);
            }

            if (count == 1)
            {
                if (Physics2D.OverlapCircleNonAlloc(touches[0].worldPos, 1f, results: touchHits, cellLayerMask) > 0)
                {
                    currentCellHit = touchHits[0].GetComponent<MazeCell>();

                    if (touches[0].touch.phase == TouchPhase.Ended)
                    {
                        var timeSinceLastTap = Time.time - lastTapTime;

                        if (currentCellHit != currentPlayerCell && currentCellHit.state < 2)
                        {
                            currentPath = pathfinder.GetAStarPath(currentPlayerCell, currentCellHit);

                            if (currentCellHit != lastCellHit)
                            {
                                tapCount = 1;
                                lastCellHit = currentCellHit;
                            }
                            else
                                tapCount++;

                            if(tapCount == 1)
                            {
                                RestartGoToDestination(currentPath, walkSpeed);
                            }
                            else
                            {
                                if (timeSinceLastTap <= DoubleTapTime)
                                    RestartGoToDestination(currentPath, runSpeed);
                                else
                                    RestartGoToDestination(currentPath, walkSpeed);
                            }
                        }

                        lastTapTime = Time.time;
                    }
                }

            }
        }
    }

    void LateUpdate()
    {
        if (canDrawTrajectory)
        {
            DrawTrajectory();
            canDrawTrajectory = false;
        }
    }

    void LaunchProjectile()
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
        { trajectory.sharedMesh.Clear(); return; }

        // the simulated projectile fills the trajectory lists for positions and directions
        simulation.SimulateProjectile(projectileSelector.currentProjectile, aimUp, ro, spinAmount);

        trajectory.DrawTrajectory();
    }

    // a holder object for the aim of equal scale with player parent object is rotated
    // the aim itself is a child of this aim holder so as the holder rotates on its own axis
    // the aim rotates around the circle
    void FaceMouse()
    {
        Vector2 diff = mousePos - (Vector2)transform.position;
        diff = diff.normalized;
        float rot = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
        aim.transform.localRotation = Quaternion.Euler(0f, 0f, rot-90f);
    }

    // track what cell the mouse is currently over
    // and set bools used by other scripts for updating
    void TrackMouseLocation()
    {
        int hitCount = Physics2D.OverlapCircleNonAlloc(mousePos, 1f, results: touchHits, cellLayerMask);

        if (hitCount > 0)
        {
            float dist = Mathf.Infinity;
            int closestIndex = 0;
            for(int i = 0; i < hitCount; i++)
            {
                var temp = Vector2.Distance(touchHits[i].transform.position, transform.position);
                dist = temp < dist ? temp : dist;
                if(temp < dist)
                {
                    dist = temp;
                    closestIndex = i;
                }
            }

            if (touchHits[closestIndex] == previousHit) { return; }

            currentPlayerCell = touchHits[closestIndex].GetComponent<MazeCell>();
            cellChanged = true;

            cellState = currentPlayerCell.state;
            areaIndex = currentPlayerCell.areaIndex;

            if (lastAreaIndex != areaIndex)
                hitIndexChanged = true;

            lastAreaIndex = areaIndex;
            previousHit = touchHits[closestIndex];

        }
    }

    void TrackPosition()
    {
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

    void RestartGoToDestination(List<MazeCell> path, float speed)
    {
        if (moving)
        { moving = false; StopCoroutine(currentAction); }
        moving = true;

        currentAction = StartCoroutine(GoToDestination(path, speed));
    }

    IEnumerator GoToDestination(List<MazeCell> path, float speed)
    {
        // starting at 1 because index 0 is the cell it is already on
        int i = 1;
        while (i < path.Count && moving)
        {
            nextCell = path[i];
            transform.LookAt2D(nextCell.transform, turnSpeed);
            transform.position = Vector2.MoveTowards(transform.position, nextCell.transform.position, speed * Time.deltaTime);

            if (((Vector2)nextCell.transform.position - (Vector2)transform.position).magnitude < bufferDistance)
            {
                currentPlayerCell = nextCell;
                i++;
            }

            yield return null;
        }

        moving = false;
    }
}