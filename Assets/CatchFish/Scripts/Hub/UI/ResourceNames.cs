using System.Collections.Generic;

/// <summary>Player-facing names of resource ids (see <see cref="ResourceIds"/>), for quest requirements and rewards.</summary>
public static class ResourceNames
{
    public static string Get(string id, ConfigService config)
    {
        switch (id)
        {
            case ResourceIds.DirtyRubles: return "Dirty rubles";
            case ResourceIds.LaunderedRubles: return "Laundered rubles";
            case ResourceIds.Reputation: return "Reputation";
            case ResourceIds.Gems: return "Gems";
        }

        var fish = config.GetFish(id);
        return fish != null ? fish.Name : id;
    }

    /// <summary>Prices like "300 DR, 2 gems".</summary>
    public static string Price(IReadOnlyDictionary<string, int> amounts, ConfigService config)
    {
        var parts = new List<string>();
        foreach (var pair in amounts)
        {
            parts.Add($"{NumberFormat.Short(pair.Value)} {Short(pair.Key, config)}");
        }

        return parts.Count > 0 ? string.Join(", ", parts) : "free";
    }

    private static string Short(string id, ConfigService config)
    {
        switch (id)
        {
            case ResourceIds.DirtyRubles: return "DR";
            case ResourceIds.LaunderedRubles: return "LR";
            case ResourceIds.Reputation: return "rep";
            case ResourceIds.Gems: return "gems";
        }

        return Get(id, config);
    }
}
