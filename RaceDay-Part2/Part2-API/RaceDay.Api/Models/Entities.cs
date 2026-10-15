using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models;

public static class Roles
{
    public const string Organiser = "Organiser";
    public const string Participant = "Participant";

    /// <summary>Returns the canonical role name, or null if the value is not a valid role.</summary>
    public static string? Normalise(string? role)
    {
        if (string.Equals(role?.Trim(), Organiser, StringComparison.OrdinalIgnoreCase)) return Organiser;
        if (string.Equals(role?.Trim(), Participant, StringComparison.OrdinalIgnoreCase)) return Participant;
        return null;
    }
}

public enum EventType
{
    Run,
    Walk,
    Cycle
}

public class User
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    /// <summary>PBKDF2 hash in the format iterations.salt.hash - never the plain password.</summary>
    [Required, MaxLength(500)]
    public string PasswordHash { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string Role { get; set; } = Roles.Participant;

    [MaxLength(30)]
    public string? PhoneNumber { get; set; }

    public DateTime? DateOfBirth { get; set; }
}

public class Event
{
    public int Id { get; set; }

    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    public DateTime Date { get; set; }

    [Required, MaxLength(200)]
    public string Location { get; set; } = string.Empty;

    public decimal DistanceKm { get; set; }

    public EventType EventType { get; set; }

    public int OrganiserId { get; set; }
    public User? Organiser { get; set; }

    public List<Category> Categories { get; set; } = new();
    public List<Enrolment> Enrolments { get; set; } = new();
}

public class Category
{
    public int Id { get; set; }

    public int EventId { get; set; }
    public Event? Event { get; set; }

    /// <summary>e.g. Under 20, Senior, 10km, 21km</summary>
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    public decimal? DistanceKm { get; set; }
}

public class Enrolment
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }

    public int EventId { get; set; }
    public Event? Event { get; set; }

    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;

    public RaceResult? Result { get; set; }
}

public class RaceResult
{
    public int Id { get; set; }

    public int EnrolmentId { get; set; }
    public Enrolment? Enrolment { get; set; }

    public TimeSpan FinishTime { get; set; }
    public int Position { get; set; }
}
