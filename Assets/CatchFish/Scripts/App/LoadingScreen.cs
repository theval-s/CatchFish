using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Full-screen overlay that survives scene changes and blocks input while loading.</summary>
public class LoadingScreen : IDisposable
{
    private GameObject _screen;

    public void Show()
    {
        if (_screen == null)
        {
            CreateScreen();
        }

        _screen.SetActive(true);
    }

    public void Hide()
    {
        if (_screen != null)
        {
            _screen.SetActive(false);
        }
    }

    public void Dispose()
    {
        if (_screen != null)
        {
            UnityEngine.Object.Destroy(_screen);
        }
    }

    private void CreateScreen()
    {
        _screen = new GameObject("Loading Screen", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        UnityEngine.Object.DontDestroyOnLoad(_screen);

        var canvas = _screen.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 100;

        var scaler = _screen.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(720, 1280);
        scaler.matchWidthOrHeight = 0.5f;

        var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
        background.transform.SetParent(_screen.transform, false);
        var backgroundRect = (RectTransform)background.transform;
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;
        var image = background.GetComponent<Image>();
        image.color = new Color(0.07f, 0.1f, 0.16f);
        image.raycastTarget = true;

        var label = new GameObject("Loading Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        label.transform.SetParent(background.transform, false);
        var labelRect = (RectTransform)label.transform;
        labelRect.anchorMin = new Vector2(0.5f, 0.5f);
        labelRect.anchorMax = labelRect.anchorMin;
        labelRect.sizeDelta = new Vector2(600f, 100f);
        var text = label.GetComponent<TextMeshProUGUI>();
        text.text = "Loading...";
        text.fontSize = 48;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
    }
}
