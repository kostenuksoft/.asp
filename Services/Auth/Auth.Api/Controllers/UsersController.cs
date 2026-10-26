using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Auth.Api.Contracts;
using Auth.Api.Data;
using Auth.Api.Security;
using Auth.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Auth.Api.Controllers;

[ApiController]
[Route("users")]
[Authorize]
[Produces("application/json")]
public sealed class UsersController(
    UserManager<ApplicationUser> userManager,
    IRefreshTokenService refreshTokens,
    ILogger<UsersController> logger
)
    : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> GetAll(CancellationToken cancellationToken)
    {
        List<ApplicationUser> users = await userManager.Users
            .AsNoTracking()
            .OrderBy(user => user.UserName)
            .ToListAsync(cancellationToken);

        return Ok(users.Select(UserDto.From).ToList());
    }

    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetById(string id)
    {
        if (!IsOwnerOrAdmin(id))
        {
            return Forbid();
        }

        ApplicationUser? user = await userManager.FindByIdAsync(id);
        return user is null ? NotFound() : Ok(UserDto.From(user));
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        string id,
        UpdateUserRequest request
    )
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!IsOwnerOrAdmin(id))
        {
            return Forbid();
        }

        ApplicationUser? user = await userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        user.DisplayName = request.DisplayName;
        IdentityResult updated = await userManager.UpdateAsync(user);
        if (!updated.Succeeded)
        {
            return ValidationProblem(new ValidationProblemDetails(updated.ToValidationErrors()));
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(string id)
    {
        if (id == CurrentUserId)
        {
            return Problem(statusCode: StatusCodes.Status409Conflict, title: "An administrator cannot delete their own account");
        }

        ApplicationUser? user = await userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        IdentityResult deleted = await userManager.DeleteAsync(user);
        if (!deleted.Succeeded)
        {
            return ValidationProblem(new ValidationProblemDetails(deleted.ToValidationErrors()));
        }

        logger.LogInformation("User {UserId} deleted by {ActorId}", id, CurrentUserId);
        return NoContent();
    }

    [HttpGet("{id}/refresh-tokens")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<RefreshTokenDto>>> GetRefreshTokens(
        string id,
        CancellationToken cancellationToken
    )
    {
        if (!IsOwnerOrAdmin(id))
        {
            return Forbid();
        }

        IReadOnlyList<RefreshToken> history = await refreshTokens.GetHistoryAsync(id, cancellationToken);
        return Ok(history
            .Select(token => new RefreshTokenDto(token.CreatedAt, token.ExpiresAt, token.RevokedAt, token.RevokedReason, token.IsActive))
            .ToList());
    }

    [HttpGet("{id}/roles")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IList<string>>> GetRoles(string id)
    {
        ApplicationUser? user = await userManager.FindByIdAsync(id);
        return user is null ? NotFound() : Ok(await userManager.GetRolesAsync(user));
    }

    [HttpPost("{id}/roles")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignRole(
        string id,
        AssignRoleRequest request
    )
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!Roles.All.Contains(request.Role))
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: $"Unknown role '{request.Role}'");
        }

        ApplicationUser? user = await userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        if (!await userManager.IsInRoleAsync(user, request.Role))
        {
            await userManager.AddToRoleAsync(user, request.Role);
            logger.LogInformation("Role {Role} assigned to {UserId} by {ActorId}", request.Role, id, CurrentUserId);
        }

        return NoContent();
    }

    [HttpDelete("{id}/roles/{role}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveRole(
        string id,
        string role
    )
    {
        ApplicationUser? user = await userManager.FindByIdAsync(id);
        if (user is null || !await userManager.IsInRoleAsync(user, role))
        {
            return NotFound();
        }

        await userManager.RemoveFromRoleAsync(user, role);
        logger.LogInformation("Role {Role} removed from {UserId} by {ActorId}", role, id, CurrentUserId);
        return NoContent();
    }

    private string CurrentUserId => User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? string.Empty;

    private bool IsOwnerOrAdmin(string id) => id == CurrentUserId || User.IsInRole(Roles.Admin);
}
