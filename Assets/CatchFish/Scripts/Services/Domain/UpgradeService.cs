using System;
using System.Threading.Tasks;

/// <summary>Fishing gear upgrades, bought with dirty rubles.</summary>
public class UpgradeService
{
    private readonly IApiClient _api;
    private readonly ConfigService _config;
    private readonly ProfileSyncService _profile;
    private readonly ResourceService _resources;

    public UpgradeService(IApiClient api, ConfigService config, ProfileSyncService profile, ResourceService resources)
    {
        _api = api;
        _config = config;
        _profile = profile;
        _resources = resources;
    }

    public event Action Changed;

    public int GetLevel(string gearId) =>
        _profile.State.Upgrades.GearLevels.TryGetValue(gearId, out var level) ? level : 0;

    public bool IsMaxed(string gearId) => GetLevel(gearId) >= _config.GetGear(gearId).MaxLevel;

    public float Value(string gearId) => _config.GetGear(gearId).ValueAt(GetLevel(gearId));

    public float NextValue(string gearId) => _config.GetGear(gearId).ValueAt(GetLevel(gearId) + 1);

    public int NextCost(string gearId) => _config.GetGear(gearId).CostAt(GetLevel(gearId));

    public bool CanBuy(string gearId) => !IsMaxed(gearId) && _resources.Has(ResourceIds.DirtyRubles, NextCost(gearId));

    public async Task<bool> TryBuyAsync(string gearId)
    {
        // Checked here too, so a purchase that can't go through isn't sent to the backend.
        if (!CanBuy(gearId) || !await _profile.TryApplyAsync(_api.BuyGearAsync(gearId)))
        {
            return false;
        }

        Changed?.Invoke();
        return true;
    }
}
