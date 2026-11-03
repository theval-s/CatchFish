/// <summary>Short numbers for counters: 9999, 10k, 1.2M.</summary>
public static class NumberFormat
{
    public static string Short(long value)
    {
        if (value < 10_000)
        {
            return value.ToString();
        }

        if (value < 1_000_000)
        {
            return $"{value / 1000f:0.#}k";
        }

        return $"{value / 1_000_000f:0.#}M";
    }
}
