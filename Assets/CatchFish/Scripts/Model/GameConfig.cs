using System;
using System.Collections.Generic;

/// <summary>
/// Game content shared by all players. Lives on the server, where the dev API can change it
/// without a client update; read-only on the client.
/// </summary>
public class GameConfig
{
    /// <summary>Goes up with every change made through the dev API.</summary>
    public int Version { get; set; }

    public List<FishType> Fish { get; set; } = new List<FishType>();
    public List<QuestData> Quests { get; set; } = new List<QuestData>();
    public List<int> StartingQuests { get; set; } = new List<int>();
    public List<GearUpgradeData> Gear { get; set; } = new List<GearUpgradeData>();
    public DailyRewardTrack DailyRewards { get; set; } = new DailyRewardTrack();
    public EnergyConfig Energy { get; set; } = new EnergyConfig();
    public SchoolConfig School { get; set; } = new SchoolConfig();
    public List<HomeObjectData> Home { get; set; } = new List<HomeObjectData>();

    /// <summary>What one gem pays for when a home upgrade is short of laundered rubles.</summary>
    public int LrPerGem { get; set; } = 50;

    /// <summary>Shown in the hub before the first run.</summary>
    public List<DialogLine> IntroDialog { get; set; } = new List<DialogLine>();
}

/// <summary>One line of a story dialog; an empty speaker is the narrator.</summary>
public class DialogLine
{
    public string Speaker { get; set; } = "";
    public string Text { get; set; } = "";
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

    /// <summary>"" for a normal fish, or one of <see cref="FishKinds"/>.</summary>
    public string Kind { get; set; } = "";

    public bool IsKind(string kind) => Kind == kind;

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

    /// <summary>The server opens it for every player who hasn't done it, not only through unlocks.</summary>
    public bool AutoStart { get; set; }
}

/// <summary>One reward per login day; after the last day the track starts over. Missed days cost nothing.</summary>
public class DailyRewardTrack
{
    /// <summary>Where the server's calendar day starts: +3 = midnight in Moscow.</summary>
    public int UtcOffsetHours { get; set; }

    /// <summary>One reward per day: resource id → amount.</summary>
    public List<Dictionary<string, int>> Days { get; set; } = new List<Dictionary<string, int>>();
}

/// <summary>Each run costs energy; it comes back over time or for gems.</summary>
public class EnergyConfig
{
    public int Max { get; set; } = 5;
    public int RegenMinutes { get; set; } = 20;
    public int RunCost { get; set; } = 1;
    public int RefillGems { get; set; } = 10;
}

/// <summary>The school: classes that launder dirty rubles and earn reputation every minute.</summary>
public class SchoolConfig
{
    /// <summary>The most minutes the server counts between two requests.</summary>
    public int OfflineCapMinutes { get; set; }

    public List<SchoolClassData> Classes { get; set; } = new List<SchoolClassData>();
}

/// <summary>A school class: hire its tutor, then recruit students.</summary>
public class SchoolClassData
{
    public string Id { get; set; }
    public string Title { get; set; }
    public Dictionary<string, int> TutorCost { get; set; } = new Dictionary<string, int>();

    /// <summary>The price of the first student; each next one costs <see cref="StudentCostGrowth"/> times more.</summary>
    public Dictionary<string, int> StudentCost { get; set; } = new Dictionary<string, int>();

    public float StudentCostGrowth { get; set; } = 1f;
    public int MaxStudents { get; set; }

    // Per minute, while the class has its tutor.
    public int LaunderBase { get; set; }
    public int LaunderPerStudent { get; set; }
    public int RepPerStudent { get; set; }

    public Dictionary<string, int> StudentCostAt(int students)
    {
        var cost = new Dictionary<string, int>();
        foreach (var pair in StudentCost)
        {
            cost[pair.Key] = (int)Math.Round(pair.Value * Math.Pow(StudentCostGrowth, students));
        }

        return cost;
    }

    public int LaunderAt(int students) => LaunderBase + LaunderPerStudent * students;

    public int RepAt(int students) => RepPerStudent * students;
}

/// <summary>An upgradable home object (wife, son, pug, apartment). Its last level is the top one.</summary>
public class HomeObjectData
{
    public string Id { get; set; }
    public string Title { get; set; }
    public List<HomeLevelData> Levels { get; set; } = new List<HomeLevelData>();

    public int MaxLevel => Levels.Count - 1;
}

/// <summary>One level of a home object: the steps that finish it and the quests its level-up needs.</summary>
public class HomeLevelData
{
    public string Title { get; set; }
    public string Description { get; set; }
    public int RequiredProgressSteps { get; set; }

    /// <summary>Laundered rubles per step.</summary>
    public int ProgressCost { get; set; }

    public List<int> PrerequisiteQuests { get; set; } = new List<int>();
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

/// <summary>Special fish (<see cref="FishType.Kind"/>).</summary>
public static class FishKinds
{
    /// <summary>Shooting it blows up the fish around it.</summary>
    public const string Bomb = "bomb";

    /// <summary>Hooking it shocks the fish already on the hook: they pay half.</summary>
    public const string Eel = "eel";

    /// <summary>Takes the hook with the whole catch; its negative value is the fine.</summary>
    public const string Shark = "shark";
}
