using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIMenu : MonoBehaviour
{
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
        TouchUI.instance = GetComponentInParent<TouchUI>();
        menuCanvasGroup = GetComponent<CanvasGroup>();

        for(int i = 0; i < transform.childCount; i++)
        {
            var items = transform.GetChild(i).GetComponentsInChildren<UIMenuItem>();

            foreach(var item in items)
            {
                if (!menuItems.Contains(item))
                    menuItems.Add(item);
            }
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
                TouchUI.instance.CurrentSelectedButton = item;
                haveSelection = true;
            }
            else
                item.Deselect();
        }

        if (!haveSelection)
            TouchUI.instance.CurrentSelectedButton = null;
    }

    public void DeselectMenuItem(UIMenuItem selection)
    {
        if (selection.IsSelected)
        {
            selection.Deselect();
            TouchUI.instance.CurrentSelectedButton = null;
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
