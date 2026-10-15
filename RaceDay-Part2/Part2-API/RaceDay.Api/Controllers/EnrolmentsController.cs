using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Auth;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

/// <summary>Event enrolments. Participants enrol; Organisers view the enrolments for their events.</summary>
[Route("api")]
[SessionAuthorize]
public class EnrolmentsController : RaceDayControllerBase
{
    private readonly RaceDayDbContext _db;

    public EnrolmentsController(RaceDayDbContext db) => _db = db;

    /// <summary>Enter an event by choosing one of its categories (Participant only).</summary>
    [HttpPost("events/{eventId:int}/enrolments")]
    [SessionAuthorize(Roles.Participant)]
    [ProducesResponseType(typeof(EnrolmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Enrol(int eventId, [FromBody] EnrolRequest request)
    {
        var ev = await _db.Events.FindAsync(eventId);
        if (ev is null) return NotFound();

        var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == request.CategoryId && c.EventId == eventId);
        if (category is null)
            return BadRequestProblem("The selected category does not belong to this event.");

        if (await _db.Enrolments.AnyAsync(e => e.UserId == CurrentUserId && e.EventId == eventId))
            return ConflictProblem("You are already enrolled in this event.");

        var user = await _db.Users.FindAsync(CurrentUserId);
        if (user is null) return Unauthorized();

        var enrolment = new Enrolment
        {
            UserId = user.Id,
            EventId = eventId,
            CategoryId = category.Id,
            EnrolledAt = DateTime.UtcNow
        };
        _db.Enrolments.Add(enrolment);
        await _db.SaveChangesAsync();

        enrolment.User = user;
        enrolment.Event = ev;
        enrolment.Category = category;
        return StatusCode(StatusCodes.Status201Created, enrolment.ToResponse());
    }

    /// <summary>View all enrolments for one of my events (Organiser only).</summary>
    [HttpGet("events/{eventId:int}/enrolments")]
    [SessionAuthorize(Roles.Organiser)]
    [ProducesResponseType(typeof(IEnumerable<EnrolmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        var ev = await _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.Id == eventId);
        if (ev is null) return NotFound();
        if (ev.OrganiserId != CurrentUserId) return Forbidden("You can only view enrolments for events you organise.");

        var enrolments = await _db.Enrolments.AsNoTracking()
            .Include(e => e.User).Include(e => e.Event).Include(e => e.Category)
            .Where(e => e.EventId == eventId)
            .OrderBy(e => e.EnrolledAt)
            .ToListAsync();

        return Ok(enrolments.Select(e => e.ToResponse()));
    }

    /// <summary>View my own enrolments (Participant only).</summary>
    [HttpGet("enrolments/mine")]
    [SessionAuthorize(Roles.Participant)]
    [ProducesResponseType(typeof(IEnumerable<EnrolmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMine()
    {
        var enrolments = await _db.Enrolments.AsNoTracking()
            .Include(e => e.User).Include(e => e.Event).Include(e => e.Category)
            .Where(e => e.UserId == CurrentUserId)
            .OrderBy(e => e.EnrolledAt)
            .ToListAsync();

        return Ok(enrolments.Select(e => e.ToResponse()));
    }
}
