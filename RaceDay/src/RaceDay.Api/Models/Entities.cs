namespace RaceDay.Api.Models;

public class AppUser
{
    public int UserID { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Organiser? Organiser { get; set; }
    public Participant? Participant { get; set; }
}

public class Organiser
{
    public int OrganiserID { get; set; }
    public int UserID { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public AppUser User { get; set; } = null!;
    public ICollection<RaceEvent> Events { get; set; } = new List<RaceEvent>();
}

public class Participant
{
    public int ParticipantID { get; set; }
    public int UserID { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public AppUser User { get; set; } = null!;
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public ICollection<UserEvent> UserEvents { get; set; } = new List<UserEvent>();
}

public class RaceEvent
{
    public int EventID { get; set; }
    public int OrganiserID { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime EventDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public string Venue { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string Status { get; set; } = "Scheduled";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Organiser Organiser { get; set; } = null!;
    public ICollection<EventCategory> EventCategories { get; set; } = new List<EventCategory>();
    public ICollection<RaceRoute> Routes { get; set; } = new List<RaceRoute>();
    public ICollection<WeatherInformation> WeatherRecords { get; set; } = new List<WeatherInformation>();
    public ICollection<UserEvent> UserEvents { get; set; } = new List<UserEvent>();
}

public class Category
{
    public int CategoryID { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal DistanceKM { get; set; }
    public string ActivityType { get; set; } = string.Empty;
    public string? Description { get; set; }

    public ICollection<EventCategory> EventCategories { get; set; } = new List<EventCategory>();
}

public class EventCategory
{
    public int EventCategoryID { get; set; }
    public int EventID { get; set; }
    public int CategoryID { get; set; }
    public decimal EntryFee { get; set; }
    public int MaximumParticipants { get; set; }

    public RaceEvent Event { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}

public class Enrollment
{
    public int EnrollmentID { get; set; }
    public int ParticipantID { get; set; }
    public int EventCategoryID { get; set; }
    public DateTime EnrollmentDate { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Registered";

    public Participant Participant { get; set; } = null!;
    public EventCategory EventCategory { get; set; } = null!;
    public RaceResult? Result { get; set; }
}

public class UserEvent
{
    public int UserEventID { get; set; }
    public int ParticipantID { get; set; }
    public int EventID { get; set; }
    public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;
    public string? RaceNumber { get; set; }
    public string Status { get; set; } = "Registered";

    public Participant Participant { get; set; } = null!;
    public RaceEvent Event { get; set; } = null!;
}

public class RaceResult
{
    public int ResultID { get; set; }
    public int EnrollmentID { get; set; }
    public TimeSpan? FinishTime { get; set; }
    public int? Position { get; set; }
    public decimal? AveragePace { get; set; }
    public string ResultStatus { get; set; } = "Completed";
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    public Enrollment Enrollment { get; set; } = null!;
}

public class RaceRoute
{
    public int RouteID { get; set; }
    public int EventID { get; set; }
    public string RouteName { get; set; } = string.Empty;
    public decimal DistanceKM { get; set; }
    public decimal ElevationGain { get; set; }
    public string? Description { get; set; }
    public string? MapURL { get; set; }

    public RaceEvent Event { get; set; } = null!;
}

public class WeatherInformation
{
    public int WeatherID { get; set; }
    public int EventID { get; set; }
    public decimal Temperature { get; set; }
    public string WeatherCondition { get; set; } = string.Empty;
    public decimal WindSpeed { get; set; }
    public decimal Humidity { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    public RaceEvent Event { get; set; } = null!;
}
