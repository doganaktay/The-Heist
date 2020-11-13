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
    int selectionIndex = -1;
    int lastSelectionIndex = -1;

    void Awake()
    {
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
            Vector2 selectionDirection = (targetPoint - pivotPoint).normalized;
            float selectionAngle = Mathf.Atan2(selectionDirection.x, selectionDirection.y) * Mathf.Rad2Deg;
            selectionAngle = (selectionAngle + 360f) % 360;
            selectionIndex = (int)(selectionAngle / perItemAngle);

            if(selectionIndex > -1 && selectionIndex < menuItems.Count && selectionIndex != lastSelectionIndex)
            {
                if(lastSelectionIndex > -1 && lastSelectionIndex < menuItems.Count)
                    menuItems[lastSelectionIndex].Deselect(baseColor);

                menuItems[selectionIndex].Select(hoverColor);

                lastSelectionIndex = selectionIndex;

                Debug.Log("New selection");
            }
            else if (selectionIndex >= menuItems.Count && selectionIndex != lastSelectionIndex)
            {
                menuItems[lastSelectionIndex].Deselect(baseColor);
                lastSelectionIndex = selectionIndex;
            }

            Debug.Log("Selection angle: " + selectionAngle + " Selection index: " + selectionIndex);
        }
        else if (selectionIndex > -1)
        {
            menuItems[selectionIndex].Deselect(baseColor);
            selectionIndex = -1;
            lastSelectionIndex = -1;
        }
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
}
