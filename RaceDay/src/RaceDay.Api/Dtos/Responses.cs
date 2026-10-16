namespace RaceDay.Api.Dtos;

public record RegisterResponse(int UserId, string Role);
public record LoginResponse(string Token, int UserId, string Role);

public record UserResponse(int UserId, string FirstName, string LastName, string Email,
    string? PhoneNumber, string Role, DateTime CreatedAt);

public record EventResponse(int EventId, int OrganiserId, string EventName, string? Description,
    DateTime EventDate, TimeSpan StartTime, string Venue, string City, string Province,
    string Status, DateTime CreatedAt);

public record CategoryResponse(int CategoryId, string CategoryName, decimal DistanceKm,
    string ActivityType, string? Description);

public record EventCategoryResponse(int EventCategoryId, int EventId, int CategoryId, string CategoryName,
    decimal DistanceKm, string ActivityType, decimal EntryFee, int MaximumParticipants, int EnrolledCount);

public record EnrollmentResponse(int EnrollmentId, int ParticipantId, string ParticipantName,
    int EventCategoryId, int EventId, string EventName, string CategoryName,
    DateTime EnrollmentDate, string Status);

public record RegistrationResponse(int UserEventId, int ParticipantId, string ParticipantName,
    int EventId, string EventName, DateTime EventDate, DateTime RegistrationDate,
    string? RaceNumber, string Status);

public record ResultResponse(int ResultId, int EnrollmentId, int ParticipantId, string ParticipantName,
    int EventCategoryId, string EventName, string CategoryName, TimeSpan? FinishTime,
    int? Position, decimal? AveragePace, string ResultStatus, DateTime RecordedAt);

public record RouteResponse(int RouteId, int EventId, string RouteName, decimal DistanceKm,
    decimal ElevationGain, string? Description, string? MapUrl);

public record WeatherResponse(int WeatherId, int EventId, decimal Temperature, string WeatherCondition,
    decimal WindSpeed, decimal Humidity, DateTime RecordedAt);
