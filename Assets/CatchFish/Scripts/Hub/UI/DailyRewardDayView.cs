using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum DailyRewardDayState
{
    Claimed, // earlier in this round of the track
    Next,    // the one the next claim gives
    Later,
}

/// <summary>One day of the daily reward track in the popup.</summary>
public class DailyRewardDayView : MonoBehaviour
{
    [SerializeField] private TMP_Text dayText;
    [SerializeField] private TMP_Text rewardText;
    [SerializeField] private Image background;

    [Header("Background by state")]
    [SerializeField] private Color claimedColor = new Color(0.55f, 0.55f, 0.55f, 0.6f);
    [SerializeField] private Color nextColor = new Color(1f, 0.8f, 0.25f, 1f);
    [SerializeField] private Color laterColor = new Color(1f, 1f, 1f, 0.85f);

    public void Show(string day, string reward, DailyRewardDayState state)
    {
        dayText.text = day;
        rewardText.text = state == DailyRewardDayState.Claimed ? "Claimed" : reward;
        background.color = state == DailyRewardDayState.Claimed ? claimedColor
            : state == DailyRewardDayState.Next ? nextColor
            : laterColor;
    }
}
