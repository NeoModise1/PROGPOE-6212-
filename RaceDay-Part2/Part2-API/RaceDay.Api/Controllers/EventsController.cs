using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Auth;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

/// <summary>Events. Both roles can view; only Organisers can create, update and delete (their own events).</summary>
[Route("api/events")]
[SessionAuthorize]
public class EventsController : RaceDayControllerBase
{
    private readonly RaceDayDbContext _db;

    public EventsController(RaceDayDbContext db) => _db = db;

    /// <summary>List all events (Organiser and Participant).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<EventResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll()
    {
        var events = await _db.Events.AsNoTracking().OrderBy(e => e.Date).ToListAsync();
        return Ok(events.Select(e => e.ToResponse()));
    }

    /// <summary>Get one event by id (Organiser and Participant).</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var ev = await _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
        return ev is null ? NotFound() : Ok(ev.ToResponse());
    }

    /// <summary>Create an event (Organiser only).</summary>
    [HttpPost]
    [SessionAuthorize(Roles.Organiser)]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] EventRequest request)
    {
        var ev = new Event
        {
            Name = request.Name.Trim(),
            Description = request.Description,
            Date = request.Date!.Value,
            Location = request.Location.Trim(),
            DistanceKm = request.DistanceKm,
            EventType = request.EventType!.Value,
            OrganiserId = CurrentUserId
        };

        _db.Events.Add(ev);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = ev.Id }, ev.ToResponse());
    }

    /// <summary>Update one of my events (Organiser only).</summary>
    [HttpPut("{id:int}")]
    [SessionAuthorize(Roles.Organiser)]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] EventRequest request)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev is null) return NotFound();
        if (ev.OrganiserId != CurrentUserId) return Forbidden("You can only change events you organise.");

        ev.Name = request.Name.Trim();
        ev.Description = request.Description;
        ev.Date = request.Date!.Value;
        ev.Location = request.Location.Trim();
        ev.DistanceKm = request.DistanceKm;
        ev.EventType = request.EventType!.Value;
        await _db.SaveChangesAsync();

        return Ok(ev.ToResponse());
    }

    /// <summary>Delete one of my events, including its categories, enrolments and results (Organiser only).</summary>
    [HttpDelete("{id:int}")]
    [SessionAuthorize(Roles.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var ev = await _db.Events
            .Include(e => e.Categories)
            .Include(e => e.Enrolments).ThenInclude(en => en.Result)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (ev is null) return NotFound();
        if (ev.OrganiserId != CurrentUserId) return Forbidden("You can only delete events you organise.");

        _db.Events.Remove(ev);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
