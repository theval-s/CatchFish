using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VContainer.Unity;

/// <summary>Shows the run summary once the backend has the results and takes the player back to the hub.</summary>
public class FishingResultsPresenter : IStartable, IDisposable
{
    // After the first run the player goes back to the hub by themselves: there's nothing else to do yet.
    private const int FirstRunReturnMs = 3000;

    private readonly FishingResultsView _view;
    private readonly FishingService _fishing;
    private readonly ConfigService _config;
    private readonly SceneLoader _scenes;
    private bool _leaving;

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
        var notes = new StringBuilder();
        if (!saved)
        {
            notes.AppendLine("Not saved: the server didn't accept the run.");
        }
        if (session.BittenBy != null)
        {
            var shark = _config.GetFish(session.BittenBy);
            notes.AppendLine($"A {shark.Name} took the hook! Fine: {-shark.Value} DR");
        }
        if (session.Destroyed > 0)
        {
            notes.AppendLine($"Blown up: {session.Destroyed} fish");
        }
        var shocked = session.ShockedFish.Values.Sum();
        if (shocked > 0)
        {
            notes.AppendLine($"Shocked by an eel: {shocked} fish at half price");
        }
        notes.Append(DescribeCollected(session));
        if (session.IsFirstRun)
        {
            notes.Append("\nHeading back to the hub...");
            _ = ReturnToHubAsync();
        }

        _view.Show(session.DepthReached, session.Caught, session.Shot, session.Lost, session.Earnings, notes.ToString().TrimEnd());
    }

    private void OnBackToHub()
    {
        // The button and the first-run timer can both fire.
        if (_leaving)
        {
            return;
        }

        _leaving = true;
        _ = _scenes.LoadAsync(SceneNames.Hub);
    }

    private async Task ReturnToHubAsync()
    {
        await Task.Delay(FirstRunReturnMs);
        OnBackToHub();
    }

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
