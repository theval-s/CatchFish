using System;

/// <summary>Fits a school on a wrapping lane while reserving a readable gap after the formation.</summary>
public static class FishSchoolLayout
{
    public static int FitCount(int requested, float span, float spacing, float gap)
    {
        var capacity = (int)Math.Floor(Math.Max(0f, 2f * span - Math.Max(0f, gap)) / Math.Max(0.01f, spacing));
        return Math.Max(1, Math.Min(Math.Max(1, requested), capacity));
    }

    public static float Wrap(float x, float span)
    {
        span = Math.Max(0.01f, span);
        var width = 2f * span;
        return x - width * (float)Math.Floor((x + span) / width);
    }
}
