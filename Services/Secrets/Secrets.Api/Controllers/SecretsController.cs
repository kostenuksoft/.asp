using Microsoft.AspNetCore.Mvc;
using Secrets.Api.Authorization;
using Secrets.Api.Contracts;
using Secrets.Api.Store;

namespace Secrets.Api.Controllers;

[ApiController]
[Route("api/secrets")]
[Produces("application/json")]
public sealed class SecretsController(
    PipelineSecretStore store,
    ILogger<SecretsController> logger
)
    : ControllerBase
{
    [HttpGet]
    [RequirePermission(Permissions.Read)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<PipelineSecretDto>> GetAll() => Ok(store.GetAll().Select(PipelineSecretDto.From).ToList());

    [HttpPost("rotate")]
    [RequirePermission(Permissions.Rotate)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<PipelineSecretDto> Rotate(RotateSecretRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        PipelineSecret? secret = store.Rotate(request.Name, request.NewValue);
        if (secret is null)
        {
            return NotFound();
        }

        logger.LogInformation("Secret {SecretName} rotated to version {Version} by {User}", secret.Name, secret.Version, User.Identity?.Name);
        return Ok(PipelineSecretDto.From(secret));
    }

    [HttpPost("revoke")]
    [RequirePermission(Permissions.Revoke)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<PipelineSecretDto> Revoke(RevokeSecretRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        PipelineSecret? secret = store.Revoke(request.Name);
        if (secret is null)
        {
            return NotFound();
        }

        logger.LogWarning("Secret {SecretName} revoked by {User}", secret.Name, User.Identity?.Name);
        return Ok(PipelineSecretDto.From(secret));
    }
}
