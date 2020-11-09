using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TouchUI : MonoBehaviour
{
    [SerializeField]
    CanvasGroup touchPoint, touchAim, touchPlace;
    [SerializeField]
    float touchTime = 0.5f;
    [SerializeField]
    float aimClampDistance = 10f;
    [SerializeField]
    float placementUIDistance = 10f;

    Camera cam;
    Coroutine fade;

    public bool ShowAimUI { get; set; }
    private Vector3 aimCenter, aimPos;
    public Vector3 AimCenter { get { return aimCenter; } set { aimCenter = value; } }
    public Vector3 AimPos { get { return aimPos; } set { aimPos = value; } }
    public bool ShowPlacementUI { get; set; }
    private Vector2 placementPos;
    public Vector2 PlacementPos { get { return placementPos; } set { placementPos = value; } }
    bool placementUISet = false;

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
            touchAim.transform.position = Vector2.ClampMagnitude(aimDir, aimClampDistance) + aimCenter;
            touchAim.alpha = 1f;
        }
        else
        {
            touchAim.alpha = 0;
        }

        if (ShowPlacementUI)
        {
            if (!placementUISet)
            {
                touchPlace.transform.position = DeterminePlacementUIPos(placementPos);
                placementUISet = true;
            }

            touchPlace.alpha = 1f;
        }
        else
        {
            placementUISet = false;
            touchPlace.alpha = 0;
        }
    }

    private Vector2 DeterminePlacementUIPos(Vector2 point)
    {
        var displacement = (Vector2.up * placementUIDistance);
        return (point + displacement * 1.5f).y < Screen.height ? point + displacement : point - displacement;
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
