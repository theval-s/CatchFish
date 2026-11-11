using System;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// The daily login reward: which day of the track comes next, whether it can be claimed yet, and
/// claiming it. The server decides by its own clock; this reads the profile and the synced server
/// time only to show the right thing.
/// </summary>
public class DailyRewardService
{
    private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly IApiClient _api;
    private readonly ConfigService _config;
    private readonly ProfileSyncService _profile;
    private readonly ServerTimeService _time;
    private int _announcedDay = -1;

    public DailyRewardService(IApiClient api, ConfigService config, ProfileSyncService profile, ServerTimeService time)
    {
        _api = api;
        _config = config;
        _profile = profile;
        _time = time;
    }

    public event Action Changed;

    /// <summary>One reward per day: resource id → amount.</summary>
    public IReadOnlyList<Dictionary<string, int>> Days => Track.Days;

    /// <summary>Index of the reward the next claim gives. Missed days don't move it.</summary>
    public int NextDay => Track.Days.Count == 0 ? 0 : Math.Max(Daily.NextDay, 0) % Track.Days.Count;

    /// <summary>Once per server calendar day.</summary>
    public bool CanClaim => Track.Days.Count > 0 && Today > Daily.LastClaimDay;

    /// <summary>Until the server's next calendar day starts, when a claimed reward's next one opens.</summary>
    public TimeSpan UntilNextDay => DayStartUtc(Today + 1) - _time.Now;

    private DailyRewardTrack Track => _config.Config.DailyRewards;

    private DailyRewardState Daily => _profile.State.Daily;

    // The same day number as the server's: days since 1970 in the track's time zone.
    private int Today => (int)Math.Floor((_time.Now - Epoch).TotalDays + Track.UtcOffsetHours / 24.0);

    /// <summary>
    /// True the first time it's asked on a server day with a reward waiting: the hub then opens the
    /// popup. Later the same day (the player closed it, or came back from fishing) the badge is enough.
    /// </summary>
    public bool TryAnnounce()
    {
        if (!CanClaim || _announcedDay == Today)
        {
            return false;
        }

        _announcedDay = Today;
        return true;
    }

    public async Task<bool> TryClaimAsync()
    {
        if (!CanClaim || !await _profile.TryApplyAsync(_api.ClaimDailyRewardAsync()))
        {
            return false;
        }

        Changed?.Invoke();
        return true;
    }

    private DateTime DayStartUtc(int day) => Epoch.AddDays(day).AddHours(-Track.UtcOffsetHours);
}
