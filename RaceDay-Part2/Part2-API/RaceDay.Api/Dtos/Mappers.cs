using RaceDay.Api.Models;

namespace RaceDay.Api.Dtos;

public static class Mappers
{
    public static ProfileResponse ToProfile(this User u) =>
        new(u.Id, u.FirstName, u.LastName, u.Email, u.Role, u.PhoneNumber, u.DateOfBirth);

    public static EventResponse ToResponse(this Event e) =>
        new(e.Id, e.Name, e.Description, e.Date, e.Location, e.DistanceKm, e.EventType.ToString(), e.OrganiserId);

    public static CategoryResponse ToResponse(this Category c) =>
        new(c.Id, c.EventId, c.Name, c.MinAge, c.MaxAge, c.DistanceKm);

    public static EnrolmentResponse ToResponse(this Enrolment e) =>
        new(e.Id, e.EventId, e.Event?.Name ?? string.Empty, e.UserId,
            e.User is null ? string.Empty : $"{e.User.FirstName} {e.User.LastName}",
            e.CategoryId, e.Category?.Name ?? string.Empty, e.EnrolledAt);

    public static ResultResponse ToResponse(this RaceResult r) =>
        new(r.Id, r.EnrolmentId, r.Enrolment?.EventId ?? 0, r.Enrolment?.Event?.Name ?? string.Empty,
            r.Enrolment?.UserId ?? 0,
            r.Enrolment?.User is null ? string.Empty : $"{r.Enrolment.User.FirstName} {r.Enrolment.User.LastName}",
            r.FinishTime.ToString("c"), r.Position);
}
