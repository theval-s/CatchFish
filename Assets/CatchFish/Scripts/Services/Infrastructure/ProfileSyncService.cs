using System;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Holds the player's <see cref="GameState"/> as the backend last sent it. Domain services send an
/// action through <see cref="IApiClient"/> and pass it to <see cref="TryApplyAsync"/>; nothing on the
/// client changes the state itself, and there's nothing to save: the backend saves every change.
/// </summary>
public class ProfileSyncService
{
    private readonly IApiClient _api;

    public ProfileSyncService(IApiClient api)
    {
        _api = api;
    }

    /// <summary>Raised when the backend sent a new profile.</summary>
    public event Action StateChanged;

    public GameState State { get; private set; }

    public async Task LoadAsync()
    {
        State = await _api.LoadProfileAsync();
    }

    /// <summary>
    /// Waits for a backend action and takes the profile it returns. False if the backend refused
    /// the action or couldn't be reached; the state then stays as it was.
    /// </summary>
    public async Task<bool> TryApplyAsync(Task<GameState> request)
    {
        try
        {
            State = await request;
        }
        catch (ApiException e)
        {
            // TODO: tell the player when the server can't be reached
            Debug.LogWarning($"Backend: {e.Message}");
            return false;
        }
        catch (Exception e)
        {
            // Callers fire and forget, so a bug here would otherwise go unnoticed.
            Debug.LogException(e);
            return false;
        }

        StateChanged?.Invoke();
        return true;
    }
}
