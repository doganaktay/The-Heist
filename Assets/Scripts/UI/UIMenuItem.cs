using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIMenuItem : MonoBehaviour
{
    Image background;
    TextMeshProUGUI buttonText;

    public ButtonActionType type;
    [SerializeField]
    bool isCombinationButton = false;
    [HideInInspector]
    public bool IsAvailable { get; private set; }
    [HideInInspector]
    public bool IsSelected { get; private set; }

    private Color baseColor, selectColor;

    void Awake()
    {
        background = GetComponent<Image>();
        buttonText = GetComponentInChildren<TextMeshProUGUI>();

        var parentUI = GetComponentInParent<UIMenu>();
        baseColor = parentUI.BaseColor;
        selectColor = parentUI.SelectColor;
    }

    public void Select()
    {
        buttonText.color = baseColor;
        background.color = selectColor;
        IsSelected = true;
    }

    public void Deselect()
    {
        buttonText.color = selectColor;
        background.color = baseColor;
        IsSelected = false;
    }
}
