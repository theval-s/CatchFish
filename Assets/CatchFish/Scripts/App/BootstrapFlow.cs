using System.Threading;
using UnityEngine;
using VContainer.Unity;

/// <summary>
/// App startup: load the config, the player's profile and server time, then open the hub.
/// Nothing is saved on exit: the backend saves every change as it happens.
/// </summary>
public class BootstrapFlow : IAsyncStartable
{
    private readonly ConfigService _config;
    private readonly ProfileSyncService _profile;
    private readonly ServerTimeService _time;
    private readonly SceneLoader _scenes;

    public BootstrapFlow(ConfigService config, ProfileSyncService profile, ServerTimeService time, SceneLoader scenes)
    {
        _config = config;
        _profile = profile;
        _time = time;
        _scenes = scenes;
    }

    public async Awaitable StartAsync(CancellationToken cancellation = default)
    {
        await _config.LoadAsync();
        await _profile.LoadAsync();
        await _time.SyncAsync();

        // TODO: the backend adds the school's income for the time the player was away when it sends the profile

        await _scenes.LoadAsync(SceneNames.Hub);
    }
}
