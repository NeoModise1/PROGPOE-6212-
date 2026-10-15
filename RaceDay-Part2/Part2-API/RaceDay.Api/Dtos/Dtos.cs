using System.ComponentModel.DataAnnotations;
using RaceDay.Api.Models;

namespace RaceDay.Api.Dtos;

// ---------- Auth / profile ----------
public class RegisterRequest
{
    [Required, StringLength(100)] public string FirstName { get; set; } = string.Empty;
    [Required, StringLength(100)] public string LastName { get; set; } = string.Empty;
    [Required, EmailAddress, StringLength(200)] public string Email { get; set; } = string.Empty;
    [Required, StringLength(100, MinimumLength = 8)] public string Password { get; set; } = string.Empty;

    /// <summary>"Organiser" or "Participant".</summary>
    [Required] public string Role { get; set; } = string.Empty;

    /// <summary>Secret code, required only when registering as an Organiser.</summary>
    public string? OrganiserCode { get; set; }

    [StringLength(30)] public string? PhoneNumber { get; set; }
    public DateTime? DateOfBirth { get; set; }
}

public class LoginRequest
{
    [Required] public string Email { get; set; } = string.Empty;
    [Required] public string Password { get; set; } = string.Empty;
}

public class UpdateProfileRequest
{
    [Required, StringLength(100)] public string FirstName { get; set; } = string.Empty;
    [Required, StringLength(100)] public string LastName { get; set; } = string.Empty;
    [StringLength(30)] public string? PhoneNumber { get; set; }
    public DateTime? DateOfBirth { get; set; }
}

public record ProfileResponse(int Id, string FirstName, string LastName, string Email, string Role,
    string? PhoneNumber, DateTime? DateOfBirth);

// ---------- Events ----------
public class EventRequest
{
    [Required, StringLength(150)] public string Name { get; set; } = string.Empty;
    [StringLength(2000)] public string Description { get; set; } = string.Empty;
    [Required] public DateTime? Date { get; set; }
    [Required, StringLength(200)] public string Location { get; set; } = string.Empty;
    [Range(0.1, 1000)] public decimal DistanceKm { get; set; }

    /// <summary>Run, Walk or Cycle.</summary>
    [Required] public EventType? EventType { get; set; }
}

public record EventResponse(int Id, string Name, string Description, DateTime Date, string Location,
    decimal DistanceKm, string EventType, int OrganiserId);

// ---------- Categories ----------
public class CategoryRequest
{
    [Required, StringLength(100)] public string Name { get; set; } = string.Empty;
    [Range(0, 120)] public int? MinAge { get; set; }
    [Range(0, 120)] public int? MaxAge { get; set; }
    [Range(0.1, 1000)] public decimal? DistanceKm { get; set; }
}

public record CategoryResponse(int Id, int EventId, string Name, int? MinAge, int? MaxAge, decimal? DistanceKm);

// ---------- Enrolments ----------
public class EnrolRequest
{
    [Range(1, int.MaxValue)] public int CategoryId { get; set; }
}

public record EnrolmentResponse(int Id, int EventId, string EventName, int UserId, string ParticipantName,
    int CategoryId, string CategoryName, DateTime EnrolledAt);

// ---------- Results ----------
public class ResultRequest
{
    /// <summary>Finish time as hh:mm:ss, e.g. 01:45:30.</summary>
    [Required] public TimeSpan? FinishTime { get; set; }

    [Range(1, 100000)] public int Position { get; set; }
}

public record ResultResponse(int Id, int EnrolmentId, int EventId, string EventName, int UserId,
    string ParticipantName, string FinishTime, int Position);
