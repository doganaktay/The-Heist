using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PlaceableItemType
{
    SoundBomb
}

public class PlaceableItem : MonoBehaviour
{
    public virtual void UseItem() { }
}
