namespace RaceDay.Web.Services;

/// <summary>Small display helpers used by the views.</summary>
public static class Format
{
    public static string Ordinal(int n)
    {
        var lastTwo = n % 100;
        if (lastTwo is >= 11 and <= 13) return $"{n}th";
        return (n % 10) switch
        {
            1 => $"{n}st",
            2 => $"{n}nd",
            3 => $"{n}rd",
            _ => $"{n}th"
        };
    }

    public static string Time(TimeSpan? t) =>
        t is null ? "-" : $"{(int)t.Value.TotalHours:00}:{t.Value.Minutes:00}:{t.Value.Seconds:00}";

    public static string ClockTime(TimeSpan t) => $"{t.Hours:00}:{t.Minutes:00}";

    public static string Money(decimal amount) => amount == 0 ? "Free" : $"R {amount:0.00}";

    public static string Distance(decimal km) => km % 1 == 0 ? $"{km:0} km" : $"{km:0.##} km";

    /// <summary>Maps a status text to the CSS class that colour-codes it.</summary>
    public static string StatusClass(string? status) => (status ?? string.Empty).ToLowerInvariant() switch
    {
        "confirmed" or "completed" => "status-green",
        "registered" or "pending" or "scheduled" => "status-amber",
        "ongoing" => "status-blue",
        "cancelled" or "disqualified" or "dns" => "status-red",
        "dnf" => "status-orange",
        _ => "status-grey"
    };
}
