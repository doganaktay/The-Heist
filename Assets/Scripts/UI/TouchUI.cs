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
    public static TouchUI instance;

    public GameManager gameManager;

    [SerializeField]
    CanvasGroup touchPoint, touchAim, topUIBar, mainMenu;
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
    private MazeCell currentTouchCell;
    public MazeCell CurrentTouchCell { get => currentTouchCell; set => currentTouchCell = value; }

    public float topMenuHeight, mainMenuHeight;

    UIMenuItem currentSelectedButton;
    public UIMenuItem CurrentSelectedButton { get => currentSelectedButton; set => currentSelectedButton = value; }

    private void Start()
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

    public void SetupUIDimensions(float mazeHeight)
    {
        var topMenuHeight = (1 - mazeHeight) * Screen.height;
        var mainMenuHeight = mazeHeight * Screen.height;

        var topUIRect = topUIBar.GetComponent<RectTransform>();
        topUIRect.sizeDelta = new Vector2(Screen.width, topMenuHeight);
        topUIRect.anchoredPosition = new Vector2(0f, -topMenuHeight / 2f);

        var mainMenuRect = mainMenu.GetComponent<RectTransform>();
        mainMenuRect.sizeDelta = new Vector2(Screen.width, mainMenuHeight);
        mainMenuRect.anchoredPosition = new Vector2(0f, -mainMenuHeight / 2f);
        var mainMenuBgRect = mainMenu.transform.GetChild(0).GetComponent<RectTransform>();
        mainMenuBgRect.sizeDelta = new Vector2(Screen.width, mainMenuHeight);
        mainMenuBgRect.anchoredPosition = new Vector2(0f, -topMenuHeight);
        var mainMenuItemsRect = mainMenu.transform.GetChild(1).GetComponent<RectTransform>();
        mainMenuItemsRect.sizeDelta = new Vector2(Screen.width / 2f, mainMenuHeight / 2f);
        mainMenuItemsRect.anchoredPosition = new Vector2(0f, -topMenuHeight);
    }

    #region Main menu methods

    public void ShowMainMenu()
    {
        mainMenu.gameObject.SetActive(true);
        mainMenu.alpha = 1f;
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        Time.timeScale = 1f;
        mainMenu.alpha = 0f;
        currentSelectedButton.Deselect();
        currentSelectedButton = null;
        mainMenu.gameObject.SetActive(false);
    }

    public void ResetGame()
    {
        gameManager.RestartGame();
        Time.timeScale = 1f;
        mainMenu.alpha = 0f;
        currentSelectedButton.Deselect();
        currentSelectedButton = null;
        mainMenu.gameObject.SetActive(false);
    }

    public void QuitGame()
    {
        Debug.Log("Quit application");
        Application.Quit();
    }

    #endregion
}
