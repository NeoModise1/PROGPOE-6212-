namespace RaceDay.Api.Settings;

public class JwtSettings
{
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = "RaceDayApi";
    public string Audience { get; set; } = "RaceDayClients";
    public int ExpiryMinutes { get; set; } = 120;
}
