using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Auth;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;

namespace RaceDay.Api.Controllers;

/// <summary>View and update the logged-in user's own profile (both roles).</summary>
[Route("api/profile")]
[SessionAuthorize]
public class ProfileController : RaceDayControllerBase
{
    private readonly RaceDayDbContext _db;

    public ProfileController(RaceDayDbContext db) => _db = db;

    /// <summary>Get my profile.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Get()
    {
        var user = await _db.Users.FindAsync(CurrentUserId);
        return user is null ? NotFound() : Ok(user.ToProfile());
    }

    /// <summary>Update my profile (email and role cannot be changed).</summary>
    [HttpPut]
    [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Update([FromBody] UpdateProfileRequest request)
    {
        var user = await _db.Users.FindAsync(CurrentUserId);
        if (user is null) return NotFound();

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.PhoneNumber = request.PhoneNumber;
        user.DateOfBirth = request.DateOfBirth;
        await _db.SaveChangesAsync();

        return Ok(user.ToProfile());
    }
}
