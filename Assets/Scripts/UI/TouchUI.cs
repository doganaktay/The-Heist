using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum ButtonActionType
{
    PlaceObject,
    UseObject,
    Menu
}

public class TouchUI : MonoBehaviour
{
    [SerializeField]
    CanvasGroup touchPoint, touchAim, topMenu;
    [SerializeField]
    RadialMenu radialMenu;
    [SerializeField]
    float touchTime = 0.5f;
    [SerializeField]
    float aimClampDistance = 10f;

    Camera cam;
    Coroutine fade;

    public bool ShowAimUI { get; set; }
    private Vector3 aimCenter, aimPos;
    public Vector3 AimCenter { get => aimCenter; set => aimCenter = value; }
    public Vector3 AimPos { get => aimPos; set => aimPos = value; }
    public bool ShowInputUI { get; set; }
    private Vector2 inputUIPos;
    public Vector2 InputUIPos { get => inputUIPos; set => inputUIPos = value; }
    bool inputUISet = false;
    private MazeCell currentTouchCell;
    public MazeCell CurrentTouchCell { get => currentTouchCell; set => currentTouchCell = value; }

    public float topMenuHeight;


    public static Action<PlaceableItemType, MazeCell> PlaceOrRemoveItem;

    private void Start()
    {
        cam = Camera.main;

        topMenu.GetComponent<RectTransform>().sizeDelta = new Vector2(Screen.width, topMenuHeight);
        topMenu.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -topMenuHeight / 2f);

    }

    private void Update()
    {
        if (ShowAimUI)
        {
            Vector2 aimCenter = cam.WorldToScreenPoint(this.aimCenter);
            Vector2 aimPos = cam.WorldToScreenPoint(this.aimPos);
            Vector2 aimDir = aimPos - aimCenter;
            touchAim.transform.position = aimCenter + Vector2.ClampMagnitude(aimDir, aimClampDistance);
            touchAim.alpha = 1f;
        }
        else
        {
            touchAim.alpha = 0;
        }

    }

    public void CallButtonHit(ButtonActionType actionType, PlaceableItemType itemType)
    {
        if(actionType == ButtonActionType.PlaceObject)
        {
            PlaceOrRemoveItem(itemType, currentTouchCell);
        }
        else if (actionType == ButtonActionType.UseObject)
        {
            if (currentTouchCell.placedItems.ContainsKey(itemType) && currentTouchCell.placedItems[itemType] != null)
                currentTouchCell.placedItems[itemType].UseItem();
            else
                Debug.Log("no item found for use");
        }

    }

    public void TouchPoint(Vector3 point)
    {
        if(fade != null)
        {
            StopCoroutine(fade);
            touchPoint.alpha = 0;
        }

        fade = StartCoroutine(IndicatePoint(point));
    }

    IEnumerator IndicatePoint(Vector3 point)
    {
        touchPoint.transform.position = cam.WorldToScreenPoint(point);

        // fade in
        while(touchPoint.alpha < 1)
        {
            touchPoint.alpha += Time.deltaTime / (touchTime / 2);
            yield return null;
        }

        // fade out
        while (touchPoint.alpha > 0)
        {
            touchPoint.alpha -= Time.deltaTime / (touchTime / 2);
            yield return null;
        }
    }
}
