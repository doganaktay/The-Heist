﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Character : MonoBehaviour
{
    protected MazeCell currentCell, lastCell;
    public MazeCell CurrentCell { get => currentCell; private set => currentCell = value; }
    public MazeCell LastCell { get => lastCell; private set => lastCell = value; }
    readonly static int cellLayerMask = 1 << 10;
    protected Collider2D[] posHits;
    protected Collider2D previousHit;
    [SerializeField] protected bool isOnGrid = true;
    private bool hasChanged = false;

    [SerializeField]
    protected MinMaxData speed;
    protected float currentSpeed;
    [SerializeField]
    protected float turnSpeed = 1f;
    public bool ShouldRun { get; set; }
    protected bool isMoving = false;
    protected MazeCell nextCell;
    protected Coroutine currentMovement;
    [HideInInspector] public Transform aim; // used if Character has aim for LookAt
    [HideInInspector] public bool AimOverride { get; set; } = false;
    [HideInInspector] public Transform aimOverrideTarget;

    public Action PositionChange;

    protected virtual void Start()
    {
        posHits = new Collider2D[10];
    }
    
    protected virtual void Update()
    {
        TrackPosition();
        if (isOnGrid && hasChanged)
            ManageCallbacks();

        if (AimOverride)
            transform.LookAt2D(aimOverrideTarget, turnSpeed * 2f);
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

            previousHit = posHits[closestIndex];
            lastCell = currentCell;
            currentCell = posHits[closestIndex].GetComponent<MazeCell>();
            hasChanged = true;
        }
    }

    void ManageCallbacks()
    {
        if(lastCell != null)
            NotificationModule.RemoveListener(lastCell.pos, HandleNotification);
        if(currentCell != null)
            NotificationModule.AddListener(currentCell.pos, HandleNotification);

        PositionChange?.Invoke();

        hasChanged = false;
    }

    protected virtual void HandleNotification(CellNotificationData data)
    {
        Debug.Log($"{gameObject.name} at {currentCell.pos.x},{currentCell.pos.y} is handling notification with {data.priority} priority, {data.signalStrength} signal strength, centered at {data.signalCenter.gameObject.name}");
    }

    #region Movement

    public bool IsMoving { get => isMoving; }

    public void Move(MazeCell targetCell, PathLayer pathLayer = PathLayer.Base)
    {
        PathRequestManager.RequestPath(new PathRequest(OnPathFound, pathLayer, currentCell, targetCell));
    }

    public void Move(PathLayer pathLayer = PathLayer.Base)
    {
        PathRequestManager.RequestPath(new PathRequest(OnPathFound, pathLayer, currentCell));
    }

    public void OnPathFound(List<MazeCell> path)
    {
        if (path[path.Count - 1] == currentCell)
            return;

        if (!ShouldRun)
            RestartGoToDestination(path, speed.min);
        else
            RestartGoToDestination(path, speed.max);
    }

    void RestartGoToDestination(List<MazeCell> path, float speed)
    {
        StopGoToDestination();
        currentMovement = StartCoroutine(GoToDestination(path, speed));
    }

    public void StopGoToDestination()
    {
        if (currentMovement != null)
        {
            isMoving = false;
            StopCoroutine(currentMovement);
        }
    }

    IEnumerator GoToDestination(List<MazeCell> path, float speed)
    {
        isMoving = true;

#if UNITY_EDITOR
        for (int j = 0; j < path.Count - 1; j++)
        {
            Debug.DrawLine(path[j].transform.position, path[j + 1].transform.position, Color.red, 5f);
        }
#endif

        int i = path[0] == currentCell ? 1 : 0;
        while (i < path.Count)
        {
            nextCell = path[i];

            if (!AimOverride)
            {
                if(aim != null)
                    aim.LookAt2D(nextCell.transform, turnSpeed);
                else
                    transform.LookAt2D(nextCell.transform, turnSpeed);
            }

            transform.position = Vector2.MoveTowards(transform.position, nextCell.transform.position, speed * Time.deltaTime);

            if (transform.position == nextCell.transform.position)
                i++;

#if UNITY_EDITOR
            Debug.DrawLine(transform.position, nextCell.transform.position, Color.blue, 5f);
#endif

            yield return null;
        }

        isMoving = false;
    }

    #endregion Movement
}
