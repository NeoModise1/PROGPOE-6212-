using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;

namespace RaceDay.Api.Controllers;

[ApiController]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    protected ApiControllerBase(RaceDayDbContext db)
    {
        Db = db;
    }

    protected RaceDayDbContext Db { get; }

    /// <summary>The UserID stored in the caller's JWT (0 when unauthenticated).</summary>
    protected int CurrentUserId =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    protected async Task<int?> GetOrganiserIdAsync()
    {
        var userId = CurrentUserId;
        return await Db.Organisers
            .Where(o => o.UserID == userId)
            .Select(o => (int?)o.OrganiserID)
            .FirstOrDefaultAsync();
    }

    protected async Task<int?> GetParticipantIdAsync()
    {
        var userId = CurrentUserId;
        return await Db.Participants
            .Where(p => p.UserID == userId)
            .Select(p => (int?)p.ParticipantID)
            .FirstOrDefaultAsync();
    }

    /// <summary>True when the logged-in organiser owns the event with the given OrganiserID.</summary>
    protected async Task<bool> IsEventOwnerAsync(int eventOrganiserId)
    {
        var organiserId = await GetOrganiserIdAsync();
        return organiserId.HasValue && organiserId.Value == eventOrganiserId;
    }

    protected ObjectResult ConflictProblem(string detail) =>
        Problem(detail: detail, statusCode: StatusCodes.Status409Conflict, title: "Conflict");

    protected ActionResult ValidationError(string field, string message)
    {
        ModelState.AddModelError(field, message);
        return ValidationProblem(ModelState);
    }
}
