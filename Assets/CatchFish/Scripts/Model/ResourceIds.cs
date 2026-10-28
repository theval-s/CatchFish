/// <summary>
/// String ids of everything the player can own. Quest requirements use the same ids,
/// so new currencies can be added without changing the data format.
/// </summary>
public static class ResourceIds
{
    public const string DirtyRubles = "dr";
    public const string LaunderedRubles = "lr";
    public const string Reputation = "rep";
    public const string Gems = "gems";

    /// <summary>Fish ids look like "f12".</summary>
    public static bool IsFish(string id) => id.Length > 1 && id[0] == 'f' && char.IsDigit(id[1]);
}
