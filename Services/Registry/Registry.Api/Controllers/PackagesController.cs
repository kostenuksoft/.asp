using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Registry.Bll.Dtos;
using Registry.Bll.Services.Interfaces;

namespace Registry.Api.Controllers;

[ApiController]
[Route("api/packages")]
[Produces("application/json")]
public sealed class PackagesController(
    IPackageService packages,
    IValidator<PackageQuery> queryValidator,
    IValidator<CreatePackageDto> createValidator,
    IValidator<ChangeLicensePriceDto> licensePriceValidator
)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<PackageDto>>> GetList(
        [FromQuery] PackageQuery query,
        CancellationToken cancellationToken
    )
    {
        ValidationResult result = await queryValidator.ValidateAsync(query, cancellationToken);
        if (!result.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(result.ToDictionary()));
        }

        return Ok(await packages.GetListAsync(query, cancellationToken));
    }

    [HttpGet("top")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PackageDto>>> GetTopDownloaded(
        [FromQuery] int take = 5,
        CancellationToken cancellationToken = default
    ) => Ok(await packages.GetTopDownloadedAsync(take, cancellationToken));

    [HttpGet("{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PackageDetailsDto>> GetById(
        long id,
        CancellationToken cancellationToken
    ) => Ok(await packages.GetByIdAsync(id, cancellationToken));

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PackageDto>> Create(
        CreatePackageDto input,
        CancellationToken cancellationToken
    )
    {
        ValidationResult result = await createValidator.ValidateAsync(input, cancellationToken);
        if (!result.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(result.ToDictionary()));
        }

        PackageDto created = await packages.CreateAsync(input, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.PackageId }, created);
    }

    [HttpPut("{id:long}/license-price")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangeLicensePrice(
        long id,
        ChangeLicensePriceDto input,
        CancellationToken cancellationToken
    )
    {
        ValidationResult result = await licensePriceValidator.ValidateAsync(input, cancellationToken);
        if (!result.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(result.ToDictionary()));
        }

        await packages.ChangeLicensePriceAsync(id, input, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:long}/downloads")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegisterDownloads(
        long id,
        RegisterDownloadsDto input,
        CancellationToken cancellationToken
    )
    {
        await packages.RegisterDownloadsAsync(id, input, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:long}/unlist")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Unlist(
        long id,
        CancellationToken cancellationToken
    )
    {
        await packages.UnlistAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:long}/relist")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Relist(
        long id,
        CancellationToken cancellationToken
    )
    {
        await packages.RelistAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:long}/tags")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AssignTag(
        long id,
        AssignTagDto input,
        CancellationToken cancellationToken
    )
    {
        await packages.AssignTagAsync(id, input, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        long id,
        CancellationToken cancellationToken
    )
    {
        await packages.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
