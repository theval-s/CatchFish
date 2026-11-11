using System;
using System.Linq;
using VContainer.Unity;

/// <summary>
/// Fills the daily reward popup from the track, opens it when a reward is waiting, claims it,
/// and keeps the "next reward in" countdown current.
/// </summary>
public class DailyRewardPresenter : IStartable, ITickable, IDisposable
{
    private readonly DailyRewardView _view;
    private readonly DailyRewardService _daily;
    private readonly FishingService _fishing;
    private readonly ConfigService _config;
    private int _shownSeconds = -1;
    private bool _shownCanClaim;

    public DailyRewardPresenter(DailyRewardView view, DailyRewardService daily, FishingService fishing,
        ConfigService config)
    {
        _view = view;
        _daily = daily;
        _fishing = fishing;
        _config = config;
    }

    public void Start()
    {
        _view.ClaimClicked += OnClaim;
        _daily.Changed += Refresh;
        Refresh();

        // Opens on the first hub visit of the day with a reward waiting, also after a run that
        // crossed midnight. Not before the first run: then the hub is about one thing, "Go fishing".
        if (!_fishing.IsFirstRun && _daily.TryAnnounce())
        {
            _view.Open();
        }
    }

    public void Dispose()
    {
        _view.ClaimClicked -= OnClaim;
        _daily.Changed -= Refresh;
    }

    // The countdown changes every second, and a new day can open a reward while the hub is open.
    public void Tick()
    {
        if (_daily.CanClaim != _shownCanClaim)
        {
            Refresh();
        }
        else if ((int)_daily.UntilNextDay.TotalSeconds != _shownSeconds)
        {
            ShowStatus();
        }
    }

    private void OnClaim() => _ = _daily.TryClaimAsync();

    private void Refresh()
    {
        _view.ClearDays();
        for (var i = 0; i < _daily.Days.Count; i++)
        {
            var state = i < _daily.NextDay ? DailyRewardDayState.Claimed
                : i == _daily.NextDay ? DailyRewardDayState.Next
                : DailyRewardDayState.Later;
            _view.AddDay().Show($"Day {i + 1}", DescribeReward(i), state);
        }

        ShowStatus();
    }

    private void ShowStatus()
    {
        _shownCanClaim = _daily.CanClaim;
        _shownSeconds = (int)_daily.UntilNextDay.TotalSeconds;
        _view.ShowStatus(_shownCanClaim ? $"Day {_daily.NextDay + 1} reward is ready!" : $"Next reward in {Countdown()}",
            _shownCanClaim);
    }

    private string DescribeReward(int day) =>
        string.Join("\n", _daily.Days[day].Select(pair => $"{pair.Value} {ResourceNames.Get(pair.Key, _config)}"));

    private string Countdown()
    {
        var left = TimeSpan.FromSeconds(Math.Max(_shownSeconds, 0));
        return left.TotalHours >= 1 ? $"{(int)left.TotalHours}h {left.Minutes:00}m" : $"{left.Minutes}m {left.Seconds:00}s";
    }
}
