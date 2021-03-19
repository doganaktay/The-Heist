using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundBomb : PlaceableItem, IPropagatable
{
    private List<(MazeCell cell, float ratio)> affectedCells = new List<(MazeCell cell, float ratio)>();
    [SerializeField] private float strength;
    [SerializeField] private float minThreshold;

    public List<(MazeCell cell, float ratio)> AffectedCells { get => affectedCells; set => affectedCells = value; }
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
            affected.cell.IndicateAOE(notificationColor);
        }
    }

    public List<(MazeCell cell, float ratio)> AcquirePropagationArea(MazeCell center, float strength, float minimum)
    {
        return Propagation.instance.Propagate(center, strength, minimum);
    }

    public override void UseItem()
    {
        Debug.Log($"Detonating sound bomb affecting {affectedCells.Count} cells at {position.gameObject.name}");
        foreach(var pair in affectedCells)
        {
            NotificationModule.MakeNotification(pair.cell, new CellNotificationData(CellNotificationType.Sound, pair.ratio, position));
            pair.cell.ClearAOE(notificationColor);
        }

        Destroy(gameObject, 0.1f);
    }

    private void OnDestroy()
    {
        // clearing notification color from cells when item is removed instead of used
        foreach (var pair in affectedCells)
        {
            pair.cell.ClearAOE(notificationColor);
        }
    }
}
