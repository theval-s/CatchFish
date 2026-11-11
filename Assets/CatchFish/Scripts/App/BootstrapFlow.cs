using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using VContainer.Unity;

/// <summary>
/// App startup: sign in, load the config, the player's profile and server time, then open the hub.
/// If the server can't be used, the loading screen says why and the player retries (or starts over).
/// Nothing is saved on exit: the server saves every change as it happens.
/// </summary>
public class BootstrapFlow : IAsyncStartable
{
    private readonly IApiClient _api;
    private readonly ConfigService _config;
    private readonly ProfileSyncService _profile;
    private readonly ServerTimeService _time;
    private readonly SceneLoader _scenes;
    private readonly LoadingScreen _loadingScreen;

    public BootstrapFlow(IApiClient api, ConfigService config, ProfileSyncService profile, ServerTimeService time,
        SceneLoader scenes, LoadingScreen loadingScreen)
    {
        _api = api;
        _config = config;
        _profile = profile;
        _time = time;
        _scenes = scenes;
        _loadingScreen = loadingScreen;
    }

    public async Awaitable StartAsync(CancellationToken cancellation = default)
    {
        var startOver = false;
        while (true)
        {
            try
            {
                if (startOver)
                {
                    await _api.StartNewPlayerAsync();
                }
                else
                {
                    await _api.SignInAsync();
                }

                await _config.LoadAsync();
                await _profile.LoadAsync();
                await _time.SyncAsync();
                break;
            }
            catch (ApiException e)
            {
                Debug.LogWarning($"Startup: {e.Message}");
                startOver = await AskStartOverAsync(e);
            }
        }

        await _scenes.LoadAsync(SceneNames.Hub);
    }

    // Waits for the player's choice: true = start over as a new player, false = retry.
    private async Task<bool> AskStartOverAsync(ApiException error)
    {
        var accountMissing = error.Code == ApiException.AccountNotFound;
        var message = accountMissing
            ? "The server doesn't know this device's player.\nRetry later, or start over as a new player."
            : error.Code == ApiException.ConnectionError
                ? "Can't reach the server.\nCheck the connection and retry."
                : $"The server refused to start the game:\n{error.Message}";

        var choice = new TaskCompletionSource<bool>();
        void OnRetry() => choice.TrySetResult(false);
        void OnStartOver() => choice.TrySetResult(true);

        _loadingScreen.RetryClicked += OnRetry;
        _loadingScreen.StartOverClicked += OnStartOver;
        _loadingScreen.ShowError(message, accountMissing);
        try
        {
            return await choice.Task;
        }
        finally
        {
            _loadingScreen.RetryClicked -= OnRetry;
            _loadingScreen.StartOverClicked -= OnStartOver;
            _loadingScreen.HideError();
        }
    }
}
