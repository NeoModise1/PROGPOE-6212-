using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Auth;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

/// <summary>Age / distance categories for an event. Both roles can view; only the event's Organiser can manage.</summary>
[Route("api/events/{eventId:int}/categories")]
[SessionAuthorize]
public class CategoriesController : RaceDayControllerBase
{
    private readonly RaceDayDbContext _db;

    public CategoriesController(RaceDayDbContext db) => _db = db;

    /// <summary>List the categories of an event (Organiser and Participant).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CategoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAll(int eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.Id == eventId)) return NotFound();

        var categories = await _db.Categories.AsNoTracking()
            .Where(c => c.EventId == eventId).OrderBy(c => c.Name).ToListAsync();
        return Ok(categories.Select(c => c.ToResponse()));
    }

    /// <summary>Add a category to one of my events (Organiser only).</summary>
    [HttpPost]
    [SessionAuthorize(Roles.Organiser)]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(int eventId, [FromBody] CategoryRequest request)
    {
        var ev = await _db.Events.FindAsync(eventId);
        if (ev is null) return NotFound();
        if (ev.OrganiserId != CurrentUserId) return Forbidden("You can only manage categories for events you organise.");

        if (request.MinAge > request.MaxAge)
            return BadRequestProblem("MinAge cannot be greater than MaxAge.");

        var name = request.Name.Trim();
        if (await _db.Categories.AnyAsync(c => c.EventId == eventId && c.Name == name))
            return ConflictProblem("This event already has a category with that name.");

        var category = new Category
        {
            EventId = eventId,
            Name = name,
            MinAge = request.MinAge,
            MaxAge = request.MaxAge,
            DistanceKm = request.DistanceKm
        };
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, category.ToResponse());
    }

    /// <summary>Update a category (Organiser only).</summary>
    [HttpPut("{id:int}")]
    [SessionAuthorize(Roles.Organiser)]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(int eventId, int id, [FromBody] CategoryRequest request)
    {
        var ev = await _db.Events.FindAsync(eventId);
        if (ev is null) return NotFound();
        if (ev.OrganiserId != CurrentUserId) return Forbidden("You can only manage categories for events you organise.");

        var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id && c.EventId == eventId);
        if (category is null) return NotFound();

        if (request.MinAge > request.MaxAge)
            return BadRequestProblem("MinAge cannot be greater than MaxAge.");

        var name = request.Name.Trim();
        if (await _db.Categories.AnyAsync(c => c.EventId == eventId && c.Name == name && c.Id != id))
            return ConflictProblem("This event already has a category with that name.");

        category.Name = name;
        category.MinAge = request.MinAge;
        category.MaxAge = request.MaxAge;
        category.DistanceKm = request.DistanceKm;
        await _db.SaveChangesAsync();

        return Ok(category.ToResponse());
    }

    /// <summary>Delete a category that nobody has enrolled in (Organiser only).</summary>
    [HttpDelete("{id:int}")]
    [SessionAuthorize(Roles.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int eventId, int id)
    {
        var ev = await _db.Events.FindAsync(eventId);
        if (ev is null) return NotFound();
        if (ev.OrganiserId != CurrentUserId) return Forbidden("You can only manage categories for events you organise.");

        var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id && c.EventId == eventId);
        if (category is null) return NotFound();

        if (await _db.Enrolments.AnyAsync(e => e.CategoryId == id))
            return ConflictProblem("Participants are already enrolled in this category, so it cannot be deleted.");

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
