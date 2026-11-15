using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>One class in the School tab, with its Hire tutor and Add student buttons.</summary>
public class SchoolClassItemView : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text infoText;
    [SerializeField] private Button hireButton;
    [SerializeField] private TMP_Text hireLabel;
    [SerializeField] private Button studentButton;
    [SerializeField] private TMP_Text studentLabel;

    public event Action HireClicked;
    public event Action StudentClicked;

    public void Show(string title, string info)
    {
        titleText.text = title;
        infoText.text = info;
    }

    /// <summary>Null text hides the button.</summary>
    public void ShowHire(string text, bool canHire) => ShowButton(hireButton, hireLabel, text, canHire);

    public void ShowStudent(string text, bool canAdd) => ShowButton(studentButton, studentLabel, text, canAdd);

    // Button OnClick methods below.

    public void Hire() => HireClicked?.Invoke();

    public void AddStudent() => StudentClicked?.Invoke();

    private static void ShowButton(Button button, TMP_Text label, string text, bool interactable)
    {
        button.gameObject.SetActive(text != null);
        label.text = text;
        button.interactable = interactable;
    }
}
