using System;
using System.Collections.Generic;
using UnityEngine;

public enum FishSwimPattern
{
    Cruise,
    Wave,
    TimedBurst,
    HookLunge,
    Inflation,
}

/// <summary>Reusable client-only movement, formation and armor tuning.</summary>
[Serializable]
public class FishBehaviorSettings
{
    [SerializeField] private FishSwimPattern pattern;
    [Header("Cycle (seconds)")]
    [Min(0f)] [SerializeField] private float idleMin = 3f;
    [Min(0f)] [SerializeField] private float idleMax = 5f;
    [Min(0f)] [SerializeField] private float warningSeconds = 0.4f;
    [Min(0f)] [SerializeField] private float activeSeconds = 0.5f;
    [Min(0f)] [SerializeField] private float recoverySeconds = 1f;
    [Header("Movement")]
    [Min(0f)] [SerializeField] private float speedMultiplier = 3f;
    [Min(0f)] [SerializeField] private float triggerDistance = 2f;
    [Min(0f)] [SerializeField] private float waveAmplitude = 0.45f;
    [Min(0.01f)] [SerializeField] private float wavePeriod = 3.2f;
    [Header("Inflation")]
    [Min(1f)] [SerializeField] private float inflatedScale = 1.65f;
    [Header("School")]
    [SerializeField] private bool school;
    [Min(1)] [SerializeField] private int schoolMin = 3;
    [Min(1)] [SerializeField] private int schoolMax = 5;
    [Min(0f)] [SerializeField] private float schoolVerticalOffset = 0.2f;
    [Tooltip("Clear space between sprite canvases, and a larger open lane after the group.")]
    [Min(0f)] [SerializeField] private float schoolSpacing = 0.2f;
    [Min(0f)] [SerializeField] private float schoolGap = 1.2f;
    [Header("Airborne")]
    [Min(1)] [SerializeField] private int airborneHits = 1;

    public FishSwimPattern Pattern => pattern;
    public float IdleMin => idleMin;
    public float IdleMax => idleMax;
    public float WarningSeconds => warningSeconds;
    public float ActiveSeconds => activeSeconds;
    public float RecoverySeconds => recoverySeconds;
    public float SpeedMultiplier => Math.Max(0f, speedMultiplier);
    public float TriggerDistance => Math.Max(0f, triggerDistance);
    public float WaveAmplitude => Math.Max(0f, waveAmplitude);
    public float WavePeriod => Math.Max(0.01f, wavePeriod);
    public float InflatedScale => Math.Max(1f, inflatedScale);
    public bool School => school;
    public int SchoolMin => Math.Max(1, Math.Min(schoolMin, schoolMax));
    public int SchoolMax => Math.Max(SchoolMin, Math.Max(schoolMin, schoolMax));
    public float SchoolVerticalOffset => Math.Max(0f, schoolVerticalOffset);
    public float SchoolSpacing => Math.Max(0f, schoolSpacing);
    public float SchoolGap => Math.Max(0f, schoolGap);
    public int AirborneHits => Math.Max(1, airborneHits);
}

/// <summary>Maps species to reusable behavior profiles independently of skins and server data.</summary>
[CreateAssetMenu(menuName = "CatchFish/Fish Behavior Catalog")]
public class FishBehaviorCatalog : ScriptableObject
{
    [Serializable]
    private class Profile
    {
        [SerializeField] private string[] fishIds;
        [SerializeField] private FishBehaviorSettings settings = new FishBehaviorSettings();

        public bool Contains(string fishId) => fishIds != null && Array.IndexOf(fishIds, fishId) >= 0;
        public FishBehaviorSettings Settings => settings;
    }

    private static readonly FishBehaviorSettings Cruise = new FishBehaviorSettings();
    [SerializeField] private List<Profile> profiles = new List<Profile>();

    public FishBehaviorSettings Get(string fishId)
    {
        foreach (var profile in profiles)
        {
            if (profile != null && profile.Contains(fishId) && profile.Settings != null)
            {
                return profile.Settings;
            }
        }

        return Cruise;
    }
}
