using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    Rigidbody2D rb;
    Transform playerPos;
    public float speed;
    [Range(0.001f, 0.999f)]
    public float brakeSpeed = 0.1f;
    public int areaIndex = 0;
    int lastAreaIndex;
    public bool hitIndexChanged = false;
    public int cellState;
    public MazeCell currentCell;
    public bool cellChanged = false;

    Collider2D[] hits;
    Collider2D previousHit;
    static int wallLayerMask = 1 << 9;
    static int cellLayerMask = 1 << 10;

    public static event Action MazeChange;

    public float clampMarginX = 5f;
    public float clampMarginY = 5f;
    float clampX, clampY;

    Camera cam;
    Transform aim;
    Vector2 mousePos;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        hits = new Collider2D[10];

        cam = Camera.main;
        clampY = cam.orthographicSize - clampMarginY;
        clampX = cam.orthographicSize / 9 * 16 - clampMarginX;

        aim = transform.GetChild(0).transform;

    }

    void Update()
    {
        // tracking mouse input pos for cell highlighting and player aim
        mousePos = cam.ScreenToWorldPoint(Input.mousePosition);

        float x = Input.GetAxis("Horizontal");
        float y = Input.GetAxis("Vertical");

        Vector2 force = new Vector2(x, y);
        Vector2 velocity = force.normalized * speed * Time.deltaTime;

        rb.AddForce(velocity, ForceMode2D.Impulse);

        if (force.sqrMagnitude < 0.5f)
            rb.velocity = Vector2.Lerp(rb.velocity, Vector2.zero, brakeSpeed);

        // clamp to screen
        float posX = Mathf.Clamp(rb.position.x, -clampX, clampX);
        float posY = Mathf.Clamp(rb.position.y, -clampY, clampY);

        transform.position = new Vector2(posX, posY);

        if (Input.GetKeyDown(KeyCode.Space))
        {
            int hitCount = Physics2D.OverlapCircleNonAlloc(transform.position, 3.5f, results: hits, wallLayerMask);

            if (hitCount == 2)
            {
                for (int i = 0; i < hitCount; i++)
                {
                    hits[i].GetComponentInParent<MazeCellWall>().RemoveWall();
                }

                MazeChange();
            }
        }

        TrackMouseLocation();
        FaceMouse();
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

    void TrackMouseLocation()
    {
        int hitCount = Physics2D.OverlapCircleNonAlloc(mousePos, 1f, results: hits, cellLayerMask);
        //int hitCount = Physics2D.OverlapCircleNonAlloc(transform.position, 1f, results: hits, cellLayerMask);

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