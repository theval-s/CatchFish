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
}
