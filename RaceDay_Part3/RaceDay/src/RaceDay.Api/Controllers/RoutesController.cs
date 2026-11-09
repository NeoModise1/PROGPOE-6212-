using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

public class RoutesController : ApiControllerBase
{
    public RoutesController(RaceDayDbContext db) : base(db) { }

    /// <summary>Returns all routes for an event.</summary>
    /// <response code="200">OK - returns array of routes.</response>
    /// <response code="404">Not Found - event does not exist.</response>
    [HttpGet("api/events/{eventId:int}/routes")]
    [ProducesResponseType(typeof(IEnumerable<RouteResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        if (!await Db.Events.AnyAsync(e => e.EventID == eventId)) return NotFound();

        var items = await Db.Routes.Where(r => r.EventID == eventId).OrderBy(r => r.RouteID).ToListAsync();
        return Ok(items.Select(r => r.ToResponse()));
    }

    /// <summary>Adds a new route to an event (owning Organiser only).</summary>
    /// <response code="201">Created - returns new route with RouteID.</response>
    /// <response code="403">Forbidden.</response>
    /// <response code="404">Not Found - event does not exist.</response>
    [HttpPost("api/events/{eventId:int}/routes")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(RouteResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Add(int eventId, [FromBody] RouteRequest request)
    {
        var ev = await Db.Events.FindAsync(eventId);
        if (ev is null) return NotFound();
        if (!await IsEventOwnerAsync(ev.OrganiserID)) return Forbid();

        var route = new RaceRoute
        {
            EventID = eventId,
            RouteName = request.RouteName.Trim(),
            DistanceKM = request.DistanceKm!.Value,
            ElevationGain = request.ElevationGain!.Value,
            Description = request.Description?.Trim(),
            MapURL = request.MapUrl?.Trim()
        };
        Db.Routes.Add(route);
        await Db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, route.ToResponse());
    }

    /// <summary>Updates an existing route's details (owning Organiser only).</summary>
    /// <response code="200">OK - returns updated route.</response>
    /// <response code="403">Forbidden.</response>
    /// <response code="404">Not Found.</response>
    [HttpPut("api/routes/{id:int}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(RouteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] RouteRequest request)
    {
        var route = await Db.Routes.Include(r => r.Event).FirstOrDefaultAsync(r => r.RouteID == id);
        if (route is null) return NotFound();
        if (!await IsEventOwnerAsync(route.Event.OrganiserID)) return Forbid();

        route.RouteName = request.RouteName.Trim();
        route.DistanceKM = request.DistanceKm!.Value;
        route.ElevationGain = request.ElevationGain!.Value;
        route.Description = request.Description?.Trim();
        route.MapURL = request.MapUrl?.Trim();
        await Db.SaveChangesAsync();

        return Ok(route.ToResponse());
    }

    /// <summary>Deletes a route from an event (owning Organiser only).</summary>
    /// <response code="204">No Content.</response>
    /// <response code="403">Forbidden.</response>
    /// <response code="404">Not Found.</response>
    [HttpDelete("api/routes/{id:int}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var route = await Db.Routes.Include(r => r.Event).FirstOrDefaultAsync(r => r.RouteID == id);
        if (route is null) return NotFound();
        if (!await IsEventOwnerAsync(route.Event.OrganiserID)) return Forbid();

        Db.Routes.Remove(route);
        await Db.SaveChangesAsync();
        return NoContent();
    }
}
