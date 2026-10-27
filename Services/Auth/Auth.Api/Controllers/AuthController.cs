using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Auth.Api.Contracts;
using Auth.Api.Data;
using Auth.Api.Options;
using Auth.Api.Security;
using Auth.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Auth.Api.Controllers;

[ApiController]
[Route("auth")]
[Produces("application/json")]
public sealed class AuthController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IJwtTokenService jwtTokens,
    IRefreshTokenService refreshTokens,
    IAccountEmailSender emailSender,
    IOptions<JwtOptions> jwtOptions,
    ILogger<AuthController> logger
)
    : ControllerBase
{
    private const string BearerTokenType = "Bearer";

    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserDto>> Register(
        RegisterRequest request,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(request);

        ApplicationUser user = new()
        {
            UserName = request.UserName,
            Email = request.Email,
            DisplayName = request.DisplayName,
        };

        IdentityResult created = await userManager.CreateAsync(user, request.Password);
        if (!created.Succeeded)
        {
            return ValidationProblem(new ValidationProblemDetails(created.ToValidationErrors()));
        }

        await userManager.AddToRoleAsync(user, Roles.User);
        await SendEmailConfirmationAsync(user, cancellationToken);
        logger.LogInformation("Registered user {UserId} {UserName}", user.Id, user.UserName);

        return CreatedAtAction(nameof(UsersController.GetById), "Users", new { id = user.Id }, UserDto.From(user));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status423Locked)]
    public async Task<ActionResult<AuthResponse>> Login(
        LoginRequest request,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(request);

        ApplicationUser? user = await userManager.FindByNameAsync(request.UserName);
        if (user is null)
        {
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Invalid user name or password");
        }

        Microsoft.AspNetCore.Identity.SignInResult result = await signInManager.CheckPasswordSignInAsync(
            user,
            request.Password,
            lockoutOnFailure: true
        );
        if (result.IsLockedOut)
        {
            return Problem(
                statusCode: StatusCodes.Status423Locked,
                title: "Account is locked after repeated failed logins"
            );
        }

        if (result.IsNotAllowed)
        {
            return Problem(statusCode: StatusCodes.Status403Forbidden, title: "Email address is not confirmed");
        }

        if (!result.Succeeded)
        {
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Invalid user name or password");
        }

        string refreshToken = await refreshTokens.IssueAsync(user, cancellationToken);
        logger.LogInformation("User {UserId} logged in", user.Id);
        return Ok(await IssueSessionAsync(user, refreshToken));
    }

    [HttpPost("refresh-token")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> RefreshToken(CancellationToken cancellationToken)
    {
        string? token = RefreshTokenCookie.Read(Request);
        RefreshTokenRotation? rotation = token is null
            ? null
            : await refreshTokens.RotateAsync(token, cancellationToken);
        if (rotation is null || await userManager.IsLockedOutAsync(rotation.User))
        {
            RefreshTokenCookie.Delete(Response);
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Refresh token is invalid, expired or revoked"
            );
        }

        return Ok(await IssueSessionAsync(rotation.User, rotation.NewToken));
    }

    [HttpPost("revoke-token")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeToken(
        RevokeTokenRequest? request,
        CancellationToken cancellationToken
    )
    {
        string? cookieToken = RefreshTokenCookie.Read(Request);
        string? token = string.IsNullOrWhiteSpace(request?.Token) ? cookieToken : request.Token;
        if (token is null)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "No refresh token in the body or the cookie"
            );
        }

        RefreshToken? refreshToken = await refreshTokens.FindActiveAsync(token, cancellationToken);
        if (refreshToken is null)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "No active refresh token matches");
        }

        if (refreshToken.UserId != CurrentUserId && !User.IsInRole(Roles.Admin))
        {
            return Forbid();
        }

        await refreshTokens.RevokeAsync(refreshToken, "revoked", cancellationToken);
        if (token == cookieToken)
        {
            RefreshTokenCookie.Delete(Response);
        }

        logger.LogInformation(
            "Refresh token of user {UserId} revoked by {ActorId}",
            refreshToken.UserId,
            CurrentUserId
        );
        return NoContent();
    }

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        string? token = RefreshTokenCookie.Read(Request);
        RefreshToken? refreshToken = token is null
            ? null
            : await refreshTokens.FindActiveAsync(token, cancellationToken);
        if (refreshToken is not null && refreshToken.UserId == CurrentUserId)
        {
            await refreshTokens.RevokeAsync(refreshToken, "logout", cancellationToken);
        }

        RefreshTokenCookie.Delete(Response);
        logger.LogInformation("User {UserId} logged out", CurrentUserId);
        return NoContent();
    }

    [HttpPost("verify-email")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyEmail(VerifyEmailRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        ApplicationUser? user = await userManager.FindByIdAsync(request.UserId);
        string? token = IdentityTokenEncoding.TryDecode(request.Token);
        if (user is null || token is null)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid or expired confirmation token");
        }

        IdentityResult confirmed = await userManager.ConfirmEmailAsync(user, token);
        if (!confirmed.Succeeded)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid or expired confirmation token");
        }

        await userManager.UpdateSecurityStampAsync(user);
        logger.LogInformation("User {UserId} confirmed the email address", user.Id);
        return NoContent();
    }

    [HttpPost("resend-verification")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> ResendVerification(
        EmailRequest request,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(request);

        ApplicationUser? user = await userManager.FindByEmailAsync(request.Email);
        if (user is { EmailConfirmed: false })
        {
            await SendEmailConfirmationAsync(user, cancellationToken);
        }

        return Accepted();
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> ForgotPassword(
        EmailRequest request,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(request);

        ApplicationUser? user = await userManager.FindByEmailAsync(request.Email);
        if (user is { EmailConfirmed: true })
        {
            string token = await userManager.GeneratePasswordResetTokenAsync(user);
            await emailSender.SendPasswordResetAsync(user, IdentityTokenEncoding.Encode(token), cancellationToken);
        }

        return Accepted();
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword(
        ResetPasswordRequest request,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(request);

        ApplicationUser? user = await userManager.FindByEmailAsync(request.Email);
        string? token = IdentityTokenEncoding.TryDecode(request.Token);
        if (user is null || token is null)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid or expired reset token");
        }

        IdentityResult reset = await userManager.ResetPasswordAsync(user, token, request.NewPassword);
        if (!reset.Succeeded)
        {
            return ValidationProblem(new ValidationProblemDetails(reset.ToValidationErrors()));
        }

        int revoked = await refreshTokens.RevokeAllAsync(user.Id, "password reset", cancellationToken);
        logger.LogInformation("User {UserId} reset the password; {Count} refresh tokens revoked", user.Id, revoked);
        return NoContent();
    }

    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword(
        ChangePasswordRequest request,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(request);

        ApplicationUser? user = await userManager.FindByIdAsync(CurrentUserId);
        if (user is null)
        {
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "The account no longer exists");
        }

        IdentityResult changed = await userManager.ChangePasswordAsync(
            user,
            request.CurrentPassword,
            request.NewPassword
        );
        if (!changed.Succeeded)
        {
            return ValidationProblem(new ValidationProblemDetails(changed.ToValidationErrors()));
        }

        int revoked = await refreshTokens.RevokeAllAsync(user.Id, "password changed", cancellationToken);
        string refreshToken = await refreshTokens.IssueAsync(user, cancellationToken);
        RefreshTokenCookie.Write(Response, refreshToken, jwtOptions.Value.RefreshTokenDays);
        logger.LogInformation("User {UserId} changed the password; {Count} other sessions revoked", user.Id, revoked);
        return NoContent();
    }

    private string CurrentUserId => User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? string.Empty;

    private async Task<AuthResponse> IssueSessionAsync(
        ApplicationUser user,
        string refreshToken
    )
    {
        IList<string> roles = await userManager.GetRolesAsync(user);
        AccessToken accessToken = jwtTokens.GenerateAccessToken(user, roles);
        RefreshTokenCookie.Write(Response, refreshToken, jwtOptions.Value.RefreshTokenDays);
        return new AuthResponse(accessToken.Token, accessToken.ExpiresAt, BearerTokenType);
    }

    private async Task SendEmailConfirmationAsync(
        ApplicationUser user,
        CancellationToken cancellationToken
    )
    {
        string token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        await emailSender.SendEmailConfirmationAsync(user, IdentityTokenEncoding.Encode(token), cancellationToken);
    }
}
