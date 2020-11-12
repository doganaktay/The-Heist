using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIButton : MonoBehaviour
{
    TouchUI touchUI;
    [SerializeField]
    PlacementObjectType type;
    public Image buttonRadial;
    public Image buttonMask;
    public RectTransform buttonIcon;

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
