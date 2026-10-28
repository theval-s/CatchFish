using System;
using System.Collections.Generic;

/// <summary>
/// Currencies and collectible fish by resource id (see <see cref="ResourceIds"/>).
/// Doesn't save: the service that runs the whole transaction does.
/// </summary>
public class ResourceService
{
    private readonly ProfileSyncService _profile;

    public ResourceService(ProfileSyncService profile)
    {
        _profile = profile;
    }

    public event Action Changed;

    private PlayerState Player => _profile.State.Player;

    public int Get(string id)
    {
        switch (id)
        {
            case ResourceIds.DirtyRubles: return Player.DirtyRubles;
            case ResourceIds.LaunderedRubles: return Player.LaunderedRubles;
            case ResourceIds.Reputation: return Player.Reputation;
            case ResourceIds.Gems: return Player.Gems;
        }

        if (ResourceIds.IsFish(id))
        {
            return Player.FishCollection.TryGetValue(id, out var count) ? count : 0;
        }

        throw new ArgumentException($"Unknown resource id '{id}'");
    }

    /// <summary>Negative amounts take away, but never below zero.</summary>
    public void Add(string id, int amount)
    {
        if (amount == 0)
        {
            return;
        }

        Set(id, Math.Max(0, Get(id) + amount));
        Changed?.Invoke();
    }

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

    public bool TrySpend(string id, int amount)
    {
        if (!Has(id, amount))
        {
            return false;
        }

        Set(id, Get(id) - amount);
        Changed?.Invoke();
        return true;
    }

    /// <summary>Spends all or nothing.</summary>
    public bool TrySpend(IReadOnlyDictionary<string, int> amounts)
    {
        if (!Has(amounts))
        {
            return false;
        }

        foreach (var pair in amounts)
        {
            Set(pair.Key, Get(pair.Key) - pair.Value);
        }

        Changed?.Invoke();
        return true;
    }

    private void Set(string id, int value)
    {
        switch (id)
        {
            case ResourceIds.DirtyRubles:
                Player.DirtyRubles = value;
                return;
            case ResourceIds.LaunderedRubles:
                Player.LaunderedRubles = value;
                return;
            case ResourceIds.Reputation:
                Player.Reputation = value;
                return;
            case ResourceIds.Gems:
                Player.Gems = value;
                return;
        }

        if (ResourceIds.IsFish(id))
        {
            Player.FishCollection[id] = value;
            return;
        }

        throw new ArgumentException($"Unknown resource id '{id}'");
    }
}
