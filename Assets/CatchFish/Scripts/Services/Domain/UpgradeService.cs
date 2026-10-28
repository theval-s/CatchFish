using System;

/// <summary>Fishing gear upgrades, bought with dirty rubles.</summary>
public class UpgradeService
{
    private readonly ConfigService _config;
    private readonly ProfileSyncService _profile;
    private readonly ResourceService _resources;

    public UpgradeService(ConfigService config, ProfileSyncService profile, ResourceService resources)
    {
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

    public bool TryBuy(string gearId)
    {
        if (IsMaxed(gearId) || !_resources.TrySpend(ResourceIds.DirtyRubles, NextCost(gearId)))
        {
            return false;
        }

        _profile.State.Upgrades.GearLevels[gearId] = GetLevel(gearId) + 1;
        _profile.Save();
        Changed?.Invoke();
        return true;
    }
}
