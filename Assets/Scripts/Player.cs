﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public Projectile projectilePrefab;
    public ProjectileSO[] projectileSOs;
    ProjectileSO currentProjectileSO; // currently being set manually to the first one in the array in start

    public PhysicsSim simulation;
    public Trajectory trajectory;
    Rigidbody2D rb;
    public float speed;
    [Range(0.001f, 0.999f)]
    public float brakeSpeed = 0.1f;
    public int areaIndex = 0;
    int lastAreaIndex;
    public bool hitIndexChanged = false;
    public int cellState;
    public MazeCell currentCell;
    public bool cellChanged = false;
    public Maze maze;

    Collider2D[] hits;
    Collider2D previousHit;
    RaycastHit2D[] rayHits;
    // mask currently includes walls and player
    static int projectileLayerMask = 1 << 9 | 1<<8;
    static int wallLayerMask = 1 << 9;
    static int cellLayerMask = 1 << 10;

    public static event Action MazeChange;

    public float clampMarginX = 5f;
    public float clampMarginY = 5f;
    float clampX, clampY;

    Camera cam;
    Transform aim;
    Vector2 mousePos;
    Vector2 lastMousePos;
    Vector3 lastPos;
    Vector3 aimUp;
    float spinAmount = 0;
    public float spinIncrement = 1;
    float lastSpinAmount;

    // input variables
    Vector2 force;
    Vector2 velocity;
    bool canDrawTrajectory = false;

    bool lineReset = true;
    [SerializeField]
    int maxLinePoints = 20;
    [SerializeField]
    float projectileWidth = 3f;
    [SerializeField]
    float zOffset = -5f;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        hits = new Collider2D[10];
        rayHits = new RaycastHit2D[10];

        cam = Camera.main;
        clampY = cam.orthographicSize - clampMarginY;
        clampX = cam.orthographicSize / 9 * 16 - clampMarginX;

        aim = transform.GetChild(0).transform;

        currentProjectileSO = projectileSOs[0];
    }

    void FixedUpdate()
    {
        rb.AddForce(velocity, ForceMode2D.Impulse);

        if (force.sqrMagnitude < 0.5f)
            rb.velocity = Vector2.Lerp(rb.velocity, Vector2.zero, brakeSpeed);

        if (canDrawTrajectory)
        {
            DrawTrajectory();
            canDrawTrajectory = false;
        }
    }

    void Update()
    {
        // tracking mouse input pos for cell highlighting and player aim
        mousePos = cam.ScreenToWorldPoint(Input.mousePosition);

        TrackMouseLocation();
        FaceMouse();

        if (Input.GetMouseButton(0))
        {
            force = Vector2.zero;

            if (Input.GetKey(KeyCode.Q) && spinAmount > -currentProjectileSO.maxLaunchSpin)
            {
                spinAmount -= spinIncrement;
            }
            else if (Input.GetKey(KeyCode.E) && spinAmount < currentProjectileSO.maxLaunchSpin)
            {
                spinAmount += spinIncrement;
            }

            // only redraw line if some condition changes
            if (lastMousePos != mousePos || lastPos != transform.position || spinAmount != lastSpinAmount || lineReset)
            {
                lineReset = false;
                lastMousePos = mousePos;
                lastPos = transform.position;
                lastSpinAmount = spinAmount;
                aimUp = aim.transform.up;

                canDrawTrajectory = true;
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                LaunchProjectile();
                //spinAmount = 0;
            }

            // returning because we don't want player movement when aiming projectile with trajectory
            // this should become a conditional depending on whether projectile type displays predetermined trajectory
            return;
        }
        else if (!lineReset)
        {
            trajectory.sharedMesh.Clear();
            lineReset = true;
            spinAmount = 0;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            var hitCount = Physics2D.RaycastNonAlloc(transform.position, aim.transform.up, results: rayHits, 5f, wallLayerMask);

            if (hitCount > 0)
            {
                for (int i = 0; i < hitCount; i++)
                {
                    var wall = rayHits[i].transform.GetComponentInParent<MazeCellWall>();
                    wall.RemoveWall();
                    maze.wallsInScene.Remove(wall);
                    simulation.RemoveWallFromSimulation(rayHits[i].collider.transform.parent.gameObject);
                }

                MazeChange();
            }
        }

        // movement inputs
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");

        force = new Vector2(x, y);
        velocity = force.normalized * speed;

        // clamp to screen
        float posX = Mathf.Clamp(rb.position.x, -clampX, clampX);
        float posY = Mathf.Clamp(rb.position.y, -clampY, clampY);

        transform.position = new Vector3(posX, posY, -3.5f);
    }

    void LaunchProjectile()
    {
        var proj = Instantiate(projectilePrefab, transform.position + aimUp * (transform.localScale.x / 2f * 1.1f + projectileWidth / 2f),
            Quaternion.identity);
        proj.GetComponent<Projectile>().Launch(projectileSOs[0], aimUp, spinAmount);
    }

    void DrawTrajectory()
    {
        Vector2 ro = transform.position + aimUp * (transform.localScale.x / 2f * 1.1f + currentProjectileSO.width / 2f);

        Physics2D.CircleCastNonAlloc(ro, currentProjectileSO.width / 2f, aimUp, results: rayHits, 500f, projectileLayerMask);
        bool wallTooClose = (rayHits[0].point - ro).sqrMagnitude < currentProjectileSO.width * currentProjectileSO.width;

        if (wallTooClose)
        { trajectory.sharedMesh.Clear(); return; }

        // the simulated projectile fills the trajectory lists for positions and directions
        simulation.SimulateProjectile(projectileSOs[0], aimUp, ro, spinAmount);

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
        int hitCount = Physics2D.OverlapCircleNonAlloc(mousePos, 1f, results: hits, cellLayerMask);

        if (hitCount > 0)
        {
            float dist = Mathf.Infinity;
            int closestIndex = 0;
            for(int i = 0; i < hitCount; i++)
            {
                var temp = Vector2.Distance(hits[i].transform.position, transform.position);
                dist = temp < dist ? temp : dist;
                if(temp < dist)
                {
                    dist = temp;
                    closestIndex = i;
                }
            }

            if (hits[closestIndex] == previousHit) { return; }

            currentCell = hits[closestIndex].GetComponent<MazeCell>();
            cellChanged = true;

            cellState = currentCell.state;
            areaIndex = currentCell.areaIndex;

            if (lastAreaIndex != areaIndex)
                hitIndexChanged = true;

            lastAreaIndex = areaIndex;
            previousHit = hits[closestIndex];

        }
    }
    
}