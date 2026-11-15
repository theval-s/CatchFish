using System;
using TMPro;
using UnityEngine;

/// <summary>A story dialog over the hub: one line at a time, with a Next button.</summary>
public class DialogView : MonoBehaviour
{
    [SerializeField] private GameObject popup;
    [SerializeField] private TMP_Text speakerText;
    [SerializeField] private TMP_Text lineText;
    [SerializeField] private TMP_Text nextLabel;

    public event Action NextClicked;

    public void Show(string speaker, string text, bool isLast)
    {
        popup.SetActive(true);
        speakerText.gameObject.SetActive(!string.IsNullOrEmpty(speaker));
        speakerText.text = speaker;
        lineText.text = text;
        nextLabel.text = isLast ? "Let's go!" : "Next";
    }

    public void Hide() => popup.SetActive(false);

    // Button OnClick
    public void Next() => NextClicked?.Invoke();
}
