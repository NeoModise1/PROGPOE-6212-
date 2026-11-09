namespace RaceDay.Web.Services;

public static class SessionKeys
{
    public const string Token = "raceday.token";
    public const string UserId = "raceday.userId";
    public const string Role = "raceday.role";
    public const string Name = "raceday.name";
    public const string Picture = "raceday.picture";
}

/// <summary>Snapshot of the signed-in user, read from the session.</summary>
public class CurrentUser
{
    public int UserId { get; init; }
    public string Role { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? PictureUrl { get; init; }
    public bool IsAuthenticated { get; init; }
    public bool IsOrganiser => IsAuthenticated && Role == "Organiser";
    public bool IsParticipant => IsAuthenticated && Role == "Participant";
    public string Initials => string.Concat(Name.Split(' ', StringSplitOptions.RemoveEmptyEntries)
        .Take(2).Select(p => char.ToUpperInvariant(p[0])));
}

public static class SessionExtensions
{
    public static CurrentUser GetCurrentUser(this ISession session)
    {
        var token = session.GetString(SessionKeys.Token);
        if (string.IsNullOrEmpty(token)) return new CurrentUser();

        return new CurrentUser
        {
            IsAuthenticated = true,
            UserId = session.GetInt32(SessionKeys.UserId) ?? 0,
            Role = session.GetString(SessionKeys.Role) ?? string.Empty,
            Name = session.GetString(SessionKeys.Name) ?? string.Empty,
            PictureUrl = session.GetString(SessionKeys.Picture)
        };
    }

    public static void SignIn(this ISession session, string token, int userId, string role, string name, string? pictureUrl)
    {
        session.Clear();
        session.SetString(SessionKeys.Token, token);
        session.SetInt32(SessionKeys.UserId, userId);
        session.SetString(SessionKeys.Role, role);
        session.SetString(SessionKeys.Name, name);
        SetPicture(session, pictureUrl);
    }

    public static void SetPicture(this ISession session, string? pictureUrl)
    {
        if (string.IsNullOrEmpty(pictureUrl)) session.Remove(SessionKeys.Picture);
        else session.SetString(SessionKeys.Picture, pictureUrl);
    }
}
