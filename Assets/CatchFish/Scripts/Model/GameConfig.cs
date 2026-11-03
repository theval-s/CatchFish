using System;
using System.Collections.Generic;

/// <summary>
/// Game content shared by all players. Comes from the server so it can be changed
/// without a client update; read-only on the client.
/// </summary>
public class GameConfig
{
    public List<FishType> Fish { get; set; } = new List<FishType>();
    public List<QuestData> Quests { get; set; } = new List<QuestData>();
    public List<int> StartingQuests { get; set; } = new List<int>();
    public List<GearUpgradeData> Gear { get; set; } = new List<GearUpgradeData>();
}

/// <summary>One kind of fish.</summary>
public class FishType
{
    /// <summary>"f&lt;num&gt;", also used as the resource id in the fish collection.</summary>
    public string Id { get; set; }
    public string Name { get; set; }

    /// <summary>Dirty rubles for shooting it. Negative = a fish you should avoid.</summary>
    public int Value { get; set; }

    public float MinDepth { get; set; }
    public float MaxDepth { get; set; }
    public float SwimSpeed { get; set; }
    public float Size { get; set; }

    /// <summary>Relative spawn chance compared to other fish at the same depth.</summary>
    public float SpawnWeight { get; set; }

    /// <summary>Shot collectible fish go to the fish collection (used by quests).</summary>
    public bool IsCollectible { get; set; }

    public bool LivesAt(float depth) => depth >= MinDepth && depth <= MaxDepth;
}

/// <summary>A quest: collect the required resources, then complete it to unlock the next ones.</summary>
public class QuestData
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }

    /// <summary>Resource id (see <see cref="ResourceIds"/>) → amount. Spent on completion.</summary>
    public Dictionary<string, int> Requirements { get; set; } = new Dictionary<string, int>();

    /// <summary>Quests added to the player after this one is completed.</summary>
    public List<int> UnlocksId { get; set; } = new List<int>();
}

/// <summary>One fishing gear upgrade track: value grows linearly, price grows exponentially.</summary>
public class GearUpgradeData
{
    public string Id { get; set; }
    public string Title { get; set; }
    public string Unit { get; set; }
    public int MaxLevel { get; set; }
    public int BaseCost { get; set; }
    public float CostGrowth { get; set; }
    public float BaseValue { get; set; }
    public float ValuePerLevel { get; set; }

    public float ValueAt(int level) => BaseValue + ValuePerLevel * Math.Min(level, MaxLevel);

    public int CostAt(int level) => (int)Math.Round(BaseCost * Math.Pow(CostGrowth, level));
}

/// <summary>Ids of the gear tracks in <see cref="GameConfig.Gear"/>.</summary>
public static class GearIds
{
    public const string LineLength = "lineLength";
    public const string LineDurability = "lineDurability";
    public const string HookCapacity = "hookCapacity";
    public const string ReelSpeed = "reelSpeed";
}
