using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The daily reward popup with its track of days, and the hub button that opens it (with a "!" when a
/// reward is waiting). Opening and closing is pure UI, so the view does it itself.
/// </summary>
public class DailyRewardView : MonoBehaviour
{
    [SerializeField] private GameObject popup;
    [SerializeField] private GameObject badge;
    [SerializeField] private DailyRewardDayView dayPrefab;
    [SerializeField] private Transform dayContainer;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button claimButton;

    public event Action ClaimClicked;

    private void Awake() => Close();

    public void ClearDays()
    {
        foreach (Transform child in dayContainer)
        {
            // Destroy happens at the end of the frame; hide it now so the layout skips it.
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
    }

    public DailyRewardDayView AddDay() => Instantiate(dayPrefab, dayContainer);

    public void ShowStatus(string text, bool canClaim)
    {
        statusText.text = text;
        claimButton.interactable = canClaim;
        badge.SetActive(canClaim);
    }

    // Button OnClick methods below.

    public void Open() => popup.SetActive(true);

    public void Close() => popup.SetActive(false);

    public void Claim() => ClaimClicked?.Invoke();
}
