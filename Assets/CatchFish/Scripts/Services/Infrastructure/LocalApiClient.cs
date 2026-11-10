using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// The backend on the device, for playing without the server: config from Resources, the profile
/// in a JSON file, device clock as server time. A C# copy of the C++ server's rules (Server/README.md);
/// keep the two in sync.
/// </summary>
public class LocalApiClient : IApiClient
{
    private const string ConfigResource = "LocalBackend/game_config";
    private const float DepthTolerance = 5f;
    private const double MinRunSeconds = 2;

    private GameConfig _config;

    private static string ProfilePath => Path.Combine(Application.persistentDataPath, "profile.json");

    public async Task<GameConfig> LoadConfigAsync()
    {
        if (_config != null)
        {
            return _config;
        }

        var request = Resources.LoadAsync<TextAsset>(ConfigResource);
        await request;

        var asset = request.asset as TextAsset;
        if (asset == null)
        {
            throw new InvalidOperationException($"Config not found at Resources/{ConfigResource}.json");
        }

        _config = Json.Deserialize<GameConfig>(asset.text);
        return _config;
    }

    public async Task<GameState> LoadProfileAsync()
    {
        if (!File.Exists(ProfilePath))
        {
            var state = GameState.CreateNew(await LoadConfigAsync());
            Save(state);
            return state;
        }

        var json = await File.ReadAllTextAsync(ProfilePath);
        return Json.Deserialize<GameState>(json);
    }

    public Task<DateTime> GetServerTimeAsync() => Task.FromResult(DateTime.UtcNow);

    public Task<GameState> BuyGearAsync(string gearId) => ChangeAsync((state, config) =>
    {
        var gear = config.Gear.Find(g => g.Id == gearId);
        if (gear == null)
        {
            throw new ApiException("unknown_gear", gearId);
        }

        state.Upgrades.GearLevels.TryGetValue(gearId, out var level);
        if (level >= gear.MaxLevel)
        {
            throw new ApiException("gear_maxed", gearId);
        }

        Spend(state.Player, new Dictionary<string, int> { [ResourceIds.DirtyRubles] = gear.CostAt(level) });
        state.Upgrades.GearLevels[gearId] = level + 1;
    });

    public Task<GameState> CompleteQuestAsync(int questId) => ChangeAsync((state, config) =>
    {
        var quest = config.Quests.Find(q => q.Id == questId);
        if (quest == null)
        {
            throw new ApiException("unknown_quest", questId.ToString());
        }

        var progress = state.Quests;
        if (!progress.Active.Contains(questId))
        {
            throw new ApiException("quest_not_active", questId.ToString());
        }

        Spend(state.Player, quest.Requirements);
        progress.Active.Remove(questId);
        progress.Completed.Add(questId);
        foreach (var id in quest.UnlocksId)
        {
            if (!progress.Active.Contains(id) && !progress.Completed.Contains(id))
            {
                progress.Active.Add(id);
            }
        }
    });

    public Task<GameState> StartRunAsync() => ChangeAsync((state, config) =>
    {
        // TODO: refuse while the wanted level is too high, once it exists
        state.ActiveRun = new ActiveRunState
        {
            Id = Guid.NewGuid().ToString("N"),
            StartedAt = DateTime.UtcNow,
            MaxDepth = GearValue(state, config, GearIds.LineLength),
            Capacity = (int)GearValue(state, config, GearIds.HookCapacity),
        };
    });

    public Task<GameState> CompleteRunAsync(RunResult result) => ChangeAsync((state, config) =>
    {
        var run = state.ActiveRun;
        if (run == null || run.Id != result.RunId)
        {
            throw new ApiException("no_active_run", result.RunId);
        }

        if ((DateTime.UtcNow - run.StartedAt).TotalSeconds < MinRunSeconds)
        {
            throw new ApiException("invalid_run", "the run was too short");
        }

        // The minigame runs on the client, so the results can only be checked for being possible.
        var shot = new List<(FishType Fish, int Count)>();
        var total = 0;
        foreach (var pair in result.ShotFish)
        {
            var fish = config.Fish.Find(f => f.Id == pair.Key);
            if (fish == null)
            {
                throw new ApiException("unknown_fish", pair.Key);
            }

            if (pair.Value < 0 || fish.MinDepth > run.MaxDepth + DepthTolerance)
            {
                throw new ApiException("invalid_run", $"{pair.Key} can't be caught with this line");
            }

            shot.Add((fish, pair.Value));
            total += pair.Value;
        }

        if (total > run.Capacity)
        {
            throw new ApiException("invalid_run", "more fish than the hook holds");
        }

        var player = state.Player;
        var earnings = 0;
        foreach (var (fish, count) in shot)
        {
            earnings += fish.Value * count;
            if (fish.IsCollectible)
            {
                player.SetResource(fish.Id, player.GetResource(fish.Id) + count);
            }
        }

        // Jellyfish cost money, but the balance never goes below zero.
        player.DirtyRubles = Math.Max(0, player.DirtyRubles + earnings);
        player.RunsCompleted++;
        state.ActiveRun = null;
    });

    // Each action reads the profile from the file and changes that copy, like the server works on
    // its database, so a refused action leaves the client's state untouched.
    private async Task<GameState> ChangeAsync(Action<GameState, GameConfig> change)
    {
        var config = await LoadConfigAsync();
        var state = await LoadProfileAsync();
        change(state, config);
        state.SavedAt = DateTime.UtcNow;
        Save(state);
        return state;
    }

    // Written synchronously, so the file is complete before the action returns.
    private static void Save(GameState state) => File.WriteAllText(ProfilePath, Json.Serialize(state));

    /// <summary>Spends all or nothing.</summary>
    private static void Spend(PlayerState player, IReadOnlyDictionary<string, int> amounts)
    {
        foreach (var pair in amounts)
        {
            if (player.GetResource(pair.Key) < pair.Value)
            {
                throw new ApiException("not_enough_resources", $"{pair.Key}: {player.GetResource(pair.Key)} / {pair.Value}");
            }
        }

        foreach (var pair in amounts)
        {
            player.SetResource(pair.Key, player.GetResource(pair.Key) - pair.Value);
        }
    }

    private static float GearValue(GameState state, GameConfig config, string gearId)
    {
        state.Upgrades.GearLevels.TryGetValue(gearId, out var level);
        return config.Gear.Find(g => g.Id == gearId).ValueAt(level);
    }
}
