using Microsoft.AspNetCore.Mvc;
using Registry.Api.Authorization;
using Registry.Bll.Dtos;
using Registry.Bll.Services.Interfaces;

namespace Registry.Api.Controllers;

[ApiController]
[Route("api/tags")]
[Produces("application/json")]
public sealed class TagsController(ITagService tags) : ControllerBase
{
    [HttpGet]
    [RequirePermission(Permissions.Read)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TagDto>>> GetAll(
        CancellationToken cancellationToken
    ) => Ok(await tags.GetAllAsync(cancellationToken));

    [HttpGet("{id:long}")]
    [RequirePermission(Permissions.Read)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TagWithPackagesDto>> GetWithPackages(
        long id,
        CancellationToken cancellationToken
    ) => Ok(await tags.GetWithPackagesAsync(id, cancellationToken));

    [HttpGet("{name}/recent-packages")]
    [RequirePermission(Permissions.Read)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<PackageDto>>> GetRecentlyTaggedPackages(
        string name,
        [FromQuery] DateTime since,
        CancellationToken cancellationToken
    ) => Ok(await tags.GetRecentlyTaggedPackagesAsync(name, since, cancellationToken));
}
