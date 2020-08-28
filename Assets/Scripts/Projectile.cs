using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public int bounceCount;
    public int[] impactLayers;
    Rigidbody2D rb;
    Vector2 velocity;
    bool hasCollided = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if(velocity != rb.velocity)
        {
            hasCollided = false;
            velocity = rb.velocity;
        }
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

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasCollided) return;

        hasCollided = true;

        bounceCount--;
        bool impact = false;

        Debug.DrawRay(collision.GetContact(0).point, collision.GetContact(0).normal, Color.blue, 10f);

        for(int i=0; i<impactLayers.Length; i++)
        {
            if (collision.gameObject.layer == impactLayers[i])
                impact = true;
        }

        if (bounceCount <= 0 || impact)
            Destroy(gameObject);
    }
}
