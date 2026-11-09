using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[Authorize]
public class UsersController : ApiControllerBase
{
    public UsersController(RaceDayDbContext db) : base(db) { }

    /// <summary>Returns the profile of the currently logged-in user.</summary>
    /// <response code="200">OK - returns user details (excluding password hash).</response>
    /// <response code="401">Unauthorized.</response>
    [HttpGet("api/users/me")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMe()
    {
        var user = await Db.Users.FindAsync(CurrentUserId);
        if (user is null) return Unauthorized();
        return Ok(user.ToResponse());
    }

    /// <summary>Updates the currently logged-in user's own profile details.</summary>
    /// <response code="200">OK - returns updated profile.</response>
    /// <response code="400">Bad Request - validation failed.</response>
    [HttpPut("api/users/me")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateProfileRequest request)
    {
        var user = await Db.Users.FindAsync(CurrentUserId);
        if (user is null) return Unauthorized();

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.PhoneNumber = request.PhoneNumber?.Trim();
        await Db.SaveChangesAsync();

        return Ok(user.ToResponse());
    }

    /// <summary>Uploads or replaces the logged-in user's profile picture. The image is stored in Azure Blob Storage.</summary>
    /// <remarks>Send multipart/form-data with a single "file" field (JPEG, PNG, GIF or WebP, max 5 MB).</remarks>
    /// <response code="200">OK - returns the public URL of the stored picture.</response>
    /// <response code="400">Bad Request - missing, empty, too large or unsupported file.</response>
    /// <response code="401">Unauthorized.</response>
    [HttpPost("api/users/me/picture")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ImageUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UploadPicture(IFormFile? file, [FromServices] IBlobStorageService storage)
    {
        var user = await Db.Users.FindAsync(CurrentUserId);
        if (user is null) return Unauthorized();

        var error = ImageValidator.Validate(file);
        if (error is not null) return ValidationError("file", error);

        if (!storage.IsConfigured)
            return Problem(detail: "Image storage is not configured.", statusCode: StatusCodes.Status503ServiceUnavailable, title: "Service Unavailable");

        var oldUrl = user.ProfilePictureUrl;
        await using var stream = file!.OpenReadStream();
        user.ProfilePictureUrl = await storage.UploadAsync("profiles", $"user-{user.UserID}", file.FileName, file.ContentType, stream);
        await Db.SaveChangesAsync();
        await storage.DeleteAsync(oldUrl);

        return Ok(new ImageUploadResponse(user.ProfilePictureUrl));
    }
}
