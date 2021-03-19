using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PlaceableItemType
{
    SoundBomb,
    Disabler
}

public class PlaceableItem : MonoBehaviour
{

    public PlaceableItemType type;
    protected MazeCell position;
    public MazeCell Position { get => position; set => position = value; }
    public bool isUsable = false;
    [SerializeField] protected Color notificationColor;

    public virtual void UseItem() { }
    public virtual void Place(MazeCell cell) { position = cell; cell.PlaceItem(type, this); }
}
