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
    [SerializeField] private Button goFishingButton;

    [Header("Popups")]
    [SerializeField] private GameObject tacklePanel;
    [SerializeField] private Button tackleButton;

    private bool _firstRunMode;
    private Vector3 _goFishingScale;

    public event Action GoFishingClicked;

    private void Awake()
    {
        _goFishingScale = goFishingButton.transform.localScale;
        ShowHome();
        CloseTackle();
    }

    private void Update()
    {
        if (_firstRunMode)
        {
            // Draw attention to the only available action on the first visit.
            goFishingButton.transform.localScale = _goFishingScale * (1.05f + 0.05f * Mathf.Sin(Time.unscaledTime * 4f));
        }
    }

    /// <summary>Before the first run only "Go fishing" works: the Quests tab and the tackle shop are locked.</summary>
    public void SetFirstRunMode(bool isFirstRun)
    {
        _firstRunMode = isFirstRun;
        tackleButton.interactable = !isFirstRun;
        goFishingButton.transform.localScale = _goFishingScale;
        CloseTackle();
        ShowHome();
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
        questsTabButton.interactable = home && !_firstRunMode;
    }
}
