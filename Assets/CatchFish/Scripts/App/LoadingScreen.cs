using System;
using TMPro;
using UnityEngine;

/// <summary>
/// Full-screen overlay that blocks input while <see cref="SceneLoader"/> switches scenes.
/// Lives in RootScope.prefab, so it survives scene changes; the panel starts visible to cover app startup too.
/// At startup it can also show why the server couldn't be used, with a way forward.
/// </summary>
public class LoadingScreen : MonoBehaviour
{
    [SerializeField] private GameObject panel;

    [Header("Startup error")]
    [SerializeField] private GameObject errorPanel;
    [SerializeField] private TMP_Text errorText;
    [SerializeField] private GameObject startOverButton;

    public event Action RetryClicked;
    public event Action StartOverClicked;

    private void Awake() => HideError();

    public void Show() => panel.SetActive(true);

    public void Hide() => panel.SetActive(false);

    public void ShowError(string message, bool canStartOver)
    {
        errorText.text = message;
        startOverButton.SetActive(canStartOver);
        errorPanel.SetActive(true);
    }

    public void HideError() => errorPanel.SetActive(false);

    // Button OnClick methods below.

    public void Retry() => RetryClicked?.Invoke();

    public void StartOver() => StartOverClicked?.Invoke();
}
