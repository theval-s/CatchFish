using System;
using TMPro;
using UnityEngine;

/// <summary>The account popup: guest or linked to Google, and the Google sign-in buttons. Opening and closing is pure UI.</summary>
public class AccountView : MonoBehaviour
{
    [SerializeField] private GameObject popup;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private GameObject googleButton;
    [SerializeField] private GameObject switchButton;
    [SerializeField] private GameObject cancelButton;

    public event Action Opened;
    public event Action GoogleClicked;
    public event Action SwitchClicked;
    public event Action CancelClicked;

    private void Awake() => popup.SetActive(false);

    public void Show(string status, bool showGoogle, bool showSwitch, bool showCancel)
    {
        statusText.text = status;
        googleButton.SetActive(showGoogle);
        switchButton.SetActive(showSwitch);
        cancelButton.SetActive(showCancel);
    }

    // Button OnClick methods below.

    public void Open()
    {
        popup.SetActive(true);
        Opened?.Invoke();
    }

    public void Close() => popup.SetActive(false);

    public void ContinueWithGoogle() => GoogleClicked?.Invoke();

    public void SwitchPlayer() => SwitchClicked?.Invoke();

    public void Cancel() => CancelClicked?.Invoke();
}
