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
    Vector2 selectionDirection;
    int selectionIndex = -1;
    int lastSelectionIndex = -1;
    TouchUI touchUI;
    float offsetAngle = 0;

    void Awake()
    {
        touchUI = GetComponentInParent<TouchUI>();
        radialCanvasGroup = GetComponent<CanvasGroup>();

        perItemAngle = maxMenuAngle / transform.childCount;
        perItemFillPercent = perItemAngle / 360f;
        SetupRadialUI();
    }

    void Update()
    {
        if (isActive)
        {
            selectionDirection = (targetPoint - pivotPoint);
            Vector2 selectionDirectionNormalized = selectionDirection.normalized;
            float selectionAngle = Mathf.Atan2(selectionDirectionNormalized.x, selectionDirectionNormalized.y) * Mathf.Rad2Deg - offsetAngle;
            selectionAngle = (selectionAngle + 720f) % 360;
            selectionIndex = Mathf.FloorToInt(selectionAngle / perItemAngle);

            bool isValidSelection = IsValidIndex() && (selectionIndex != lastSelectionIndex || wasOutOfBounds);

            if (isValidSelection && IsInButtonBounds())
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
            else if (!IsInButtonBounds() && !wasOutOfBounds)
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
        //if (IsValidIndex() && IsInButtonBounds())
            //touchUI.CallButtonHit(menuItems[selectionIndex].actionType, menuItems[selectionIndex].itemType);
    }


    void SetupRadialUI()
    {
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

    void RotateRadialUI()
    {
        var halfRect = radialRectSize / 2f;

        offsetAngle = 0f;

        bool yOutOfBounds = pivotPoint.y + halfRect > Screen.height || pivotPoint.y - halfRect < 0f;

        if (yOutOfBounds)
        {
            if (pivotPoint.y + halfRect > Screen.height)
                offsetAngle = pivotPoint.x + halfRect > Screen.width ? 180f : 90f;
        
            if (pivotPoint.y - halfRect < 0f)
                offsetAngle = pivotPoint.x + halfRect > Screen.width ? 270f : 0f;

        }
        else if (pivotPoint.x + halfRect > Screen.width)
            offsetAngle = 270f;

        for(int i = 0; i < menuItems.Count; i++)
        {
            menuItems[i].GetComponent<RectTransform>().localRotation = Quaternion.Euler(0f, 0f, -perItemAngle * i - offsetAngle);
            menuItems[i].icon.rectTransform.rotation = Quaternion.identity;
        }
    }

    public void ShowRadialUI(MazeCell cell)
    {
        RotateRadialUI();

        if (cell.HasPlacedItem())
        {
            foreach(var button in menuItems)
            {
                if (button.actionType == ButtonActionType.PlaceObject)
                    button.ChangeButtonText("-");
                else if (button.actionType == ButtonActionType.UseObject)
                    button.gameObject.SetActive(true);
            }
        }
        else
        {
            foreach (var button in menuItems)
            {
                if (button.actionType == ButtonActionType.PlaceObject)
                    button.ChangeButtonText("+");
                else if (button.actionType == ButtonActionType.UseObject)
                    button.gameObject.SetActive(false);
            }
        }

        radialCanvasGroup.alpha = 1f;
        isActive = true;
    }

    public void HideRadialUI()
    {
        radialCanvasGroup.alpha = 0f;
        isActive = false;
    }

    bool IsValidIndex() => selectionIndex > -1 && selectionIndex < menuItems.Count;

    bool IsInButtonBounds() => selectionDirection.sqrMagnitude < (radialRectSize / 2f) * (radialRectSize / 2f)
                              && selectionDirection.sqrMagnitude > (radialRectSize / 7f) * (radialRectSize / 7f);

    private void ResetSelection()
    {
        foreach(var item in menuItems)
        {
            item.Deselect(baseColor);
        }
    }
}
