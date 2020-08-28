﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public Projectile projectilePrefab;
    public ProjectileSO[] projectileSOs;

    public PhysicsSim simulation;
    Trajectory trajectory;
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

        trajectory = FindObjectOfType<Trajectory>();

    }

    void Update()
    {
        // tracking mouse input pos for cell highlighting and player aim
        mousePos = cam.ScreenToWorldPoint(Input.mousePosition);

        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");

        Vector2 force = new Vector2(x, y);
        Vector2 velocity = force.normalized * speed * Time.deltaTime;

        rb.AddForce(velocity, ForceMode2D.Impulse);
        //rb.velocity = velocity;

        if (force.sqrMagnitude < 0.5f)
            rb.velocity = Vector2.Lerp(rb.velocity, Vector2.zero, brakeSpeed);

        // clamp to screen
        float posX = Mathf.Clamp(rb.position.x, -clampX, clampX);
        float posY = Mathf.Clamp(rb.position.y, -clampY, clampY);

        transform.position = new Vector3(posX, posY, -3.5f);

        if (Input.GetMouseButton(0))
        {
            // only draw line again if mouse position or player has moved
            if (lastMousePos != mousePos || lastPos != transform.position)
            {
                DrawTrajectory(true);
                lineReset = false;
                lastMousePos = mousePos;
                lastPos = transform.position;
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                LaunchProjectile();
            }
        }
        else if (!lineReset)
        {
            DrawTrajectory(false);
            lineReset = true;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            var hitCount = Physics2D.RaycastNonAlloc(transform.position, aim.transform.up, results: rayHits, 5f, wallLayerMask);

            if (hitCount == 2)
            {
                for (int i = 0; i < hitCount; i++)
                {
                    rayHits[i].transform.GetComponentInParent<MazeCellWall>().RemoveWall();
                    maze.wallsInScene.Remove(rayHits[i].transform.GetComponentInParent<MazeCellWall>());
                    simulation.RemoveWallFromSimulation(rayHits[i].collider.transform.parent.gameObject);
                }

                MazeChange();
            }
        }

        TrackMouseLocation();
        FaceMouse();
    }

    void LaunchProjectile()
    {
        var proj = Instantiate(projectilePrefab, transform.position + aim.transform.up * (transform.localScale.x / 2f * 1.1f + projectileWidth / 2f),
            Quaternion.identity);
        proj.GetComponent<Projectile>().Launch(projectileSOs[0], transform, aim.transform.up);
    }

    void DrawTrajectory(bool draw)
    {
        if (draw)
        {
            Vector2 ro = transform.position + aim.transform.up * (transform.localScale.x / 2f * 1.1f + projectileWidth / 2f);
            Vector2 rd = aim.transform.up;

            trajectory.width = projectileWidth;
            trajectory.points.Clear();
            trajectory.dirs.Clear();

            Physics2D.CircleCastNonAlloc(ro, projectileWidth / 2f, rd, results: rayHits, 500f, projectileLayerMask);
            bool wallTooClose = Vector2.Distance(ro, rayHits[0].point) < projectileWidth;

            if (wallTooClose)
            { trajectory.sharedMesh.Clear(); return; }

            // the simulated projectile fills the trajectory lists for positions and directions
            var projPos = transform.position + aim.transform.up * (transform.localScale.x / 2f * 1.1f + projectileWidth / 2f);
            simulation.SimulateProjectile(projectileSOs[0], aim.transform.up, projPos);

            trajectory.DrawTrajectory();
            
        }
        else
            trajectory.sharedMesh.Clear();

    }

    // sends necessary variables and gets Trajectory class to draw a procedural mesh for projectile
    //void DrawTrajectory(bool draw)
    //{
    //    if (draw)
    //    {
    //        Vector2 ro = transform.position + aim.transform.up * (transform.localScale.x / 2f * 1.1f + projectileWidth / 2f);
    //        Vector2 rd = aim.transform.up;

    //        trajectory.width = projectileWidth;
    //        trajectory.points.Clear();
    //        trajectory.dirs.Clear();

    //        bool wallTooClose = false;

    //        for (int i = 1; i < maxLinePoints; i++)
    //        {
    //            var hitCount = Physics2D.CircleCastNonAlloc(ro, projectileWidth / 2f, rd, results: rayHits, 500f, projectileLayerMask);

    //            wallTooClose = Vector2.Distance(ro, rayHits[0].point) < projectileWidth;

    //            if (hitCount == 0 || wallTooClose)
    //                break;

    //            if (i == 1)
    //            {
    //                trajectory.points.Add(new Vector3(ro.x, ro.y, zOffset));
    //                trajectory.dirs.Add(new Vector3(rd.x, rd.y, 0));
    //            }

    //            var surfaceNormal = rayHits[0].normal;
    //            var dot = Vector2.Dot(rd, surfaceNormal);

    //            ro = ro + rd * rayHits[0].distance + surfaceNormal * 0.005f;
    //            rd = rd - 2f * dot * surfaceNormal;

    //            trajectory.points.Add(new Vector3(ro.x, ro.y, zOffset));
    //            trajectory.dirs.Add(new Vector3(rd.x, rd.y, 0));

    //            // 9 is the wall layer
    //            if (rayHits[0].collider.gameObject.layer != 9)
    //                break;
    //        }

    //        if (!wallTooClose)
    //            trajectory.DrawTrajectory();
    //        else
    //            trajectory.sharedMesh.Clear();
    //    }
    //    else
    //        trajectory.sharedMesh.Clear();

    //}

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