using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Auth;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

/// <summary>Race results. Organisers capture them; Participants view their own.</summary>
[Route("api")]
[SessionAuthorize]
public class ResultsController : RaceDayControllerBase
{
    private readonly RaceDayDbContext _db;

    public ResultsController(RaceDayDbContext db) => _db = db;

    /// <summary>Capture the finish time and position for an enrolment (Organiser only).</summary>
    [HttpPost("enrolments/{enrolmentId:int}/result")]
    [SessionAuthorize(Roles.Organiser)]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public Task<IActionResult> Capture(int enrolmentId, [FromBody] ResultRequest request) =>
        Upsert(enrolmentId, request, create: true);

    /// <summary>Correct an already captured result (Organiser only).</summary>
    [HttpPut("enrolments/{enrolmentId:int}/result")]
    [SessionAuthorize(Roles.Organiser)]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public Task<IActionResult> Correct(int enrolmentId, [FromBody] ResultRequest request) =>
        Upsert(enrolmentId, request, create: false);

    /// <summary>View all results for one of my events (Organiser only).</summary>
    [HttpGet("events/{eventId:int}/results")]
    [SessionAuthorize(Roles.Organiser)]
    [ProducesResponseType(typeof(IEnumerable<ResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        var ev = await _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.Id == eventId);
        if (ev is null) return NotFound();
        if (ev.OrganiserId != CurrentUserId) return Forbidden("You can only view results for events you organise.");

        var results = await _db.Results.AsNoTracking()
            .Include(r => r.Enrolment!).ThenInclude(e => e.User)
            .Include(r => r.Enrolment!).ThenInclude(e => e.Event)
            .Where(r => r.Enrolment!.EventId == eventId)
            .OrderBy(r => r.Position)
            .ToListAsync();

        return Ok(results.Select(r => r.ToResponse()));
    }

    /// <summary>View my own results (Participant only).</summary>
    [HttpGet("results/mine")]
    [SessionAuthorize(Roles.Participant)]
    [ProducesResponseType(typeof(IEnumerable<ResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMine()
    {
        var results = await _db.Results.AsNoTracking()
            .Include(r => r.Enrolment!).ThenInclude(e => e.User)
            .Include(r => r.Enrolment!).ThenInclude(e => e.Event)
            .Where(r => r.Enrolment!.UserId == CurrentUserId)
            .OrderBy(r => r.Enrolment!.EventId)
            .ToListAsync();

        return Ok(results.Select(r => r.ToResponse()));
    }

    private async Task<IActionResult> Upsert(int enrolmentId, ResultRequest request, bool create)
    {
        var enrolment = await _db.Enrolments
            .Include(e => e.Event).Include(e => e.User).Include(e => e.Result)
            .FirstOrDefaultAsync(e => e.Id == enrolmentId);

        if (enrolment is null) return NotFound();
        if (enrolment.Event!.OrganiserId != CurrentUserId)
            return Forbidden("You can only capture results for events you organise.");

        var finishTime = request.FinishTime!.Value;
        if (finishTime <= TimeSpan.Zero || finishTime >= TimeSpan.FromHours(24))
            return BadRequestProblem("FinishTime must be greater than 00:00:00 and less than 24:00:00.");

        var existing = enrolment.Result;
        if (create && existing is not null)
            return ConflictProblem("A result has already been captured for this enrolment. Use PUT to correct it.");
        if (!create && existing is null)
            return NotFound();

        var positionTaken = await _db.Results.AnyAsync(r =>
            r.Enrolment!.EventId == enrolment.EventId && r.Position == request.Position &&
            (existing == null || r.Id != existing.Id));
        if (positionTaken)
            return ConflictProblem($"Position {request.Position} has already been awarded in this event.");

        RaceResult result;
        if (existing is null)
        {
            result = new RaceResult { EnrolmentId = enrolmentId, FinishTime = finishTime, Position = request.Position };
            _db.Results.Add(result);
        }
        else
        {
            result = existing;
            result.FinishTime = finishTime;
            result.Position = request.Position;
        }

        await _db.SaveChangesAsync();
        result.Enrolment = enrolment;

        return create
            ? StatusCode(StatusCodes.Status201Created, result.ToResponse())
            : Ok(result.ToResponse());
    }
}
