using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RadialMenuItem : MonoBehaviour
{
    public Image radialBackground;
    public Image icon;

    public ButtonActionType buttonActionType;

    public void Select(Color hoverColor)
    {
        radialBackground.color = hoverColor;
    }

    public void Deselect(Color baseColor)
    {
        radialBackground.color = baseColor;
    }
}
