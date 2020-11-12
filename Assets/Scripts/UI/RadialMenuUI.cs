using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RadialMenuUI : MonoBehaviour
{
    CanvasGroup menuCanvas;
    [SerializeField]
    Image radialBackground;
    [SerializeField]
    Image radialBackgroundMask;
    [SerializeField]
    float maxSpreadAngle = 360f;
    [SerializeField]
    float radius = 100f;
    [SerializeField]
    float uiRingBuffer = 20f;

    Camera cam;
    float halfButtonHeight;

    private void Awake()
    {
        menuCanvas = GetComponent<CanvasGroup>();
        cam = Camera.main;

        float halfButtonHeight = transform.GetChild(1).GetComponent<RectTransform>().rect.height;

        Rect radialRect = radialBackground.GetComponent<RectTransform>().rect;
        radialRect.height = radialRect.width = radialRect.height + radius;

        float newOuterSize = radialRect.height + radius + uiRingBuffer;
        radialBackground.GetComponent<RectTransform>().sizeDelta =
            new Vector2(newOuterSize, newOuterSize);
        float newInnerSize = radialRect.height + radius - halfButtonHeight * 2f - uiRingBuffer;
        radialBackgroundMask.GetComponent<RectTransform>().sizeDelta =
            new Vector2(newInnerSize, newInnerSize);

        radialBackground.fillAmount = (maxSpreadAngle / 360f);
    }

    public void ShowRadialMenu(Vector2 center)
    {
        var screenPos = cam.WorldToScreenPoint(center);

        transform.position = screenPos;

        DetermineMenuLayout(screenPos);

        menuCanvas.alpha = 1f;
    }

    public void HideRadialMenu()
    {
        menuCanvas.alpha = 0f;
    }

    private void DetermineMenuLayout(Vector2 screenPos)
    {
        float spreadAngle = maxSpreadAngle;
        float angleOffset = 0f;

        bool firstDirectionBlocked = false;

        if ((screenPos + Vector2.up * (radius + halfButtonHeight)).y > Screen.height)
        {
            angleOffset += 180f;
            firstDirectionBlocked = true;
        }
        else if ((screenPos + Vector2.down * (radius + halfButtonHeight)).y < -Screen.height)
        {

        }


        float perButtonAngle = maxSpreadAngle * Mathf.Deg2Rad / (transform.childCount - 2);

        // first child is background image so we skip it
        for(int i = 1; i < transform.childCount; i++)
        {
            var child = transform.GetChild(i);
            float theta = perButtonAngle * (i - 1);
            float xPos = Mathf.Sin(theta);
            float yPos = Mathf.Cos(theta);
            transform.GetChild(i).localPosition = new Vector2(xPos, yPos) * radius;
        }
    }
}
