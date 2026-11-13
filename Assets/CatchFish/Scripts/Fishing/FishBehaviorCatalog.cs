using System;
using System.Collections.Generic;
using UnityEngine;

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
