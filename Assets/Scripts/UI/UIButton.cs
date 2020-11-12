using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIButton : MonoBehaviour
{
    TouchUI touchUI;
    [SerializeField]
    PlacementObjectType type;

    private void Awake()
    {
        touchUI = GetComponentInParent<TouchUI>();
    }

    public void PlaceObject()
    {
        touchUI.CallPlaceObject(type);
        Debug.Log("Placing bomb");
    }
}
