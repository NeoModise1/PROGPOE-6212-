using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;

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
}
