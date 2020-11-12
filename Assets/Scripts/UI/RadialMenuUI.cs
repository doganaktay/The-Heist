using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RadialMenuUI : MonoBehaviour
{
    CanvasGroup menuCanvas;
    [SerializeField]
    float maxSpreadAngle = 360f;
    [SerializeField]
    float angleOffset = 10f;
    [SerializeField]
    float radius = 100f;
    [SerializeField]
    float ringOuterBuffer = 20f;
    [SerializeField]
    float ringInnerBuffer = 50f;
    [SerializeField]
    float buttonSize = 100f;

    Camera cam;

    List<UIButton> buttons = new List<UIButton>();

    private void Awake()
    {
        menuCanvas = GetComponent<CanvasGroup>();
        cam = Camera.main;

        for(int i = 0; i < transform.childCount; i++)
        {
            var button = transform.GetChild(i).GetComponent<UIButton>();
            buttons.Add(button);

            float newOuterSize = radius + buttonSize / 2f + ringOuterBuffer;
            button.buttonRadial.GetComponent<RectTransform>().sizeDelta = new Vector2(newOuterSize, newOuterSize);
            float newInnerSize = radius - buttonSize / 2f - ringInnerBuffer;
            button.buttonMask.GetComponent<RectTransform>().sizeDelta = new Vector2(newInnerSize, newInnerSize);
        }
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
        float perButtonAngle = spreadAngle / transform.childCount;
        float buttonFillAmount = perButtonAngle / 360f;

        for(int i = 0; i < buttons.Count; i++)
        {
            var button = buttons[i];
            button.buttonRadial.fillAmount = buttonFillAmount;
            Quaternion rot = button.buttonMask.rectTransform.localRotation;
            rot.eulerAngles = new Vector3(0f, 0f, -perButtonAngle * i);
            button.buttonMask.rectTransform.localRotation = rot;

            float theta = (perButtonAngle * Mathf.Deg2Rad * (i + 1)) - (perButtonAngle * Mathf.Deg2Rad / 2f);
            float xPos = Mathf.Sin(theta);
            float yPos = Mathf.Cos(theta);

            button.buttonIcon.transform.localPosition = new Vector2(xPos, yPos) * radius;
        }
    }
}
