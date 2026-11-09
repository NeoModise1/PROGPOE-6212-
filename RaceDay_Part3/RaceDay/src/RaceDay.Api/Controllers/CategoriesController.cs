using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

/// <summary>The master list of reusable category types (e.g. 5km Run, Under 20).</summary>
public class CategoriesController : ApiControllerBase
{
    public CategoriesController(RaceDayDbContext db) : base(db) { }

    private static string NormaliseActivity(string value)
    {
        var v = value.Trim().ToLowerInvariant();
        return char.ToUpperInvariant(v[0]) + v.Substring(1);
    }

    /// <summary>Returns the master list of category types.</summary>
    /// <response code="200">OK - returns array of categories.</response>
    [HttpGet("api/categories")]
    [ProducesResponseType(typeof(IEnumerable<CategoryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategories()
    {
        var items = await Db.Categories.OrderBy(c => c.ActivityType).ThenBy(c => c.DistanceKM).ThenBy(c => c.CategoryName).ToListAsync();
        return Ok(items.Select(c => c.ToResponse()));
    }

    /// <summary>Creates a new reusable category type (Organiser only).</summary>
    /// <response code="201">Created - returns new category with CategoryID.</response>
    /// <response code="400">Bad Request.</response>
    /// <response code="409">Conflict - category with this name and distance already exists.</response>
    [HttpPost("api/categories")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateCategory([FromBody] CategoryRequest request)
    {
        var name = request.CategoryName.Trim();
        var distance = request.DistanceKm!.Value;
        if (await Db.Categories.AnyAsync(c => c.CategoryName == name && c.DistanceKM == distance))
            return ConflictProblem("A category with this name and distance already exists.");

        var category = new Category
        {
            CategoryName = request.CategoryName.Trim(),
            DistanceKM = request.DistanceKm!.Value,
            ActivityType = NormaliseActivity(request.ActivityType),
            Description = request.Description?.Trim()
        };
        Db.Categories.Add(category);
        await Db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, category.ToResponse());
    }

    /// <summary>Updates a category type's details (Organiser only).</summary>
    /// <response code="200">OK - returns updated category.</response>
    /// <response code="404">Not Found.</response>
    [HttpPut("api/categories/{id:int}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCategory(int id, [FromBody] CategoryRequest request)
    {
        var category = await Db.Categories.FindAsync(id);
        if (category is null) return NotFound();

        var name = request.CategoryName.Trim();
        var distance = request.DistanceKm!.Value;
        if (await Db.Categories.AnyAsync(c => c.CategoryID != id && c.CategoryName == name && c.DistanceKM == distance))
            return ConflictProblem("A category with this name and distance already exists.");

        category.CategoryName = request.CategoryName.Trim();
        category.DistanceKM = request.DistanceKm!.Value;
        category.ActivityType = NormaliseActivity(request.ActivityType);
        category.Description = request.Description?.Trim();
        await Db.SaveChangesAsync();

        return Ok(category.ToResponse());
    }

    /// <summary>Deletes a category type (Organiser only). Fails if the category is used by any event.</summary>
    /// <response code="204">No Content.</response>
    /// <response code="404">Not Found.</response>
    /// <response code="409">Conflict - category is in use.</response>
    [HttpDelete("api/categories/{id:int}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var category = await Db.Categories.FindAsync(id);
        if (category is null) return NotFound();

        if (await Db.EventCategories.AnyAsync(ec => ec.CategoryID == id))
            return ConflictProblem("This category is in use by one or more events and cannot be deleted.");

        Db.Categories.Remove(category);
        await Db.SaveChangesAsync();
        return NoContent();
    }
}
