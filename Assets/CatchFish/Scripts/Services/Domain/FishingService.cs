using System;

/// <summary>
/// Starts fishing runs and applies their results to the profile. The minigame itself
/// runs in <see cref="FishingController"/> and fills in <see cref="Session"/>.
/// </summary>
public class FishingService
{
    private readonly UpgradeService _upgrades;
    private readonly ResourceService _resources;
    private readonly ProfileSyncService _profile;

    public FishingService(UpgradeService upgrades, ResourceService resources, ProfileSyncService profile)
    {
        _upgrades = upgrades;
        _resources = resources;
        _profile = profile;
    }

    /// <summary>Raised after the results of a run were added to the profile.</summary>
    public event Action RunCompleted;

    public FishingSessionState Session { get; private set; }

    public FishingSessionState StartRun()
    {
        // TODO: check the wanted level here once it exists
        Session = new FishingSessionState
        {
            MaxDepth = _upgrades.Value(GearIds.LineLength),
            Durability = (int)_upgrades.Value(GearIds.LineDurability),
            Capacity = (int)_upgrades.Value(GearIds.HookCapacity),
            ReelSpeed = _upgrades.Value(GearIds.ReelSpeed),
        };
        return Session;
    }

    public void CompleteRun()
    {
        Session.Phase = FishingPhase.Results;

        // Earnings can be negative (jellyfish); Add() doesn't go below zero.
        _resources.Add(ResourceIds.DirtyRubles, Session.Earnings);
        foreach (var pair in Session.CollectedFish)
        {
            _resources.Add(pair.Key, pair.Value);
        }

        _profile.Save();
        RunCompleted?.Invoke();
    }
}
