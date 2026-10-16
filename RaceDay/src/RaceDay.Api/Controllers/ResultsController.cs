using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

public class ResultsController : ApiControllerBase
{
    private static readonly string[] AllowedStatuses = { "Completed", "DNF", "DNS", "Disqualified" };

    public ResultsController(RaceDayDbContext db) : base(db) { }

    /// <summary>Captures a finish result for a specific enrolment (owning Organiser only).</summary>
    /// <remarks>ResultStatus must be Completed, DNF, DNS or Disqualified. A "Completed" result requires FinishTime and Position.</remarks>
    /// <response code="201">Created - returns new result.</response>
    /// <response code="403">Forbidden.</response>
    /// <response code="404">Not Found - enrolment does not exist.</response>
    /// <response code="409">Conflict - result already captured.</response>
    [HttpPost("api/enrollments/{enrollmentId:int}/results")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Capture(int enrollmentId, [FromBody] ResultRequest request)
    {
        var enrollment = await Db.Enrollments
            .Include(e => e.EventCategory).ThenInclude(ec => ec.Event)
            .FirstOrDefaultAsync(e => e.EnrollmentID == enrollmentId);
        if (enrollment is null) return NotFound();
        if (!await IsEventOwnerAsync(enrollment.EventCategory.Event.OrganiserID)) return Forbid();

        var status = AllowedStatuses.FirstOrDefault(s => s.Equals(request.ResultStatus.Trim(), StringComparison.OrdinalIgnoreCase));
        if (status is null)
            return ValidationError(nameof(request.ResultStatus), $"ResultStatus must be one of: {string.Join(", ", AllowedStatuses)}.");

        if (status == "Completed" && (request.FinishTime is null || request.Position is null))
            return ValidationError(nameof(request.FinishTime), "A 'Completed' result requires both FinishTime and Position.");

        if (await Db.Results.AnyAsync(r => r.EnrollmentID == enrollmentId))
            return ConflictProblem("A result has already been captured for this enrolment.");

        var result = new RaceResult
        {
            EnrollmentID = enrollmentId,
            FinishTime = request.FinishTime,
            Position = request.Position,
            AveragePace = request.AveragePace,
            ResultStatus = status
        };
        Db.Results.Add(result);
        enrollment.Status = status;
        await Db.SaveChangesAsync();

        var saved = await Db.Results.WithDetails().SingleAsync(r => r.ResultID == result.ResultID);
        return StatusCode(StatusCodes.Status201Created, saved.ToResponse());
    }

    /// <summary>Returns the full results list for an event category, ordered by position.</summary>
    /// <response code="200">OK - returns array of results ordered by position.</response>
    /// <response code="404">Not Found - event category does not exist.</response>
    [HttpGet("api/event-categories/{eventCategoryId:int}/results")]
    [ProducesResponseType(typeof(IEnumerable<ResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForCategory(int eventCategoryId)
    {
        if (!await Db.EventCategories.AnyAsync(ec => ec.EventCategoryID == eventCategoryId)) return NotFound();

        var items = await Db.Results.WithDetails()
            .Where(r => r.Enrollment.EventCategoryID == eventCategoryId)
            .ToListAsync();

        var ordered = items
            .OrderBy(r => r.Position.HasValue ? 0 : 1)
            .ThenBy(r => r.Position)
            .ThenBy(r => r.ResultID)
            .Select(r => r.ToResponse());
        return Ok(ordered);
    }

    /// <summary>Returns the logged-in participant's own personal performance history.</summary>
    /// <response code="200">OK - returns array of the participant's results.</response>
    [HttpGet("api/users/me/results")]
    [Authorize(Roles = "Participant")]
    [ProducesResponseType(typeof(IEnumerable<ResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMine()
    {
        var participantId = await GetParticipantIdAsync();
        if (participantId is null) return Forbid();

        var items = await Db.Results.WithDetails()
            .Where(r => r.Enrollment.ParticipantID == participantId)
            .OrderByDescending(r => r.RecordedAt)
            .ToListAsync();
        return Ok(items.Select(r => r.ToResponse()));
    }
}
