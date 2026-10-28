using System;
using TMPro;
using UnityEngine;

/// <summary>Run summary shown after the last fish is shot or lost.</summary>
public class FishingResultsView : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text depthText;
    [SerializeField] private TMP_Text caughtText;
    [SerializeField] private TMP_Text shotText;
    [SerializeField] private TMP_Text lostText;
    [SerializeField] private TMP_Text earnedText;
    [SerializeField] private TMP_Text collectedText;

    public event Action BackToHubClicked;

    public void Hide() => panel.SetActive(false);

    public void Show(float depth, int caught, int shot, int lost, int earned, string collected)
    {
        panel.SetActive(true);
        depthText.text = $"Depth reached: {depth:0} m";
        caughtText.text = $"Fish caught: {caught}";
        shotText.text = $"Fish shot: {shot}";
        lostText.text = $"Fish lost: {lost}";
        earnedText.text = $"Earned: {earned} DR";
        collectedText.text = collected;
    }

    // Button OnClick
    public void BackToHub() => BackToHubClicked?.Invoke();
}
