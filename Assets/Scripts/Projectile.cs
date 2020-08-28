using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public Trajectory trajectory;
    public int bounceCount;
    public int[] impactLayers;
    Rigidbody2D rb;
    Vector2 velocity;
    bool hasCollided = false;

    public bool isSimulated = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Launch(ProjectileSO so, Transform player, Vector2 dir)
    {
        var scale = new Vector3(so.width, so.width, 1);
        transform.localScale = scale;
        transform.parent = player;

        bounceCount = so.bounceLimit - 1;

        impactLayers = new int[so.impactLayers.Length];
        Array.Copy(so.impactLayers, impactLayers, so.impactLayers.Length);

        rb.AddForce(dir * so.launchForceMagnitude, ForceMode2D.Impulse);

        velocity = rb.velocity;
    }

    public void Launch(ProjectileSO so, Transform playerCopy, Vector2 dir, Vector3 pos)
    {
        var scale = new Vector3(so.width, so.width, 1);
        transform.localScale = scale;
        transform.position = pos;
        //transform.parent = playerCopy;

        bounceCount = so.bounceLimit - 1;

        impactLayers = new int[so.impactLayers.Length];
        Array.Copy(so.impactLayers, impactLayers, so.impactLayers.Length);

        trajectory.points.Add(pos);

        rb.AddForce(dir * so.launchForceMagnitude, ForceMode2D.Impulse);

        velocity = rb.velocity;

        trajectory.dirs.Add(velocity.normalized);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (velocity != rb.velocity)
        {
            hasCollided = false;
            velocity = rb.velocity;
        }

        if (hasCollided) return;

        hasCollided = true;

        bounceCount--;
        bool impact = false;

        for (int i = 0; i < impactLayers.Length; i++)
        {
            if (collision.gameObject.layer == impactLayers[i])
                impact = true;
        }

        if (isSimulated)
        {
            trajectory.points.Add(rb.position);
            trajectory.dirs.Add(rb.velocity.normalized);
        }

        if (bounceCount <= 0 || impact)
        {
            if(isSimulated)
            {
                transform.position = new Vector3(5000, 5000, 0);
                rb.velocity = Vector2.zero;
                rb.Sleep();
                Debug.Log("simulation terminated");
            }
            else
                Destroy(gameObject);
        }
    }

    //public void Launch(ProjectileSO so, Transform player, Vector2 dir)
    //{
    //    var scale = new Vector3(so.width, so.width, 1);
    //    transform.localScale = scale;

    //    if(!isSimulated)
    //        transform.parent = player;

    //    bounceCount = so.bounceLimit - 1;

    //    impactLayers = new int[so.impactLayers.Length];
    //    Array.Copy(so.impactLayers, impactLayers, so.impactLayers.Length);

    //    rb.AddForce(dir * so.launchForceMagnitude, ForceMode2D.Impulse);

    //    velocity = rb.velocity;
    //}

    //void OnCollisionEnter2D(Collision2D collision)
    //{
    //    if (hasCollided) return;

    //    hasCollided = true;

    //    bounceCount--;
    //    bool impact = false;

    //    for(int i=0; i<impactLayers.Length; i++)
    //    {
    //        if (collision.gameObject.layer == impactLayers[i])
    //            impact = true;
    //    }

    //    if (bounceCount <= 0 || impact)
    //        Destroy(gameObject);
    //}
}
