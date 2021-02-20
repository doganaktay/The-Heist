using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ProjectileSelector))]
public class Player : Character
{
    // physics sim creates a copy so we need to keep track of instance count to determine signing up for events
    public static List<Player> instances = new List<Player>();

    public Projectile projectilePrefab;
    ProjectileSelector projectileSelector; // selects current projectile from an array of SOs
    public SoundBomb soundBombPrefab;
    [HideInInspector] public Pathfinder pathfinder;
    [HideInInspector] public PhysicsSim simulation;
    [HideInInspector] public Trajectory trajectory;
    [HideInInspector] public GameObject spawnedObjectHolder;
    
    [HideInInspector] public Maze maze;

    RaycastHit2D[] rayHits;
    // mask currently includes walls, placements and player
    readonly static int projectileLayerMask = 1 << 9 | 1<<8 | 1<<13;

    Vector3 lastPos;
    public Vector3 aimUp;
    float spinAmount = 0;
    public float spinIncrement = 1;
    float lastSpinAmount;

    // input variables
    public bool canDrawTrajectory = false;
    public bool lineReset = true;

    Coroutine currentTask;

    #region MonoBehaviour

    protected override void Start()
    {
        base.Start();

        rayHits = new RaycastHit2D[10];
        aim = transform.GetChild(0).transform;

        projectileSelector = GetComponent<ProjectileSelector>();

        // doing event subscription in start because reference is not available when OnEnable is called
        instances.Add(this);
    }

    protected override void Update()
    {
        base.Update();
    }

    void LateUpdate()
    {
        if (canDrawTrajectory)
        {
            DrawTrajectory();
            canDrawTrajectory = false;
        }
    }

    private void OnDisable()
    {
        if(instances.Contains(this))
            instances.Remove(this);
    }

    protected override void HandleNotification(MazeCell cell, CellNotificationData data)
    {
        if (cell != currentCell)
            return;

        Debug.Log($"{gameObject.name} at {currentCell.pos.x},{currentCell.pos.y} is handling notification with {data.priority} priority, {data.signalStrength} signal strength, centered at {data.signalCenter.gameObject.name}");
    }

    #endregion MonoBehaviour

    #region Projectile

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

        proj.gameObject.transform.SetParent(spawnedObjectHolder.transform);

        proj.Launch(projectileSelector.currentProjectile, aimUp, spinAmount);
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

    #endregion Projectile

    #region Object Placement

    public void PutOrRemoveItem(PlaceableItemType itemType, MazeCell cell)
    {
        if (currentTask != null)
            StopCoroutine(currentTask);
        if (currentMovement != null)
        {
            isMoving = false;
            StopCoroutine(currentMovement);
        }

        switch (itemType)
        {
            case PlaceableItemType.SoundBomb:
                {
                    currentTask = StartCoroutine(ItemTask(PlaceableItemType.SoundBomb, soundBombPrefab, cell));
                }
                break;
        }
    }

    IEnumerator ItemTask(PlaceableItemType itemType, PlaceableItem item, MazeCell targetCell)
    {
        if (targetCell != currentCell)
        {
            Move(targetCell);
        }

        yield return null;

        while (isMoving)
            yield return null;

        PutOrRemove(itemType, item, targetCell);
    }

    void PutOrRemove(PlaceableItemType type, PlaceableItem item, MazeCell cell)
    {
        if (!cell.HasPlacedItem(type))
        {
            var go = Instantiate(item, cell.transform.position, Quaternion.identity);
            go.Place(cell);
            go.transform.SetParent(spawnedObjectHolder.transform);
        }
        else
        {
            Destroy(cell.placedItems[type].gameObject);
            cell.RemoveItem(type);
        }
    }

    public void StopTask()
    {
        if (currentTask != null)
            StopCoroutine(currentTask);
    }

    #endregion Object Placement
}