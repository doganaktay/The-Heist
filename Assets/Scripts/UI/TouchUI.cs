using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TouchUI : MonoBehaviour
{
    [SerializeField]
    CanvasGroup touchPoint, touchAim;
    [SerializeField]
    float touchTime = 0.5f;
    [SerializeField]
    float aimClampDistance = 10f;

    Camera cam;
    Coroutine fade;

    private bool showAim = false;
    public bool ShowAim { get { return showAim; }  set { showAim = value; } }
    private Vector3 aimCenter, aimPos;
    public Vector3 AimCenter { get { return aimCenter; } set { aimCenter = value; } }
    public Vector3 AimPos { get { return aimPos; } set { aimPos = value; } }

    private void Awake()
    {
        cam = Camera.main;
    }

    private void Update()
    {
        if (showAim)
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
