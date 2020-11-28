using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIMenu : MonoBehaviour
{
    TouchUI touchUI;
    CanvasGroup menuCanvasGroup;

    [SerializeField]
    Color baseColor, selectColor;
    public Color BaseColor { get => baseColor; private set => baseColor = value; }
    public Color SelectColor { get => selectColor; private set => selectColor = value; }

    [SerializeField]
    float fadeTime = 2f;

    List<UIMenuItem> menuItems = new List<UIMenuItem>();

    void Awake()
    {
        touchUI = GetComponentInParent<TouchUI>();
        menuCanvasGroup = GetComponent<CanvasGroup>();

        for(int i = 0; i < transform.childCount; i++)
        {
            menuItems.Add(transform.GetChild(i).GetComponent<UIMenuItem>());
        }
    }

    public void SelectMenuItem(UIMenuItem selection)
    {
        bool haveSelection = false;

        foreach(var item in menuItems)
        {
            if (item == selection && !item.IsSelected)
            {
                item.Select();
                touchUI.CurrentSelectedButton = item;
                haveSelection = true;
            }
            else
                item.Deselect();
        }

        if (!haveSelection)
            touchUI.CurrentSelectedButton = null;
    }

    public void DeselectMenuItem(UIMenuItem selection)
    {
        if (selection.IsSelected)
        {
            selection.Deselect();
            touchUI.CurrentSelectedButton = null;
        }
    }

    IEnumerator FadeIn()
    {
        while(menuCanvasGroup.alpha < 1f)
        {
            menuCanvasGroup.alpha += Time.deltaTime / fadeTime;
            yield return null;
        }
    }

    IEnumerator FadeOut()
    {
        while (menuCanvasGroup.alpha > 0f)
        {
            menuCanvasGroup.alpha -= Time.deltaTime / fadeTime;
            yield return null;
        }
    }
}
