using System;
using System.Collections.Generic;

/// <summary>
/// The player's profile: everything that is saved and synced with the server.
/// Split into parts that belong to different services.
/// </summary>
public class GameState
{
    public PlayerState Player { get; set; } = new PlayerState();
    public QuestProgressState Quests { get; set; } = new QuestProgressState();
    public UpgradeProgressState Upgrades { get; set; } = new UpgradeProgressState();

    /// <summary>Server time of the last save.</summary>
    public DateTime SavedAt { get; set; }

    public static GameState CreateNew(GameConfig config)
    {
        var state = new GameState();
        state.Quests.Active.AddRange(config.StartingQuests);
        return state;
    }
}

/// <summary>Currencies and the fish collection.</summary>
public class PlayerState
{
    public int MatchesPlayed { get; set; }
    public int Gems { get; set; }
    public int LaunderedRubles { get; set; }
    public int DirtyRubles { get; set; }
    public int Reputation { get; set; }

    /// <summary>Fish id → how many collectible fish of that kind the player has.</summary>
    public Dictionary<string, int> FishCollection { get; set; } = new Dictionary<string, int>();
}

/// <summary>Which quests are open and which are done.</summary>
public class QuestProgressState
{
    public List<int> Active { get; set; } = new List<int>();
    public List<int> Completed { get; set; } = new List<int>();
}

/// <summary>Levels of everything the player has upgraded.</summary>
public class UpgradeProgressState
{
    /// <summary>Gear id → level.</summary>
    public Dictionary<string, int> GearLevels { get; set; } = new Dictionary<string, int>();
}
