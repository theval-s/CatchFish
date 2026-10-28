using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Hub navigation: bottom tabs, the tackle popup and the "Go fishing" button.
/// Switching tabs is pure UI, so the view does it itself.
/// </summary>
public class HubMenuView : MonoBehaviour
{
    [Header("Tabs")]
    [SerializeField] private GameObject homePanel;
    [SerializeField] private GameObject questsPanel;
    [SerializeField] private Button homeTabButton;
    [SerializeField] private Button questsTabButton;
    [SerializeField] private GameObject questsBadge;

    [Header("Popups")]
    [SerializeField] private GameObject tacklePanel;

    public event Action GoFishingClicked;

    private void Awake()
    {
        ShowHome();
        CloseTackle();
    }

    public void SetQuestsBadge(bool visible) => questsBadge.SetActive(visible);

    // Button OnClick methods below.

    public void GoFishing() => GoFishingClicked?.Invoke();

    public void ShowHome() => ShowTab(home: true);

    public void ShowQuests() => ShowTab(home: false);

    public void OpenTackle() => tacklePanel.SetActive(true);

    public void CloseTackle() => tacklePanel.SetActive(false);

    private void ShowTab(bool home)
    {
        homePanel.SetActive(home);
        questsPanel.SetActive(!home);

        // The open tab's button is disabled so it reads as selected.
        homeTabButton.interactable = !home;
        questsTabButton.interactable = home;
    }
}
