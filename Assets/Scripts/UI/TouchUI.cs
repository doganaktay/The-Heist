using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum PlacementObjectType
{
    SoundBomb
}

public class TouchUI : MonoBehaviour
{
    [SerializeField]
    CanvasGroup touchPoint, touchAim;
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
    public Vector3 AimCenter { get { return aimCenter; } set { aimCenter = value; } }
    public Vector3 AimPos { get { return aimPos; } set { aimPos = value; } }
    public bool ShowInputUI { get; set; }
    private Vector2 inputUIPos;
    public Vector2 InputUIPos { get { return inputUIPos; } set { inputUIPos = value; } }
    bool inputUISet = false;

    public Action<PlacementObjectType> PlaceObject;

    private void Awake()
    {
        cam = Camera.main;
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

        if (ShowInputUI)
        {
            if (!inputUISet)
            {
                radialMenu.transform.position = inputUIPos;
                radialMenu.ShowRadialUI();
                radialMenu.pivotPoint = inputUIPos;
                inputUISet = true;
            }
            else
            {
                radialMenu.targetPoint = inputUIPos;
            }
        }
        else
        {
            radialMenu.HideRadialUI();
            inputUISet = false;
        }
    }

    public void CallPlaceObject(PlacementObjectType type)
    {
        PlaceObject(type);
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
