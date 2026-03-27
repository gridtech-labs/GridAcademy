using GridAcademy.Common;
using GridAcademy.DTOs.Users;
using GridAcademy.Jobs;
using GridAcademy.Services;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GridAcademy.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(AuthenticationSchemes = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly IUserService _users;
    private readonly IBackgroundJobClient _jobs;

    public UsersController(IUserService users, IBackgroundJobClient jobs)
    {
        _users = users;
        _jobs  = jobs;
    }

    // ── GET /api/users ───────────────────────────────────────────────────────
    /// <summary>List users with optional search, role filter, and pagination.</summary>
    [HttpGet]
    [Authorize(AuthenticationSchemes = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<UserDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] UserListRequest request)
    {
        var result = await _users.GetUsersAsync(request);
        return Ok(ApiResponse<PagedResult<UserDto>>.Ok(result));
    }

    // ── GET /api/users/{id} ─────────────────────────────────────────────────
    /// <summary>Get a specific user by ID.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(AuthenticationSchemes = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var user = await _users.GetByIdAsync(id);
        return Ok(ApiResponse<UserDto>.Ok(user));
    }

    // ── POST /api/users ─────────────────────────────────────────────────────
    /// <summary>Create a new user. Admin only.</summary>
    [HttpPost]
    [Authorize(AuthenticationSchemes = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        var user = await _users.CreateAsync(request);

        // Enqueue a welcome email in the background (fire-and-forget, retried by Hangfire)
        _jobs.Enqueue<EmailJob>(job => job.SendWelcomeEmailAsync(user.Email, user.FullName));

        return CreatedAtAction(nameof(GetById), new { id = user.Id },
            ApiResponse<UserDto>.Ok(user, "User created successfully."));
    }

    // ── PUT /api/users/{id} ─────────────────────────────────────────────────
    /// <summary>Update an existing user. Admin only.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(AuthenticationSchemes = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserRequest request)
    {
        var user = await _users.UpdateAsync(id, request);
        return Ok(ApiResponse<UserDto>.Ok(user, "User updated successfully."));
    }

    // ── DELETE /api/users/{id} ──────────────────────────────────────────────
    /// <summary>Permanently delete a user. Admin only.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(AuthenticationSchemes = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _users.DeleteAsync(id);
        return Ok(ApiResponse.Ok("User deleted successfully."));
    }

    // ── GET /api/users/me ───────────────────────────────────────────────────
    /// <summary>Get the currently authenticated user's profile.</summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Me()
    {
        // Extract user ID from JWT sub claim
        var sub = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
               ?? User.FindFirst("sub")?.Value;

        if (!Guid.TryParse(sub, out var userId))
            return Unauthorized(ApiResponse.Fail("Invalid token."));

        var user = await _users.GetByIdAsync(userId);
        return Ok(ApiResponse<UserDto>.Ok(user));
    }
}
