using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

/// <summary>Category offerings (with fee and capacity) attached to a specific event.</summary>
public class EventCategoriesController : ApiControllerBase
{
    public EventCategoriesController(RaceDayDbContext db) : base(db) { }

    private Task<EventCategoryResponse> LoadResponseAsync(int eventCategoryId) =>
        Db.EventCategories.Where(x => x.EventCategoryID == eventCategoryId).ToResponses().SingleAsync();

    /// <summary>Returns all category offerings for an event, with fee and capacity.</summary>
    /// <response code="200">OK - returns array of event categories.</response>
    /// <response code="404">Not Found - event does not exist.</response>
    [HttpGet("api/events/{eventId:int}/categories")]
    [ProducesResponseType(typeof(IEnumerable<EventCategoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        if (!await Db.Events.AnyAsync(e => e.EventID == eventId)) return NotFound();

        var items = await Db.EventCategories
            .Where(ec => ec.EventID == eventId)
            .OrderBy(ec => ec.EventCategoryID)
            .ToResponses()
            .ToListAsync();
        return Ok(items);
    }

    /// <summary>Attaches a master category to this event with a fee and capacity (owning Organiser only).</summary>
    /// <response code="201">Created - returns new EventCategory.</response>
    /// <response code="403">Forbidden.</response>
    /// <response code="404">Not Found - event does not exist.</response>
    /// <response code="409">Conflict - category already attached to this event.</response>
    [HttpPost("api/events/{eventId:int}/categories")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(EventCategoryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Attach(int eventId, [FromBody] EventCategoryCreateRequest request)
    {
        var ev = await Db.Events.FindAsync(eventId);
        if (ev is null) return NotFound();
        if (!await IsEventOwnerAsync(ev.OrganiserID)) return Forbid();

        var categoryId = request.CategoryId!.Value;
        if (!await Db.Categories.AnyAsync(c => c.CategoryID == categoryId))
            return ValidationError(nameof(request.CategoryId), "The selected category does not exist.");

        if (await Db.EventCategories.AnyAsync(ec => ec.EventID == eventId && ec.CategoryID == categoryId))
            return ConflictProblem("This category is already attached to the event.");

        var ec = new EventCategory
        {
            EventID = eventId,
            CategoryID = categoryId,
            EntryFee = request.EntryFee!.Value,
            MaximumParticipants = request.MaximumParticipants!.Value
        };
        Db.EventCategories.Add(ec);
        await Db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, await LoadResponseAsync(ec.EventCategoryID));
    }

    /// <summary>Updates the fee or capacity for an event's category offering (owning Organiser only).</summary>
    /// <response code="200">OK - returns updated event category.</response>
    /// <response code="403">Forbidden.</response>
    /// <response code="404">Not Found.</response>
    [HttpPut("api/event-categories/{id:int}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(EventCategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] EventCategoryUpdateRequest request)
    {
        var ec = await Db.EventCategories.Include(x => x.Event).FirstOrDefaultAsync(x => x.EventCategoryID == id);
        if (ec is null) return NotFound();
        if (!await IsEventOwnerAsync(ec.Event.OrganiserID)) return Forbid();

        ec.EntryFee = request.EntryFee!.Value;
        ec.MaximumParticipants = request.MaximumParticipants!.Value;
        await Db.SaveChangesAsync();

        return Ok(await LoadResponseAsync(id));
    }

    /// <summary>Removes a category offering from an event (owning Organiser only).</summary>
    /// <response code="204">No Content.</response>
    /// <response code="403">Forbidden.</response>
    /// <response code="404">Not Found.</response>
    /// <response code="409">Conflict - enrolments exist.</response>
    [HttpDelete("api/event-categories/{id:int}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        var ec = await Db.EventCategories.Include(x => x.Event).FirstOrDefaultAsync(x => x.EventCategoryID == id);
        if (ec is null) return NotFound();
        if (!await IsEventOwnerAsync(ec.Event.OrganiserID)) return Forbid();

        if (await Db.Enrollments.AnyAsync(e => e.EventCategoryID == id))
            return ConflictProblem("Enrolments exist for this category, so it cannot be removed.");

        Db.EventCategories.Remove(ec);
        await Db.SaveChangesAsync();
        return NoContent();
    }
}
