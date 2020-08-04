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
    int lastCellState;
    MazeCell lastCell;
    public MazeCell currentCell;
    public bool cellChanged = false;

    Collider2D[] hits;
    Collider2D previousHit;
    static int wallLayerMask = 1 << 9;
    static int cellLayerMask = 1 << 10;

    public event Action MazeChange;

    public float clampMarginX = 5f;
    public float clampMarginY = 5f;
    float clampX, clampY;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        hits = new Collider2D[10];

        var cam = Camera.main;
        clampY = cam.orthographicSize - clampMarginY;
        clampX = cam.orthographicSize / 9 * 16 - clampMarginX;

    }

    void Update()
    {
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

        TrackLocation();
    }

    void TrackLocation()
    {
        int hitCount = Physics2D.OverlapCircleNonAlloc(transform.position, 3.5f, results: hits, cellLayerMask);

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
            previousHit = hits[0];

        }
    }
    
}