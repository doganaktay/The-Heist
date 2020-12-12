using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Character : MonoBehaviour
{
    protected MazeCell currentCell, lastCell;
    public MazeCell CurrentCell { get => currentCell; private set => currentCell = value; }
    public MazeCell LastCell { get => lastCell; private set => lastCell = value; }
    static int cellLayerMask = 1 << 10;
    protected Collider2D[] posHits;
    protected Collider2D previousHit;
    [SerializeField] protected bool isOnGrid = true;
    private bool hasChanged = false;

    protected virtual void Start()
    {
        posHits = new Collider2D[10];
    }

    
    protected virtual void Update()
    {
        TrackPosition();
        if (isOnGrid && hasChanged)
            ManageCallbacks();
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
            
        hasChanged = false;
    }

    protected virtual void HandleNotification(CellNotificationData data)
    {
        Debug.Log($"{gameObject.name} at {currentCell.pos.x},{currentCell.pos.y} is handling notification with {data.priority} priority, {data.signalStrength} signal strength, centered at {data.signalCenter.gameObject.name}");
    }
}
