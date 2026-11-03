using System;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Server time, so changing the phone's clock can't speed up timers.
/// Synced once at startup, then advanced with the app's own unscaled clock.
/// </summary>
public class ServerTimeService
{
    private readonly IApiClient _api;
    private DateTime _syncedServerTime;
    private double _syncedAtRealtime;

    public ServerTimeService(IApiClient api)
    {
        _api = api;
    }

    public DateTime Now => _syncedServerTime.AddSeconds(Time.realtimeSinceStartupAsDouble - _syncedAtRealtime);

    public async Task SyncAsync()
    {
        _syncedServerTime = await _api.GetServerTimeAsync();
        _syncedAtRealtime = Time.realtimeSinceStartupAsDouble;
    }
}
