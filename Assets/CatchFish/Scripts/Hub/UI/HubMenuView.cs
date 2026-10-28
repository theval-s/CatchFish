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

    private bool _newbieMode;
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
        if (_newbieMode)
        {
            // Draw attention to the only available action on the first visit.
            goFishingButton.transform.localScale = _goFishingScale * (1.05f + 0.05f * Mathf.Sin(Time.unscaledTime * 4f));
        }
    }

    public void SetNewbieMode(bool enabled)
    {
        _newbieMode = enabled;
        questsTabButton.interactable = !enabled;
        tackleButton.interactable = !enabled;
        if (enabled)
        {
            ShowHome();
            CloseTackle();
        }
        else
        {
            goFishingButton.transform.localScale = _goFishingScale;
        }
    }

    public void SetGoFishingEnabled(bool enabled) => goFishingButton.interactable = enabled;

    public void SetQuestsBadge(bool visible) => questsBadge.SetActive(visible);

    // Button OnClick methods below.

    public void GoFishing() => GoFishingClicked?.Invoke();

    public void ShowHome() => ShowTab(home: true);

    public void ShowQuests()
    {
        if (!_newbieMode) ShowTab(home: false);
    }

    public void OpenTackle()
    {
        if (!_newbieMode) tacklePanel.SetActive(true);
    }

    public void CloseTackle() => tacklePanel.SetActive(false);

    private void ShowTab(bool home)
    {
        homePanel.SetActive(home);
        questsPanel.SetActive(!home);

        // The open tab's button is disabled so it reads as selected.
        homeTabButton.interactable = !home;
        questsTabButton.interactable = home && !_newbieMode;
    }
}
