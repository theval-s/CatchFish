using System;
using System.Text;
using VContainer.Unity;

/// <summary>Shows the run summary once the backend has the results and takes the player back to the hub.</summary>
public class FishingResultsPresenter : IStartable, IDisposable
{
    private readonly FishingResultsView _view;
    private readonly FishingService _fishing;
    private readonly ConfigService _config;
    private readonly SceneLoader _scenes;

    public FishingResultsPresenter(FishingResultsView view, FishingService fishing, ConfigService config,
        SceneLoader scenes)
    {
        _view = view;
        _fishing = fishing;
        _config = config;
        _scenes = scenes;
    }

    public void Start()
    {
        _view.Hide();
        _fishing.RunCompleted += OnRunCompleted;
        _view.BackToHubClicked += OnBackToHub;
    }

    public void Dispose()
    {
        _fishing.RunCompleted -= OnRunCompleted;
        _view.BackToHubClicked -= OnBackToHub;
    }

    private void OnRunCompleted(bool saved)
    {
        var session = _fishing.Session;
        var collected = DescribeCollected(session);
        if (!saved)
        {
            collected = "Not saved: the server didn't accept the run.\n" + collected;
        }

        _view.Show(session.DepthReached, session.Caught, session.Shot, session.Lost, session.Earnings, collected);
    }

    private void OnBackToHub() => _ = _scenes.LoadAsync(SceneNames.Hub);

    private string DescribeCollected(FishingSessionState session)
    {
        var text = new StringBuilder();
        foreach (var pair in session.ShotFish)
        {
            var fish = _config.GetFish(pair.Key);
            if (fish.IsCollectible)
            {
                text.Append($" {fish.Name} x{pair.Value}");
            }
        }

        return text.Length > 0 ? "Collected:" + text : "";
    }
}
