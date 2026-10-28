using System.Collections.Generic;

public enum FishingPhase
{
    Descent,  // hook sinks, dodge fish
    Ascent,   // hook rises, catch fish
    Shooting, // catch flies up, shoot it for money
    Results,  // run summary
}

/// <summary>
/// One fishing run: the gear it started with and what happened so far. Not saved;
/// only the results are applied to the profile when the run ends.
/// </summary>
public class FishingSessionState
{
    public FishingPhase Phase { get; set; } = FishingPhase.Descent;

    // Gear snapshot taken when the run starts.
    public float MaxDepth { get; set; }
    public int Durability { get; set; }
    public int Capacity { get; set; }
    public float ReelSpeed { get; set; }

    public float DepthReached { get; set; }
    public int Caught { get; set; }
    public int Shot { get; set; }
    public int Lost { get; set; }
    public int Earnings { get; set; }

    /// <summary>Collectible fish shot during the run: fish id → count.</summary>
    public Dictionary<string, int> CollectedFish { get; } = new Dictionary<string, int>();
}
