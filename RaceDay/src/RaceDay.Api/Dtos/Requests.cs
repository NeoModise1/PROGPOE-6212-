using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos;

public class RegisterRequest
{
    [Required, StringLength(50)] public string FirstName { get; set; } = string.Empty;
    [Required, StringLength(50)] public string LastName { get; set; } = string.Empty;
    [Required, EmailAddress, StringLength(100)] public string Email { get; set; } = string.Empty;
    [Required, StringLength(100, MinimumLength = 8)] public string Password { get; set; } = string.Empty;
    /// <summary>Either "Organiser" or "Participant".</summary>
    [Required] public string Role { get; set; } = string.Empty;
    [StringLength(20)] public string? PhoneNumber { get; set; }
}

public class LoginRequest
{
    [Required] public string Email { get; set; } = string.Empty;
    [Required] public string Password { get; set; } = string.Empty;
}

public class UpdateProfileRequest
{
    [Required, StringLength(50)] public string FirstName { get; set; } = string.Empty;
    [Required, StringLength(50)] public string LastName { get; set; } = string.Empty;
    [StringLength(20)] public string? PhoneNumber { get; set; }
}

public class EventRequest
{
    [Required, StringLength(150)] public string EventName { get; set; } = string.Empty;
    [StringLength(1000)] public string? Description { get; set; }
    [Required] public DateTime? EventDate { get; set; }
    /// <summary>Format hh:mm:ss, e.g. 06:30:00</summary>
    [Required] public TimeSpan? StartTime { get; set; }
    [Required, StringLength(200)] public string Venue { get; set; } = string.Empty;
    [Required, StringLength(100)] public string City { get; set; } = string.Empty;
    [Required, StringLength(100)] public string Province { get; set; } = string.Empty;
}

public class EventUpdateRequest : EventRequest
{
    /// <summary>One of: Scheduled, Ongoing, Completed, Cancelled.</summary>
    [Required] public string Status { get; set; } = string.Empty;
}

public class CategoryRequest
{
    [Required, StringLength(100)] public string CategoryName { get; set; } = string.Empty;
    [Required, Range(0.01, 9999)] public decimal? DistanceKm { get; set; }
    /// <summary>One of: Run, Walk, Cycle.</summary>
    [Required, RegularExpression("^(?i:run|walk|cycle)$", ErrorMessage = "ActivityType must be Run, Walk or Cycle.")]
    public string ActivityType { get; set; } = string.Empty;
    [StringLength(500)] public string? Description { get; set; }
}

public class EventCategoryCreateRequest
{
    [Required] public int? CategoryId { get; set; }
    [Required, Range(0, 100000)] public decimal? EntryFee { get; set; }
    [Required, Range(1, 1000000)] public int? MaximumParticipants { get; set; }
}

public class EventCategoryUpdateRequest
{
    [Required, Range(0, 100000)] public decimal? EntryFee { get; set; }
    [Required, Range(1, 1000000)] public int? MaximumParticipants { get; set; }
}

public class ResultRequest
{
    /// <summary>Format hh:mm:ss, e.g. 00:45:12</summary>
    public TimeSpan? FinishTime { get; set; }
    [Range(1, 1000000)] public int? Position { get; set; }
    [Range(0, 9999)] public decimal? AveragePace { get; set; }
    /// <summary>One of: Completed, DNF, DNS, Disqualified.</summary>
    [Required] public string ResultStatus { get; set; } = string.Empty;
}

public class UserEventUpdateRequest
{
    [StringLength(30)] public string? RaceNumber { get; set; }
    /// <summary>One of: Registered, Confirmed, Cancelled.</summary>
    [Required] public string Status { get; set; } = string.Empty;
}

public class RouteRequest
{
    [Required, StringLength(150)] public string RouteName { get; set; } = string.Empty;
    [Required, Range(0.01, 9999)] public decimal? DistanceKm { get; set; }
    [Required, Range(0, 9999)] public decimal? ElevationGain { get; set; }
    [StringLength(500)] public string? Description { get; set; }
    [StringLength(500)] public string? MapUrl { get; set; }
}

public class WeatherRequest
{
    [Required, Range(-90, 70)] public decimal? Temperature { get; set; }
    [Required, StringLength(100)] public string WeatherCondition { get; set; } = string.Empty;
    [Required, Range(0, 500)] public decimal? WindSpeed { get; set; }
    [Required, Range(0, 100)] public decimal? Humidity { get; set; }
}
