using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundBomb : PlaceableItem, IPropagatable
{
    private List<MazeCell> affectedCells = new List<MazeCell>();
    [SerializeField] private float strength;
    [SerializeField] private float minThreshold;
    [SerializeField] Color notificationColor;

    public List<MazeCell> AffectedCells { get => affectedCells; set => affectedCells = value; }
    public float PropagationStrength { get => strength; set => strength = value; }
    public float PropagationMinThreshold { get => minThreshold; set => minThreshold = value; }
    public MazeCell Center { get => position; set => position = value; }
    public Color NotificationColor { get => notificationColor; set => notificationColor = value; }

    public override void Place(MazeCell cell)
    {
        base.Place(cell);
        affectedCells = AcquirePropagationArea(position, strength, minThreshold);
        foreach(var affected in affectedCells)
        {
            affected.IndicateAOE(notificationColor);
        }
    }

    public List<MazeCell> AcquirePropagationArea(MazeCell center, float strength, float minimum)
    {
        return Propagation.instance.Propagate(center, strength, minimum);
    }

    public override void UseItem()
    {
        Debug.Log($"Detonating sound bomb affecting {affectedCells.Count} cells at {position.gameObject.name}");
        foreach(var cell in affectedCells)
        {
            NotificationModule.MakeNotification(cell, new CellNotificationData(1, 10, position));
            cell.ClearAOE(notificationColor);
        }
        //position.RemoveItem(type);
        Destroy(gameObject, 0.1f);
    }
}
