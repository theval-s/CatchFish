using TMPro;
using UnityEngine;

/// <summary>
/// Shows the corner HUD
/// </summary>
public class GameHud : MonoBehaviour
{
    [SerializeField] private GameManager game;

    [Header("Panels")]
    [SerializeField] private GameObject hubPanel;
    [SerializeField] private GameObject runPanel;
    [SerializeField] private GameObject resultsPanel;

    //As far as I understand this will help with localizing, probably can be done better
    [Header("Hub")]
    [SerializeField] private TMP_Text moneyText;
    [SerializeField] private TMP_Text bestDepthText;
    [SerializeField] private UpgradeButton[] upgradeButtons;

    [Header("Run")]
    [SerializeField] private TMP_Text phaseText;
    [SerializeField] private TMP_Text depthText;
    [SerializeField] private TMP_Text hitsText;
    [SerializeField] private TMP_Text catchText;
    [SerializeField] private TMP_Text earnedText;

    [Header("Results")]
    [SerializeField] private TMP_Text resultDepthText;
    [SerializeField] private TMP_Text resultCaughtText;
    [SerializeField] private TMP_Text resultShotText;
    [SerializeField] private TMP_Text resultLostText;
    [SerializeField] private TMP_Text resultEarnedText;

    [Header("Shot popups")]
    [SerializeField] private ShotPopup popupPrefab;
    [SerializeField] private RectTransform popupLayer;

    private Camera _camera;
    private GameState? _shownState;
    private long _shownMoney = -1;

    private void Awake()
    {
        _camera = Camera.main;
        foreach (var button in upgradeButtons)
        {
            button.Init(game);
        }
    }

    private void OnEnable() => game.FishShot += OnFishShot;

    private void OnDisable() => game.FishShot -= OnFishShot;

    private void Update()
    {
        var state = game.State;
        if (state != _shownState)
        {
            _shownState = state;
            ShowPanelFor(state);
        }

        switch (state)
        {
            case GameState.Hub:
                // Money is the only thing that changes in the hub (buying spends it).
                if (game.Progression.Money != _shownMoney)
                {
                    RefreshHub();
                }

                break;
            case GameState.Descent:
            case GameState.Ascent:
            case GameState.Shooting:
                RefreshRun();
                break;
        }
    }

    private void ShowPanelFor(GameState state)
    {
        hubPanel.SetActive(state == GameState.Hub);
        runPanel.SetActive(state == GameState.Descent || state == GameState.Ascent || state == GameState.Shooting);
        resultsPanel.SetActive(state == GameState.Results);

        if (state == GameState.Hub)
        {
            RefreshHub();
        }
        else if (state == GameState.Results)
        {
            FillResults();
        }
    }

    private void RefreshHub()
    {
        var progression = game.Progression;
        _shownMoney = progression.Money;
        moneyText.text = $"${progression.Money}";
        bestDepthText.text = $"Best depth: {progression.BestDepth:0} m";

        foreach (var button in upgradeButtons)
        {
            button.Refresh(progression);
        }
    }

    private void RefreshRun()
    {
        var hook = game.Hook;
        phaseText.text = PhaseHint(game.State);
        depthText.text = $"Depth: {hook.Depth:0} / {game.Progression.MaxDepth:0} m";
        hitsText.text = $"Hits left: {Mathf.Max(0, hook.HitsLeft)}";
        catchText.text = $"On hook: {hook.Caught.Count} / {hook.Capacity}";
        earnedText.text = $"Earned: ${game.Run.Earnings}";
    }

    private void FillResults()
    {
        var run = game.Run;
        resultDepthText.text = $"Depth reached: {run.DepthReached:0} m";
        resultCaughtText.text = $"Fish caught: {run.Caught}";
        resultShotText.text = $"Fish shot: {run.Shot}";
        resultLostText.text = $"Fish lost: {run.Lost}";
        resultEarnedText.text = $"Earned: ${run.Earnings}";
    }

    private void OnFishShot(Vector2 worldPosition, int value)
    {
        var popup = Instantiate(popupPrefab, popupLayer);
        popup.transform.position = _camera.WorldToScreenPoint(worldPosition);
        popup.Show(value >= 0 ? $"+${value}" : $"-${-value}");
    }

    private static string PhaseHint(GameState state)
    {
        switch (state)
        {
            case GameState.Descent: return "Dodge the fish!";
            case GameState.Ascent: return "Catch the fish!";
            default: return "Shoot them!";
        }
    }
}
