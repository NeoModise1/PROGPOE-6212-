using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

public class EventsController : ApiControllerBase
{
    private static readonly string[] AllowedStatuses = { "Scheduled", "Ongoing", "Completed", "Cancelled" };

    public EventsController(RaceDayDbContext db) : base(db) { }

    /// <summary>Returns a list of all events.</summary>
    /// <response code="200">OK - returns array of events.</response>
    [HttpGet("api/events")]
    [ProducesResponseType(typeof(IEnumerable<EventResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEvents()
    {
        var events = await Db.Events.OrderBy(e => e.EventDate).ThenBy(e => e.EventID).ToListAsync();
        return Ok(events.Select(e => e.ToResponse()));
    }

    /// <summary>Returns full details for a single event.</summary>
    /// <response code="200">OK - returns event details.</response>
    /// <response code="404">Not Found.</response>
    [HttpGet("api/events/{id:int}")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEvent(int id)
    {
        var ev = await Db.Events.FindAsync(id);
        return ev is null ? NotFound() : Ok(ev.ToResponse());
    }

    /// <summary>Creates a new event (Organiser only). The event is owned by the logged-in organiser.</summary>
    /// <response code="201">Created - returns new event with EventID.</response>
    /// <response code="400">Bad Request.</response>
    /// <response code="401">Unauthorized - no/invalid token.</response>
    /// <response code="403">Forbidden - caller is not an Organiser.</response>
    [HttpPost("api/events")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateEvent([FromBody] EventRequest request)
    {
        var organiserId = await GetOrganiserIdAsync();
        if (organiserId is null) return Forbid();

        var ev = new RaceEvent
        {
            OrganiserID = organiserId.Value,
            EventName = request.EventName.Trim(),
            Description = request.Description?.Trim(),
            EventDate = request.EventDate!.Value.Date,
            StartTime = request.StartTime!.Value,
            Venue = request.Venue.Trim(),
            City = request.City.Trim(),
            Province = request.Province.Trim(),
            Status = "Scheduled"
        };

        Db.Events.Add(ev);
        await Db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetEvent), new { id = ev.EventID }, ev.ToResponse());
    }

    /// <summary>Updates an existing event's details (owning Organiser only).</summary>
    /// <response code="200">OK - returns updated event.</response>
    /// <response code="400">Bad Request.</response>
    /// <response code="403">Forbidden - not the owner / not an Organiser.</response>
    /// <response code="404">Not Found.</response>
    [HttpPut("api/events/{id:int}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEvent(int id, [FromBody] EventUpdateRequest request)
    {
        var ev = await Db.Events.FindAsync(id);
        if (ev is null) return NotFound();
        if (!await IsEventOwnerAsync(ev.OrganiserID)) return Forbid();

        var status = AllowedStatuses.FirstOrDefault(s => s.Equals(request.Status.Trim(), StringComparison.OrdinalIgnoreCase));
        if (status is null)
            return ValidationError(nameof(request.Status), $"Status must be one of: {string.Join(", ", AllowedStatuses)}.");

        ev.EventName = request.EventName.Trim();
        ev.Description = request.Description?.Trim();
        ev.EventDate = request.EventDate!.Value.Date;
        ev.StartTime = request.StartTime!.Value;
        ev.Venue = request.Venue.Trim();
        ev.City = request.City.Trim();
        ev.Province = request.Province.Trim();
        ev.Status = status;
        await Db.SaveChangesAsync();

        return Ok(ev.ToResponse());
    }

    /// <summary>Deletes an event and its associated categories, routes and weather records (owning Organiser only).</summary>
    /// <response code="204">No Content.</response>
    /// <response code="403">Forbidden.</response>
    /// <response code="404">Not Found.</response>
    [HttpDelete("api/events/{id:int}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var ev = await Db.Events.FindAsync(id);
        if (ev is null) return NotFound();
        if (!await IsEventOwnerAsync(ev.OrganiserID)) return Forbid();

        Db.Events.Remove(ev);
        await Db.SaveChangesAsync();
        return NoContent();
    }
}
