using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;

namespace RaceDay.Api.Controllers;

/// <summary>Event registrations (race number and registration status).</summary>
public class UserEventsController : ApiControllerBase
{
    private static readonly string[] AllowedStatuses = { "Registered", "Confirmed", "Cancelled" };

    public UserEventsController(RaceDayDbContext db) : base(db) { }

    /// <summary>Returns all events the logged-in participant is registered for, with race number and status.</summary>
    /// <response code="200">OK - returns array of registrations.</response>
    [HttpGet("api/users/me/events")]
    [Authorize(Roles = "Participant")]
    [ProducesResponseType(typeof(IEnumerable<RegistrationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMine()
    {
        var participantId = await GetParticipantIdAsync();
        if (participantId is null) return Forbid();

        var items = await Db.UserEvents.WithDetails()
            .Where(u => u.ParticipantID == participantId)
            .OrderBy(u => u.Event.EventDate)
            .ToListAsync();
        return Ok(items.Select(u => u.ToResponse()));
    }

    /// <summary>Returns all registered participants for an event (owning Organiser only).</summary>
    /// <response code="200">OK - returns array of registrations.</response>
    /// <response code="403">Forbidden.</response>
    /// <response code="404">Not Found - event does not exist.</response>
    [HttpGet("api/events/{eventId:int}/registrations")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(IEnumerable<RegistrationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        var ev = await Db.Events.FindAsync(eventId);
        if (ev is null) return NotFound();
        if (!await IsEventOwnerAsync(ev.OrganiserID)) return Forbid();

        var items = await Db.UserEvents.WithDetails()
            .Where(u => u.EventID == eventId)
            .OrderBy(u => u.UserEventID)
            .ToListAsync();
        return Ok(items.Select(u => u.ToResponse()));
    }

    /// <summary>Assigns or updates a participant's race number and registration status (owning Organiser only).</summary>
    /// <response code="200">OK - returns updated registration.</response>
    /// <response code="403">Forbidden.</response>
    /// <response code="404">Not Found.</response>
    /// <response code="409">Conflict - race number already used in this event.</response>
    [HttpPut("api/user-events/{id:int}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(RegistrationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(int id, [FromBody] UserEventUpdateRequest request)
    {
        var registration = await Db.UserEvents.WithDetails().FirstOrDefaultAsync(u => u.UserEventID == id);
        if (registration is null) return NotFound();
        if (!await IsEventOwnerAsync(registration.Event.OrganiserID)) return Forbid();

        var status = AllowedStatuses.FirstOrDefault(s => s.Equals(request.Status.Trim(), StringComparison.OrdinalIgnoreCase));
        if (status is null)
            return ValidationError(nameof(request.Status), $"Status must be one of: {string.Join(", ", AllowedStatuses)}.");

        var raceNumber = string.IsNullOrWhiteSpace(request.RaceNumber) ? null : request.RaceNumber.Trim();
        if (raceNumber is not null)
        {
            var eventId = registration.EventID;
            var taken = await Db.UserEvents.AnyAsync(u =>
                u.EventID == eventId && u.UserEventID != id && u.RaceNumber == raceNumber);
            if (taken) return ConflictProblem("That race number is already assigned to another participant in this event.");
        }

        registration.RaceNumber = raceNumber;
        registration.Status = status;
        await Db.SaveChangesAsync();

        return Ok(registration.ToResponse());
    }
}
