using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>One quest in the Quests tab. The Complete button's OnClick calls <see cref="Complete"/>.</summary>
public class QuestItemView : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text requirementsText;
    [SerializeField] private Button completeButton;

    public event Action CompleteClicked;

    public void Show(string title, string description, string requirements, bool canComplete)
    {
        titleText.text = title;
        descriptionText.text = description;
        requirementsText.text = requirements;
        completeButton.interactable = canComplete;
    }

    // Button OnClick
    public void Complete() => CompleteClicked?.Invoke();
}
