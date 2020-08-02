using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    Rigidbody2D rb;
    public float speed;
    [Range(0.001f, 0.999f)]
    public float brakeSpeed = 0.1f;

    Collider2D[] hits;
    int wallLayerMask = 1 << 9;

    public event Action MazeChange;   

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        hits = new Collider2D[10];
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

        if(Input.GetKeyDown(KeyCode.Space))
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
    }
}