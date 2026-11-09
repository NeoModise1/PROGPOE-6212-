namespace RaceDay.Web.Models;

// Shapes returned by the RaceDay API (Part 2). Property names match the API's camelCase JSON.

public class LoginDto
{
    public string Token { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string Role { get; set; } = string.Empty;
}

public class UserDto
{
    public int UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string Role { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public string FullName => $"{FirstName} {LastName}".Trim();
}

public class EventDto
{
    public int EventId { get; set; }
    public int OrganiserId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime EventDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public string Venue { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string EventType { get; set; } = "Run";
    public decimal? DistanceKm { get; set; }
    public string? BannerUrl { get; set; }

    public string Location => $"{Venue}, {City}, {Province}";
    public bool IsUpcoming => EventDate.Date >= DateTime.Today && (Status == "Scheduled" || Status == "Ongoing");
}

public class CategoryDto
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public string ActivityType { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class EventCategoryDto
{
    public int EventCategoryId { get; set; }
    public int EventId { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public string ActivityType { get; set; } = string.Empty;
    public decimal EntryFee { get; set; }
    public int MaximumParticipants { get; set; }
    public int EnrolledCount { get; set; }
    public bool IsFull => EnrolledCount >= MaximumParticipants;
}

public class EnrollmentDto
{
    public int EnrollmentId { get; set; }
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public int EventCategoryId { get; set; }
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public DateTime EnrollmentDate { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class RegistrationDto
{
    public int UserEventId { get; set; }
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public DateTime RegistrationDate { get; set; }
    public string? RaceNumber { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class ResultDto
{
    public int ResultId { get; set; }
    public int EnrollmentId { get; set; }
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public int EventCategoryId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public TimeSpan? FinishTime { get; set; }
    public int? Position { get; set; }
    public decimal? AveragePace { get; set; }
    public string ResultStatus { get; set; } = string.Empty;
}

public class RouteDto
{
    public int RouteId { get; set; }
    public string RouteName { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public decimal ElevationGain { get; set; }
    public string? Description { get; set; }
    public string? MapUrl { get; set; }
}

public class WeatherDto
{
    public int WeatherId { get; set; }
    public decimal Temperature { get; set; }
    public string WeatherCondition { get; set; } = string.Empty;
    public decimal WindSpeed { get; set; }
    public decimal Humidity { get; set; }
    public DateTime RecordedAt { get; set; }
}

public class ImageUploadDto
{
    public string Url { get; set; } = string.Empty;
}
