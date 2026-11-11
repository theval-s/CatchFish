using System;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Server time, so changing the phone's clock can't speed up timers.
/// Synced at startup and whenever the app comes back to the foreground, advanced in between
/// with the app's own unscaled clock.
/// </summary>
public class ServerTimeService : IDisposable
{
    private readonly IApiClient _api;
    private DateTime _syncedServerTime;
    private double _syncedAtRealtime;
    private bool _synced;

    public ServerTimeService(IApiClient api)
    {
        _api = api;
        Application.focusChanged += OnFocusChanged;
    }

    public DateTime Now => _syncedServerTime.AddSeconds(Time.realtimeSinceStartupAsDouble - _syncedAtRealtime);

    public void Dispose() => Application.focusChanged -= OnFocusChanged;

    public async Task SyncAsync()
    {
        _syncedServerTime = await _api.GetServerTimeAsync();
        _syncedAtRealtime = Time.realtimeSinceStartupAsDouble;
        _synced = true;
    }

    // The app's clock may not count while the phone sleeps, and a day can change in that time
    // (the daily reward). One request on resume puts it right.
    private async void OnFocusChanged(bool hasFocus)
    {
        if (!hasFocus || !_synced)
        {
            return;
        }

        try
        {
            await SyncAsync();
        }
        catch (ApiException e)
        {
            // The last sync keeps counting; the server still checks every claim itself.
            Debug.LogWarning($"Server time: {e.Message}");
        }
    }
}
