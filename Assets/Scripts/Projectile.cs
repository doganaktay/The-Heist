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

    public PhysicsSim simulation;
    int lastStepCount;
    int lastFrameCount;

    public bool isSimulated = false;

    // private static dictionary for storing materials with different friction values
    // gets cleared when game is restarted by GameManager
    static Dictionary<PhysMaterial, PhysicsMaterial2D> materialCache = new Dictionary<PhysMaterial, PhysicsMaterial2D>();

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Launch(ProjectileSO so, Vector2 dir, float spin = 0)
    {
        var scale = new Vector3(so.width, so.width, 1);
        transform.localScale = scale;

        bounceCount = so.bounceLimit;

        impactLayers = new int[so.impactLayers.Length];
        Array.Copy(so.impactLayers, impactLayers, so.impactLayers.Length);

        rb.sharedMaterial = GetPhysicsMaterial(so.physicsMaterial);

        rb.velocity = dir * so.launchForceMagnitude;
        rb.angularVelocity = spin;
    }

    public void Launch(ProjectileSO so, Vector2 dir, Vector3 pos, float spin = 0)
    {
        var scale = new Vector3(so.width, so.width, 1);
        transform.localScale = scale;
        transform.position = pos;

        bounceCount = so.bounceLimit;

        impactLayers = new int[so.impactLayers.Length];
        Array.Copy(so.impactLayers, impactLayers, so.impactLayers.Length);

        rb.sharedMaterial = GetPhysicsMaterial(so.physicsMaterial);

        rb.velocity = dir * so.launchForceMagnitude;
        rb.angularVelocity = spin;

        // reset trajectory
        trajectory.width = so.width;
        trajectory.points.Clear();
        trajectory.dirs.Clear();

        trajectory.points.Add(pos);
        trajectory.dirs.Add(dir);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if((isSimulated && lastStepCount == simulation.currentStepCount) || (!isSimulated && lastStepCount == Time.frameCount))
            return;
        else
            lastStepCount = isSimulated? simulation.currentStepCount : Time.frameCount;

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
            if (isSimulated)
            {
                transform.position = new Vector3(5000, 5000, 0);
                rb.velocity = Vector2.zero;
                rb.Sleep();
            }
            else
                Destroy(gameObject);
        }        
    }

    // generate, store and return physics materials as needed with new friction and bounce values
    public static PhysicsMaterial2D GetPhysicsMaterial(PhysMaterial material)
    {
        if (!materialCache.ContainsKey(material))
        {
            materialCache[material] = Instantiate(new PhysicsMaterial2D());
            materialCache[material].friction = material.frictionCoefficient;
            materialCache[material].bounciness = material.bounceCoefficient;
        }

        return materialCache[material];
    }

    public static void ClearMaterialCache()
    {
        materialCache.Clear();
    }
}
