using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>The energy meter in the hub and its refill popup. Opening and closing is pure UI.</summary>
public class EnergyView : MonoBehaviour
{
    [SerializeField] private TMP_Text amountText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private GameObject popup;
    [SerializeField] private TMP_Text popupText;
    [SerializeField] private TMP_Text refillLabel;
    [SerializeField] private Button refillButton;

    public event Action RefillClicked;

    private void Awake() => Close();

    public void Show(string amount, string timer)
    {
        amountText.text = amount;
        timerText.text = timer;
    }

    public void ShowPopup(string text, string refillText, bool canRefill)
    {
        popupText.text = text;
        refillLabel.text = refillText;
        refillButton.interactable = canRefill;
    }

    // Button OnClick methods below.

    public void Open() => popup.SetActive(true);

    public void Close() => popup.SetActive(false);

    public void Refill() => RefillClicked?.Invoke();
}
