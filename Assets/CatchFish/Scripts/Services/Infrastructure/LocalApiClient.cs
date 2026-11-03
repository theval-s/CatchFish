using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Stand-in for the backend: config from Resources, profile in a JSON file on the device,
/// device clock as server time.
/// </summary>
public class LocalApiClient : IApiClient
{
    private const string ConfigResource = "LocalBackend/game_config";

    private static string ProfilePath => Path.Combine(Application.persistentDataPath, "profile.json");

    public async Task<GameConfig> LoadConfigAsync()
    {
        var request = Resources.LoadAsync<TextAsset>(ConfigResource);
        await request;

        var asset = request.asset as TextAsset;
        if (asset == null)
        {
            throw new InvalidOperationException($"Config not found at Resources/{ConfigResource}.json");
        }

        return Json.Deserialize<GameConfig>(asset.text);
    }

    public async Task<GameState> LoadProfileAsync()
    {
        if (!File.Exists(ProfilePath))
        {
            return null;
        }

        var json = await File.ReadAllTextAsync(ProfilePath);
        return Json.Deserialize<GameState>(json);
    }

    public Task SaveProfileAsync(GameState state)
    {
        // Written synchronously: saves also happen while the app is quitting, when nothing gets awaited.
        File.WriteAllText(ProfilePath, Json.Serialize(state));
        return Task.CompletedTask;
    }

    public Task<DateTime> GetServerTimeAsync() => Task.FromResult(DateTime.UtcNow);
}
