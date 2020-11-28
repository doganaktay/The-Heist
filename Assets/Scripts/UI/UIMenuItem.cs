using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIMenuItem : MonoBehaviour
{
    Image background;
    TextMeshProUGUI buttonText;
    UIMenu menu;

    [SerializeField]
    ButtonActionType buttonType;
    public ButtonActionType ButtonType { get => buttonType; private set => buttonType = value; }
    [SerializeField]
    PlaceableItemType itemType;
    public PlaceableItemType ItemType { get => itemType; private set => itemType = value; }

    [SerializeField]
    bool isCombinationButton = false;
    [HideInInspector]
    public bool IsAvailable { get; set; }
    [HideInInspector]
    public bool IsSelected { get; private set; }

    private Color baseColor, selectColor;

    void Awake()
    {
        background = GetComponent<Image>();
        buttonText = GetComponentInChildren<TextMeshProUGUI>();

        menu = GetComponentInParent<UIMenu>();
        baseColor = menu.BaseColor;
        selectColor = menu.SelectColor;
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

#if UNITY_EDITOR

namespace Archi.Touch.Inspector
{
    using UnityEditor;

    [CustomEditor(typeof(UIMenuItem))]
    public class UIMenuItem_Editor : Editor
    {
        public override void OnInspectorGUI()
        {
            var menuItem = (UIMenuItem)target;

            DrawDefault("buttonType");

            if (menuItem.ButtonType == ButtonActionType.PlaceObject)
                DrawDefault("itemType");

            DrawDefault("isCombinationButton");

            Repaint();
        }

        private void DrawDefault(string name)
        {
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(serializedObject.FindProperty(name));

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}

#endif
