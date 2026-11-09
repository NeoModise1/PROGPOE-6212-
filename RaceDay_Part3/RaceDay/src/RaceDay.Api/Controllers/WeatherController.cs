using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

public class WeatherController : ApiControllerBase
{
    public WeatherController(RaceDayDbContext db) : base(db) { }

    /// <summary>Returns the recorded weather information for an event, latest first.</summary>
    /// <response code="200">OK - returns array of weather records.</response>
    /// <response code="404">Not Found - event does not exist.</response>
    [HttpGet("api/events/{eventId:int}/weather")]
    [ProducesResponseType(typeof(IEnumerable<WeatherResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        if (!await Db.Events.AnyAsync(e => e.EventID == eventId)) return NotFound();

        var items = await Db.WeatherInformation
            .Where(w => w.EventID == eventId)
            .OrderByDescending(w => w.RecordedAt).ThenByDescending(w => w.WeatherID)
            .ToListAsync();
        return Ok(items.Select(w => w.ToResponse()));
    }

    /// <summary>Records a new weather reading for an event (owning Organiser only).</summary>
    /// <response code="201">Created - returns new weather record.</response>
    /// <response code="403">Forbidden.</response>
    /// <response code="404">Not Found - event does not exist.</response>
    [HttpPost("api/events/{eventId:int}/weather")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(WeatherResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Record(int eventId, [FromBody] WeatherRequest request)
    {
        var ev = await Db.Events.FindAsync(eventId);
        if (ev is null) return NotFound();
        if (!await IsEventOwnerAsync(ev.OrganiserID)) return Forbid();

        var weather = new WeatherInformation
        {
            EventID = eventId,
            Temperature = request.Temperature!.Value,
            WeatherCondition = request.WeatherCondition.Trim(),
            WindSpeed = request.WindSpeed!.Value,
            Humidity = request.Humidity!.Value
        };
        Db.WeatherInformation.Add(weather);
        await Db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, weather.ToResponse());
    }
}
