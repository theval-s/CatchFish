using System;
using System.Collections.Generic;

public enum UpgradeType
{
    LineLength,
    LineDurability,
    HookCapacity,
    ReelSpeed,
}

/// <summary>One upgrade track: value grows linearly, price grows exponentially.</summary>
public class Upgrade
{
    public Upgrade(string title, string unit, int maxLevel, int baseCost, float costGrowth, float baseValue, float valuePerLevel)
    {
        Title = title;
        Unit = unit;
        MaxLevel = maxLevel;
        BaseCost = baseCost;
        CostGrowth = costGrowth;
        BaseValue = baseValue;
        ValuePerLevel = valuePerLevel;
    }

    public string Title { get; }
    public string Unit { get; }
    public int MaxLevel { get; }
    public int BaseCost { get; }
    public float CostGrowth { get; }
    public float BaseValue { get; }
    public float ValuePerLevel { get; }
    public int Level { get; set; }

    public bool IsMaxed => Level >= MaxLevel;
    public float Value => BaseValue + ValuePerLevel * Level;
    public float NextValue => BaseValue + ValuePerLevel * Math.Min(Level + 1, MaxLevel);
    public int NextCost => (int)Math.Round(BaseCost * Math.Pow(CostGrowth, Level));
}

/// <summary>
/// Player money and upgrades. Plain C# on purpose: this is the part that will move
/// to the authoritative server later, so keep game rules about money in here only.
/// </summary>
public class Progression
{
    private readonly Dictionary<UpgradeType, Upgrade> _upgrades = new Dictionary<UpgradeType, Upgrade>
    {
        //                                             title             unit  max cost growth base perLvl
        [UpgradeType.LineLength] = new Upgrade("Line length", "m", 10, 60, 1.6f, 50f, 35f),
        [UpgradeType.LineDurability] = new Upgrade("Sinker durability", " hits", 5, 150, 2.0f, 0f, 1f),
        [UpgradeType.HookCapacity] = new Upgrade("Hook capacity", " fish", 10, 80, 1.55f, 6f, 3f),
        [UpgradeType.ReelSpeed] = new Upgrade("Reel speed", " m/s", 8, 100, 1.7f, 7f, 1.5f),
    };

    public long Money { get; private set; }
    public float BestDepth { get; private set; }
    public IEnumerable<KeyValuePair<UpgradeType, Upgrade>> Upgrades => _upgrades;

    public Upgrade GetUpgrade(UpgradeType type) => _upgrades[type];

    public float MaxDepth => _upgrades[UpgradeType.LineLength].Value;
    public int Durability => (int)_upgrades[UpgradeType.LineDurability].Value;
    public int Capacity => (int)_upgrades[UpgradeType.HookCapacity].Value;
    public float ReelSpeed => _upgrades[UpgradeType.ReelSpeed].Value;

    public bool TryBuy(UpgradeType type)
    {
        var upgrade = _upgrades[type];
        if (upgrade.IsMaxed || Money < upgrade.NextCost)
        {
            return false;
        }

        Money -= upgrade.NextCost;
        upgrade.Level++;
        return true;
    }

    public void AddRunResult(int earnings, float depthReached)
    {
        Money = Math.Max(0, Money + earnings);
        BestDepth = Math.Max(BestDepth, depthReached);
    }
}
