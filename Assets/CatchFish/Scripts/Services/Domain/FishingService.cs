using System;
using System.Threading.Tasks;

/// <summary>
/// Starts fishing runs and reports their results to the backend. The minigame itself
/// runs in <see cref="FishingController"/> and fills in <see cref="Session"/>.
/// </summary>
public class FishingService
{
    private readonly IApiClient _api;
    private readonly UpgradeService _upgrades;
    private readonly ProfileSyncService _profile;

    public FishingService(IApiClient api, UpgradeService upgrades, ProfileSyncService profile)
    {
        _api = api;
        _upgrades = upgrades;
        _profile = profile;
    }

    /// <summary>Raised when a run has ended: true if the backend took the results.</summary>
    public event Action<bool> RunCompleted;

    public FishingSessionState Session { get; private set; }

    /// <summary>The first run is easier, and until it's done the hub offers nothing but fishing.</summary>
    public bool IsFirstRun => _profile.State.Player.RunsCompleted == 0;

    /// <summary>The backend records the run first, so it can check the results later.</summary>
    public async Task<bool> TryStartRunAsync()
    {
        if (!await _profile.TryApplyAsync(_api.StartRunAsync()))
        {
            return false;
        }

        Session = new FishingSessionState
        {
            IsFirstRun = IsFirstRun,
            MaxDepth = _upgrades.Value(GearIds.LineLength),
            Durability = (int)_upgrades.Value(GearIds.LineDurability),
            Capacity = (int)_upgrades.Value(GearIds.HookCapacity),
            ReelSpeed = _upgrades.Value(GearIds.ReelSpeed),
        };
        return true;
    }

    /// <summary>Sends the shot fish; the backend pays for them and adds the collectible ones.</summary>
    public async Task CompleteRunAsync()
    {
        if (Session == null || Session.Phase == FishingPhase.Results)
        {
            return;
        }

        Session.Phase = FishingPhase.Results;

        var result = new RunResult
        {
            RunId = _profile.State.ActiveRun?.Id,
            ShotFish = Session.ShotFish,
        };
        var saved = await _profile.TryApplyAsync(_api.CompleteRunAsync(result));
        RunCompleted?.Invoke(saved);
    }
}
