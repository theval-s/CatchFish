using System;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Owns the player's <see cref="GameState"/>: loads it at startup and sends it to the backend
/// after every change. Domain services change the state, then call <see cref="Save"/> once.
/// </summary>
public class ProfileSyncService : IDisposable
{
    private readonly IApiClient _api;
    private readonly ConfigService _config;
    private readonly ServerTimeService _time;

    public ProfileSyncService(IApiClient api, ConfigService config, ServerTimeService time)
    {
        _api = api;
        _config = config;
        _time = time;

        Application.quitting += Save;
        Application.focusChanged += OnFocusChanged;
    }

    public GameState State { get; private set; }

    public void Dispose()
    {
        Application.quitting -= Save;
        Application.focusChanged -= OnFocusChanged;
    }

    /// <summary>Needs the config to be loaded first: a new player starts from it.</summary>
    public async Task LoadAsync()
    {
        State = await _api.LoadProfileAsync() ?? GameState.CreateNew(_config.Config);
    }

    public void Save()
    {
        if (State == null)
        {
            return;
        }

        State.SavedAt = _time.Now;
        SaveAsync();
    }

    private async void SaveAsync()
    {
        try
        {
            await _api.SaveProfileAsync(State);
        }
        catch (Exception e)
        {
            // TODO: retry and tell the player once there is a real server to lose connection to
            Debug.LogException(e);
        }
    }

    // On phones the app is often killed in the background without quitting.
    private void OnFocusChanged(bool hasFocus)
    {
        if (!hasFocus)
        {
            Save();
        }
    }
}
