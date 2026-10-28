using System;
using System.Text;
using VContainer.Unity;

/// <summary>Shows the run summary once the results are saved and takes the player back to the hub.</summary>
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

    private void OnRunCompleted()
    {
        var session = _fishing.Session;
        _view.Show(session.DepthReached, session.Caught, session.Shot, session.Lost, session.Earnings,
            DescribeCollected(session));
    }

    private void OnBackToHub() => _ = _scenes.LoadAsync(SceneNames.Hub);

    private string DescribeCollected(FishingSessionState session)
    {
        if (session.CollectedFish.Count == 0)
        {
            return "";
        }

        var text = new StringBuilder("Collected:");
        foreach (var pair in session.CollectedFish)
        {
            text.Append($" {_config.GetFish(pair.Key).Name} x{pair.Value}");
        }

        return text.ToString();
    }
}
