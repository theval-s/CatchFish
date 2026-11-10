using System;
using System.Collections.Generic;

/// <summary>
/// The player's profile. The backend owns it: the client gets the whole profile back after every
/// action and never sends it. Split into parts that belong to different services.
/// </summary>
public class GameState
{
    public PlayerState Player { get; set; } = new PlayerState();
    public QuestProgressState Quests { get; set; } = new QuestProgressState();
    public UpgradeProgressState Upgrades { get; set; } = new UpgradeProgressState();

    /// <summary>The fishing run the backend expects results for, or null.</summary>
    public ActiveRunState ActiveRun { get; set; }

    /// <summary>Server time of the last change.</summary>
    public DateTime SavedAt { get; set; }

    public static GameState CreateNew(GameConfig config)
    {
        var state = new GameState();
        state.Quests.Active.AddRange(config.StartingQuests);
        return state;
    }
}

/// <summary>Currencies, the fish collection and how many fishing runs were finished.</summary>
public class PlayerState
{
    public int RunsCompleted { get; set; }
    public int Gems { get; set; }
    public int LaunderedRubles { get; set; }
    public int DirtyRubles { get; set; }
    public int Reputation { get; set; }

    /// <summary>Fish id → how many collectible fish of that kind the player has.</summary>
    public Dictionary<string, int> FishCollection { get; set; } = new Dictionary<string, int>();

    /// <summary>A currency or collectible fish by resource id (see <see cref="ResourceIds"/>).</summary>
    public int GetResource(string id)
    {
        switch (id)
        {
            case ResourceIds.DirtyRubles: return DirtyRubles;
            case ResourceIds.LaunderedRubles: return LaunderedRubles;
            case ResourceIds.Reputation: return Reputation;
            case ResourceIds.Gems: return Gems;
        }

        if (ResourceIds.IsFish(id))
        {
            return FishCollection.TryGetValue(id, out var count) ? count : 0;
        }

        throw new ArgumentException($"Unknown resource id '{id}'");
    }

    public void SetResource(string id, int value)
    {
        switch (id)
        {
            case ResourceIds.DirtyRubles:
                DirtyRubles = value;
                return;
            case ResourceIds.LaunderedRubles:
                LaunderedRubles = value;
                return;
            case ResourceIds.Reputation:
                Reputation = value;
                return;
            case ResourceIds.Gems:
                Gems = value;
                return;
        }

        if (ResourceIds.IsFish(id))
        {
            FishCollection[id] = value;
            return;
        }

        throw new ArgumentException($"Unknown resource id '{id}'");
    }
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

/// <summary>A fishing run the backend has started, with what it needs to check the results.</summary>
public class ActiveRunState
{
    public string Id { get; set; }
    public DateTime StartedAt { get; set; }

    // Gear at the start of the run: the results can't go deeper or hold more fish than this.
    public float MaxDepth { get; set; }
    public int Capacity { get; set; }
}
