using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RadialMenu : MonoBehaviour
{
    [SerializeField]
    float maxMenuAngle = 360f;
    [SerializeField]
    float radialRectSize = 500f;

    public Color baseColor, hoverColor;

    float perItemAngle, perItemFillPercent;
    CanvasGroup radialCanvasGroup;
    List<RadialMenuItem> menuItems = new List<RadialMenuItem>();

    [HideInInspector]
    public Vector2 pivotPoint, targetPoint;
    bool isActive = false;
    bool wasOutOfBounds = false;
    int selectionIndex = -1;
    int lastSelectionIndex = -1;
    TouchUI touchUI;

    void Awake()
    {
        touchUI = GetComponentInParent<TouchUI>();
        radialCanvasGroup = GetComponent<CanvasGroup>();

        perItemAngle = maxMenuAngle / transform.childCount;
        perItemFillPercent = perItemAngle / 360f;

        // the icons rotate with the  radial pieces
        // so the trig calculation is always the same values
        float theta = (perItemAngle - perItemAngle / 2f) * Mathf.Deg2Rad;
        float xPos = Mathf.Sin(theta);
        float yPos = Mathf.Cos(theta);

        float scaledIconSize = Mathf.Min(perItemAngle / 45f, 1f);

        for (int i = 0; i < transform.childCount; i++)
        {
            RadialMenuItem item = transform.GetChild(i).GetComponent<RadialMenuItem>();
            menuItems.Add(item);

            item.radialBackground.rectTransform.sizeDelta = new Vector2(radialRectSize, radialRectSize);
            item.radialBackground.fillAmount = perItemFillPercent;
            item.GetComponent<RectTransform>().localRotation = Quaternion.Euler(0f, 0f, -perItemAngle * i);

            RectTransform iconRect = item.icon.rectTransform;

            iconRect.localScale = new Vector3(iconRect.localScale.x * scaledIconSize, iconRect.localScale.y * scaledIconSize, 1f);

            iconRect.localPosition = new Vector2(xPos, yPos) * (radialRectSize / 3f);
            iconRect.rotation = Quaternion.identity;
        }
    }

    void Update()
    {
        if (isActive)
        {
            Vector2 selectionDirection = (targetPoint - pivotPoint);
            Vector2 selectionDirectionNormalized = selectionDirection.normalized;
            float selectionAngle = Mathf.Atan2(selectionDirectionNormalized.x, selectionDirectionNormalized.y) * Mathf.Rad2Deg;
            selectionAngle = (selectionAngle + 360f) % 360;
            selectionIndex = (int)(selectionAngle / perItemAngle);

            bool isValidSelection = selectionIndex > -1 && selectionIndex < menuItems.Count
                                    && (selectionIndex != lastSelectionIndex || wasOutOfBounds);

            bool isInBounds = selectionDirection.sqrMagnitude < (radialRectSize / 2f) * (radialRectSize / 2f)
                              && selectionDirection.sqrMagnitude > (radialRectSize / 7f) * (radialRectSize / 7f);


            if (isValidSelection && isInBounds)
            {
                if (lastSelectionIndex > -1 && lastSelectionIndex < menuItems.Count)
                    menuItems[lastSelectionIndex].Deselect(baseColor);

                menuItems[selectionIndex].Select(hoverColor);

                wasOutOfBounds = false;

                lastSelectionIndex = selectionIndex;
            }
            else if (selectionIndex >= menuItems.Count && selectionIndex != lastSelectionIndex)
            {
                if (lastSelectionIndex > -1 && lastSelectionIndex < menuItems.Count)
                    menuItems[lastSelectionIndex].Deselect(baseColor);

                lastSelectionIndex = selectionIndex;
            }
            else if (!isInBounds && !wasOutOfBounds)
            {
                if (selectionIndex > -1 && selectionIndex < menuItems.Count)
                    menuItems[selectionIndex].Deselect(baseColor);

                wasOutOfBounds = true;
            }
        }
        else if (selectionIndex > -1 && selectionIndex < menuItems.Count)
        {
            menuItems[selectionIndex].Deselect(baseColor);
            selectionIndex = -1;
            lastSelectionIndex = -1;
        }
    }

    public void PressButton()
    {
        touchUI.CallButtonHit(menuItems[selectionIndex].buttonActionType);
    }

    public void ShowRadialUI()
    {
        radialCanvasGroup.alpha = 1f;
        isActive = true;
    }

    public void HideRadialUI()
    {
        radialCanvasGroup.alpha = 0f;
        isActive = false;
    }

    private void ResetSelection()
    {
        foreach(var item in menuItems)
        {
            item.Deselect(baseColor);
        }
    }
}
