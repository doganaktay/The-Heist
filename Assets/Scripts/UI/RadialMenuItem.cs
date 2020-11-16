using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RadialMenuItem : MonoBehaviour
{
    public Image radialBackground;
    public Image icon;

    public TextMeshProUGUI buttonText;

    public ButtonActionType actionType;
    public PlaceableItemType itemType;

    private void Awake()
    {
        buttonText = GetComponentInChildren<TextMeshProUGUI>();
    }

    public void Select(Color hoverColor)
    {
        radialBackground.color = hoverColor;
    }

    public void Deselect(Color baseColor)
    {
        radialBackground.color = baseColor;
    }

    public void ChangeButtonText(string text)
    {
        buttonText.text = text;
    }
}
