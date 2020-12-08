using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PlaceableItemType
{
    SoundBomb
}

public class PlaceableItem : MonoBehaviour
{

    public PlaceableItemType type;
    protected MazeCell position;
    public MazeCell Position { get => position; set => position = value; }

    public virtual void UseItem() { }
    public virtual void Place(MazeCell cell) { position = cell; cell.PlaceItem(type, this); }
}
