using System;
using System.Collections.Generic;

/// <summary>
/// Currencies and collectible fish by resource id (see <see cref="ResourceIds"/>). Read-only:
/// only the backend changes them, and <see cref="Changed"/> fires when it sends a new profile.
/// </summary>
public class ResourceService : IDisposable
{
    private readonly ProfileSyncService _profile;

    public ResourceService(ProfileSyncService profile)
    {
        _profile = profile;
        _profile.StateChanged += OnStateChanged;
    }

    public event Action Changed;

    public void Dispose() => _profile.StateChanged -= OnStateChanged;

    public int Get(string id) => _profile.State.Player.GetResource(id);

    public bool Has(string id, int amount) => Get(id) >= amount;

    public bool Has(IReadOnlyDictionary<string, int> amounts)
    {
        foreach (var pair in amounts)
        {
            if (!Has(pair.Key, pair.Value))
            {
                return false;
            }
        }

        return true;
    }

    private void OnStateChanged() => Changed?.Invoke();
}
