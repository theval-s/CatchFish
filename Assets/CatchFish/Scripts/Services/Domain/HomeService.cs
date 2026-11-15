using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// Home objects: each upgrade step costs laundered rubles (gems can pay the rest), and when a level's
/// steps are done its quests unlock the next level.
/// </summary>
public class HomeService
{
    private static readonly HomeObjectState Untouched = new HomeObjectState();

    private readonly IApiClient _api;
    private readonly ConfigService _config;
    private readonly ProfileSyncService _profile;
    private readonly ResourceService _resources;

    public HomeService(IApiClient api, ConfigService config, ProfileSyncService profile, ResourceService resources)
    {
        _api = api;
        _config = config;
        _profile = profile;
        _resources = resources;
    }

    public event Action Changed;

    public IReadOnlyList<HomeObjectData> Objects => _config.Config.Home;

    public HomeObjectState Get(HomeObjectData data) =>
        _profile.State.Home.TryGetValue(data.Id, out var state) ? state : Untouched;

    public HomeLevelData CurrentLevel(HomeObjectData data) => data.Levels[Math.Min(Get(data).Level, data.MaxLevel)];

    public bool IsMaxed(HomeObjectData data) => Get(data).Level >= data.MaxLevel;

    public bool StepsDone(HomeObjectData data) => Get(data).Steps >= CurrentLevel(data).RequiredProgressSteps;

    /// <summary>Gems for what the laundered rubles don't cover; 0 when they do.</summary>
    public int GemsForShortfall(HomeObjectData data)
    {
        var missing = CurrentLevel(data).ProgressCost - _resources.Get(ResourceIds.LaunderedRubles);
        var perGem = Math.Max(1, _config.Config.LrPerGem);
        return missing <= 0 ? 0 : (missing + perGem - 1) / perGem;
    }

    public bool CanUpgrade(HomeObjectData data) => CanBuyStep(data) &&
        _resources.Has(ResourceIds.LaunderedRubles, CurrentLevel(data).ProgressCost);

    public bool CanUpgradeWithGems(HomeObjectData data) => CanBuyStep(data) && GemsForShortfall(data) > 0 &&
        _resources.Has(ResourceIds.Gems, GemsForShortfall(data));

    /// <summary>Prerequisite quests of the current level that aren't completed yet.</summary>
    public IReadOnlyList<QuestData> MissingQuests(HomeObjectData data) => CurrentLevel(data).PrerequisiteQuests
        .Where(id => !_profile.State.Quests.Completed.Contains(id))
        .Select(id => _config.GetQuest(id))
        .Where(quest => quest != null)
        .ToList();

    public bool CanLevelUp(HomeObjectData data) => !IsMaxed(data) && StepsDone(data) && MissingQuests(data).Count == 0;

    public Task<bool> TryUpgradeAsync(HomeObjectData data, bool useGems) =>
        TryApplyAsync(useGems ? CanUpgradeWithGems(data) : CanUpgrade(data), () => _api.UpgradeHomeAsync(data.Id, useGems));

    public Task<bool> TryLevelUpAsync(HomeObjectData data) =>
        TryApplyAsync(CanLevelUp(data), () => _api.LevelUpHomeAsync(data.Id));

    private bool CanBuyStep(HomeObjectData data) => !IsMaxed(data) && !StepsDone(data);

    private async Task<bool> TryApplyAsync(bool allowed, Func<Task<GameState>> request)
    {
        if (!allowed || !await _profile.TryApplyAsync(request()))
        {
            return false;
        }

        Changed?.Invoke();
        return true;
    }
}
