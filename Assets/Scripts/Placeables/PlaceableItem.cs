using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PlaceableItemType
{
    SoundBomb
}

public class PlaceableItem : MonoBehaviour
{

    [SerializeField]
    bool hasAOE = false;
    List<MazeCell> affectedCells = new List<MazeCell>();

    public virtual void UseItem() { }
}
