using System;
using System.Threading.Tasks;

/// <summary>
/// Energy: each run costs some, it comes back over time or for gems. The server counts the
/// regeneration; this repeats its arithmetic with the synced server time to show the meter.
/// </summary>
public class EnergyService
{
    private readonly IApiClient _api;
    private readonly ConfigService _config;
    private readonly ProfileSyncService _profile;
    private readonly ServerTimeService _time;
    private readonly ResourceService _resources;

    public EnergyService(IApiClient api, ConfigService config, ProfileSyncService profile, ServerTimeService time,
        ResourceService resources)
    {
        _api = api;
        _config = config;
        _profile = profile;
        _time = time;
        _resources = resources;
    }

    public event Action Changed;

    public int Max => Config.Max;
    public int RunCost => Config.RunCost;
    public int RefillGems => Config.RefillGems;

    public int Current => State.Amount >= Max ? State.Amount : (int)Math.Min(Max, State.Amount + Gained);

    public bool IsFull => Current >= Max;

    /// <summary>Until the next point comes back; zero when full.</summary>
    public TimeSpan UntilNext => IsFull ? TimeSpan.Zero : Interval - TimeSpan.FromTicks(Elapsed.Ticks % Interval.Ticks);

    public bool CanStartRun => Current >= RunCost;

    public bool CanRefill => !IsFull && _resources.Has(ResourceIds.Gems, RefillGems);

    private EnergyConfig Config => _config.Config.Energy;
    private EnergyState State => _profile.State.Energy;
    private TimeSpan Interval => TimeSpan.FromMinutes(Math.Max(1, Config.RegenMinutes));
    private TimeSpan Elapsed => _time.Now > State.UpdatedAt ? _time.Now - State.UpdatedAt : TimeSpan.Zero;
    private long Gained => Elapsed.Ticks / Interval.Ticks;

    public async Task<bool> TryRefillAsync()
    {
        if (!CanRefill || !await _profile.TryApplyAsync(_api.RefillEnergyAsync()))
        {
            return false;
        }

        NotifyChanged();
        return true;
    }

    /// <summary>For actions that spend energy elsewhere (starting a run).</summary>
    public void NotifyChanged() => Changed?.Invoke();
}
