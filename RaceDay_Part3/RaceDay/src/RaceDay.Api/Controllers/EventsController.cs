using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

public class EventsController : ApiControllerBase
{
    private static readonly string[] AllowedStatuses = { "Scheduled", "Ongoing", "Completed", "Cancelled" };
    private static readonly string[] AllowedEventTypes = { "Run", "Walk", "Cycle", "Multisport" };

    private static string? NormaliseEventType(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "Run";
        return AllowedEventTypes.FirstOrDefault(t => t.Equals(value.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public EventsController(RaceDayDbContext db) : base(db) { }

    /// <summary>Returns a list of all events.</summary>
    /// <response code="200">OK - returns array of events.</response>
    [HttpGet("api/events")]
    [ProducesResponseType(typeof(IEnumerable<EventResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEvents()
    {
        var events = await Db.Events.OrderBy(e => e.EventDate).ThenBy(e => e.EventID).ToListAsync();
        return Ok(events.Select(e => e.ToResponse()));
    }

    /// <summary>Returns the events created by the logged-in Organiser.</summary>
    /// <response code="200">OK - returns array of the organiser's own events.</response>
    /// <response code="401">Unauthorized.</response>
    /// <response code="403">Forbidden - caller is not an Organiser.</response>
    [HttpGet("api/events/mine")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(IEnumerable<EventResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMine()
    {
        var organiserId = await GetOrganiserIdAsync();
        if (organiserId is null) return Forbid();

        var events = await Db.Events.Where(e => e.OrganiserID == organiserId)
            .OrderBy(e => e.EventDate).ThenBy(e => e.EventID).ToListAsync();
        return Ok(events.Select(e => e.ToResponse()));
    }

    /// <summary>Returns full details for a single event.</summary>
    /// <response code="200">OK - returns event details.</response>
    /// <response code="404">Not Found.</response>
    [HttpGet("api/events/{id:int}")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEvent(int id)
    {
        var ev = await Db.Events.FindAsync(id);
        return ev is null ? NotFound() : Ok(ev.ToResponse());
    }

    /// <summary>Creates a new event (Organiser only). The event is owned by the logged-in organiser.</summary>
    /// <response code="201">Created - returns new event with EventID.</response>
    /// <response code="400">Bad Request.</response>
    /// <response code="401">Unauthorized - no/invalid token.</response>
    /// <response code="403">Forbidden - caller is not an Organiser.</response>
    [HttpPost("api/events")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateEvent([FromBody] EventRequest request)
    {
        var organiserId = await GetOrganiserIdAsync();
        if (organiserId is null) return Forbid();

        var eventType = NormaliseEventType(request.EventType);
        if (eventType is null)
            return ValidationError(nameof(request.EventType), $"EventType must be one of: {string.Join(", ", AllowedEventTypes)}.");

        var ev = new RaceEvent
        {
            OrganiserID = organiserId.Value,
            EventName = request.EventName.Trim(),
            Description = request.Description?.Trim(),
            EventDate = request.EventDate!.Value.Date,
            StartTime = request.StartTime!.Value,
            Venue = request.Venue.Trim(),
            City = request.City.Trim(),
            Province = request.Province.Trim(),
            EventType = eventType,
            DistanceKM = request.DistanceKm,
            Status = "Scheduled"
        };

        Db.Events.Add(ev);
        await Db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetEvent), new { id = ev.EventID }, ev.ToResponse());
    }

    /// <summary>Updates an existing event's details (owning Organiser only).</summary>
    /// <response code="200">OK - returns updated event.</response>
    /// <response code="400">Bad Request.</response>
    /// <response code="403">Forbidden - not the owner / not an Organiser.</response>
    /// <response code="404">Not Found.</response>
    [HttpPut("api/events/{id:int}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEvent(int id, [FromBody] EventUpdateRequest request)
    {
        var ev = await Db.Events.FindAsync(id);
        if (ev is null) return NotFound();
        if (!await IsEventOwnerAsync(ev.OrganiserID)) return Forbid();

        var status = AllowedStatuses.FirstOrDefault(s => s.Equals(request.Status.Trim(), StringComparison.OrdinalIgnoreCase));
        if (status is null)
            return ValidationError(nameof(request.Status), $"Status must be one of: {string.Join(", ", AllowedStatuses)}.");

        var eventType = NormaliseEventType(request.EventType);
        if (eventType is null)
            return ValidationError(nameof(request.EventType), $"EventType must be one of: {string.Join(", ", AllowedEventTypes)}.");

        ev.EventName = request.EventName.Trim();
        ev.Description = request.Description?.Trim();
        ev.EventDate = request.EventDate!.Value.Date;
        ev.StartTime = request.StartTime!.Value;
        ev.Venue = request.Venue.Trim();
        ev.City = request.City.Trim();
        ev.Province = request.Province.Trim();
        ev.Status = status;
        ev.EventType = eventType;
        ev.DistanceKM = request.DistanceKm;
        await Db.SaveChangesAsync();

        return Ok(ev.ToResponse());
    }

    /// <summary>Deletes an event and its associated categories, routes and weather records (owning Organiser only).</summary>
    /// <response code="204">No Content.</response>
    /// <response code="403">Forbidden.</response>
    /// <response code="404">Not Found.</response>
    [HttpDelete("api/events/{id:int}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var ev = await Db.Events.FindAsync(id);
        if (ev is null) return NotFound();
        if (!await IsEventOwnerAsync(ev.OrganiserID)) return Forbid();

        Db.Events.Remove(ev);
        await Db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Uploads or replaces the banner image of an event (owning Organiser only). The image is stored in Azure Blob Storage.</summary>
    /// <remarks>Send multipart/form-data with a single "file" field (JPEG, PNG, GIF or WebP, max 5 MB).</remarks>
    /// <response code="200">OK - returns the public URL of the stored banner.</response>
    /// <response code="400">Bad Request - missing, empty, too large or unsupported file.</response>
    /// <response code="403">Forbidden.</response>
    /// <response code="404">Not Found.</response>
    /// <response code="503">Service Unavailable - Blob Storage is not configured.</response>
    [HttpPost("api/events/{id:int}/banner")]
    [Authorize(Roles = "Organiser")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ImageUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadBanner(int id, IFormFile? file, [FromServices] IBlobStorageService storage)
    {
        var ev = await Db.Events.FindAsync(id);
        if (ev is null) return NotFound();
        if (!await IsEventOwnerAsync(ev.OrganiserID)) return Forbid();

        var error = ImageValidator.Validate(file);
        if (error is not null) return ValidationError("file", error);

        if (!storage.IsConfigured)
            return Problem(detail: "Image storage is not configured.", statusCode: StatusCodes.Status503ServiceUnavailable, title: "Service Unavailable");

        var oldUrl = ev.BannerUrl;
        await using var stream = file!.OpenReadStream();
        ev.BannerUrl = await storage.UploadAsync("banners", $"event-{id}", file.FileName, file.ContentType, stream);
        await Db.SaveChangesAsync();
        await storage.DeleteAsync(oldUrl);

        return Ok(new ImageUploadResponse(ev.BannerUrl));
    }
}
