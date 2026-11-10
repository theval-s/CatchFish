using System.Collections.Generic;

/// <summary>What the client reports when a fishing run ends. The backend works out the money itself.</summary>
public class RunResult
{
    public string RunId { get; set; }

    /// <summary>Fish id → how many fish of that kind were shot.</summary>
    public Dictionary<string, int> ShotFish { get; set; } = new Dictionary<string, int>();
}
