using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Dtos;

public static class Mapping
{
    public static UserResponse ToResponse(this AppUser u) =>
        new(u.UserID, u.FirstName, u.LastName, u.Email, u.PhoneNumber, u.Role, u.CreatedAt);

    public static EventResponse ToResponse(this RaceEvent e) =>
        new(e.EventID, e.OrganiserID, e.EventName, e.Description, e.EventDate, e.StartTime,
            e.Venue, e.City, e.Province, e.Status, e.CreatedAt);

    public static CategoryResponse ToResponse(this Category c) =>
        new(c.CategoryID, c.CategoryName, c.DistanceKM, c.ActivityType, c.Description);

    public static RouteResponse ToResponse(this RaceRoute r) =>
        new(r.RouteID, r.EventID, r.RouteName, r.DistanceKM, r.ElevationGain, r.Description, r.MapURL);

    public static WeatherResponse ToResponse(this WeatherInformation w) =>
        new(w.WeatherID, w.EventID, w.Temperature, w.WeatherCondition, w.WindSpeed, w.Humidity, w.RecordedAt);

    // The following mappers expect the navigation properties to be loaded (see the WithDetails helpers).
    public static EnrollmentResponse ToResponse(this Enrollment e) =>
        new(e.EnrollmentID, e.ParticipantID,
            $"{e.Participant.User.FirstName} {e.Participant.User.LastName}",
            e.EventCategoryID, e.EventCategory.EventID, e.EventCategory.Event.EventName,
            e.EventCategory.Category.CategoryName, e.EnrollmentDate, e.Status);

    public static RegistrationResponse ToResponse(this UserEvent u) =>
        new(u.UserEventID, u.ParticipantID,
            $"{u.Participant.User.FirstName} {u.Participant.User.LastName}",
            u.EventID, u.Event.EventName, u.Event.EventDate, u.RegistrationDate, u.RaceNumber, u.Status);

    public static ResultResponse ToResponse(this RaceResult r) =>
        new(r.ResultID, r.EnrollmentID, r.Enrollment.ParticipantID,
            $"{r.Enrollment.Participant.User.FirstName} {r.Enrollment.Participant.User.LastName}",
            r.Enrollment.EventCategoryID, r.Enrollment.EventCategory.Event.EventName,
            r.Enrollment.EventCategory.Category.CategoryName, r.FinishTime, r.Position,
            r.AveragePace, r.ResultStatus, r.RecordedAt);

    public static IQueryable<Enrollment> WithDetails(this IQueryable<Enrollment> q) =>
        q.Include(e => e.Participant).ThenInclude(p => p.User)
         .Include(e => e.EventCategory).ThenInclude(ec => ec.Event)
         .Include(e => e.EventCategory).ThenInclude(ec => ec.Category);

    public static IQueryable<UserEvent> WithDetails(this IQueryable<UserEvent> q) =>
        q.Include(u => u.Participant).ThenInclude(p => p.User)
         .Include(u => u.Event);

    public static IQueryable<RaceResult> WithDetails(this IQueryable<RaceResult> q) =>
        q.Include(r => r.Enrollment).ThenInclude(e => e.Participant).ThenInclude(p => p.User)
         .Include(r => r.Enrollment).ThenInclude(e => e.EventCategory).ThenInclude(ec => ec.Event)
         .Include(r => r.Enrollment).ThenInclude(e => e.EventCategory).ThenInclude(ec => ec.Category);

    /// <summary>Projects event categories (including the number of enrolments) straight in SQL.</summary>
    public static IQueryable<EventCategoryResponse> ToResponses(this IQueryable<EventCategory> q) =>
        q.Select(ec => new EventCategoryResponse(
            ec.EventCategoryID, ec.EventID, ec.CategoryID, ec.Category.CategoryName,
            ec.Category.DistanceKM, ec.Category.ActivityType, ec.EntryFee,
            ec.MaximumParticipants, ec.Enrollments.Count()));
}
