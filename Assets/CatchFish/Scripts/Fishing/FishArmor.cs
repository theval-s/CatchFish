using System;

/// <summary>Tracks airborne damage and reports a lethal hit exactly once per initialization.</summary>
public sealed class FishArmor
{
    public int HitsLeft { get; private set; }
    public int TotalHits { get; private set; }
    public float Damage => 1f - (float)HitsLeft / TotalHits;

    public void Reset(int hits)
    {
        TotalHits = Math.Max(1, hits);
        HitsLeft = TotalHits;
    }

    public bool TryHit()
    {
        if (HitsLeft <= 0)
        {
            return false;
        }

        HitsLeft--;
        return HitsLeft == 0;
    }
}
