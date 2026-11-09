using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

public class EnrollmentsController : ApiControllerBase
{
    public EnrollmentsController(RaceDayDbContext db) : base(db) { }

    /// <summary>Enrols the logged-in participant into a specific event category.</summary>
    /// <remarks>Also creates the participant's event registration (UserEvent) if they are not yet registered for the event.</remarks>
    /// <response code="201">Created - returns EnrollmentID and status.</response>
    /// <response code="404">Not Found - event category does not exist.</response>
    /// <response code="409">Conflict - already enrolled, category full, or event closed.</response>
    [HttpPost("api/event-categories/{eventCategoryId:int}/enrollments")]
    [Authorize(Roles = "Participant")]
    [ProducesResponseType(typeof(EnrollmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Enrol(int eventCategoryId)
    {
        var ec = await Db.EventCategories.Include(x => x.Event)
            .FirstOrDefaultAsync(x => x.EventCategoryID == eventCategoryId);
        if (ec is null) return NotFound();

        var participantId = await GetParticipantIdAsync();
        if (participantId is null) return Forbid();

        if (ec.Event.Status == "Completed" || ec.Event.Status == "Cancelled")
            return ConflictProblem($"This event is {ec.Event.Status.ToLowerInvariant()} and no longer accepts enrolments.");

        if (await Db.Enrollments.AnyAsync(e => e.ParticipantID == participantId && e.EventCategoryID == eventCategoryId))
            return ConflictProblem("You are already enrolled in this category.");

        var enrolled = await Db.Enrollments.CountAsync(e => e.EventCategoryID == eventCategoryId);
        if (enrolled >= ec.MaximumParticipants)
            return ConflictProblem("This category is full.");

        var enrollment = new Enrollment
        {
            ParticipantID = participantId.Value,
            EventCategoryID = eventCategoryId,
            Status = "Registered"
        };
        Db.Enrollments.Add(enrollment);

        var eventId = ec.EventID;
        if (!await Db.UserEvents.AnyAsync(u => u.ParticipantID == participantId && u.EventID == eventId))
        {
            Db.UserEvents.Add(new UserEvent
            {
                ParticipantID = participantId.Value,
                EventID = eventId,
                Status = "Registered"
            });
        }

        await Db.SaveChangesAsync();

        var saved = await Db.Enrollments.WithDetails().SingleAsync(e => e.EnrollmentID == enrollment.EnrollmentID);
        return StatusCode(StatusCodes.Status201Created, saved.ToResponse());
    }

    /// <summary>Returns the logged-in participant's own enrolments and history.</summary>
    /// <response code="200">OK - returns array of enrolments.</response>
    [HttpGet("api/users/me/enrollments")]
    [Authorize(Roles = "Participant")]
    [ProducesResponseType(typeof(IEnumerable<EnrollmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMine()
    {
        var participantId = await GetParticipantIdAsync();
        if (participantId is null) return Forbid();

        var items = await Db.Enrollments.WithDetails()
            .Where(e => e.ParticipantID == participantId)
            .OrderByDescending(e => e.EnrollmentDate)
            .ToListAsync();
        return Ok(items.Select(e => e.ToResponse()));
    }

    /// <summary>Returns all enrolments for an event (owning Organiser only).</summary>
    /// <response code="200">OK - returns array of enrolments.</response>
    /// <response code="403">Forbidden.</response>
    /// <response code="404">Not Found - event does not exist.</response>
    [HttpGet("api/events/{eventId:int}/enrollments")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(IEnumerable<EnrollmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        var ev = await Db.Events.FindAsync(eventId);
        if (ev is null) return NotFound();
        if (!await IsEventOwnerAsync(ev.OrganiserID)) return Forbid();

        var items = await Db.Enrollments.WithDetails()
            .Where(e => e.EventCategory.EventID == eventId)
            .OrderBy(e => e.EnrollmentID)
            .ToListAsync();
        return Ok(items.Select(e => e.ToResponse()));
    }

    /// <summary>Cancels the logged-in participant's own enrolment.</summary>
    /// <response code="204">No Content.</response>
    /// <response code="403">Forbidden - not your enrolment.</response>
    /// <response code="404">Not Found.</response>
    /// <response code="409">Conflict - a result has already been recorded.</response>
    [HttpDelete("api/enrollments/{id:int}")]
    [Authorize(Roles = "Participant")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(int id)
    {
        var enrollment = await Db.Enrollments.Include(e => e.EventCategory)
            .FirstOrDefaultAsync(e => e.EnrollmentID == id);
        if (enrollment is null) return NotFound();

        var participantId = await GetParticipantIdAsync();
        if (participantId is null || enrollment.ParticipantID != participantId.Value) return Forbid();

        if (await Db.Results.AnyAsync(r => r.EnrollmentID == id))
            return ConflictProblem("A result has already been recorded for this enrolment.");

        var eventId = enrollment.EventCategory.EventID;
        Db.Enrollments.Remove(enrollment);

        var hasOthers = await Db.Enrollments.AnyAsync(e =>
            e.ParticipantID == participantId && e.EnrollmentID != id && e.EventCategory.EventID == eventId);
        if (!hasOthers)
        {
            var registration = await Db.UserEvents
                .FirstOrDefaultAsync(u => u.ParticipantID == participantId && u.EventID == eventId);
            if (registration is not null) Db.UserEvents.Remove(registration);
        }

        await Db.SaveChangesAsync();
        return NoContent();
    }
}
