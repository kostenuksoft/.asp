using System.Diagnostics;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Registry.Api.Authorization;
using Registry.Api.Metrics;
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
    IValidator<ChangeLicensePriceDto> licensePriceValidator,
    IValidator<RenamePackageDto> renameValidator,
    RegistryMetrics metrics
)
    : ControllerBase
{
    private const string CreateOperation = "create";
    private const string ChangeLicensePriceOperation = "change_license_price";

    [HttpGet]
    [RequirePermission(Permissions.Read)]
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
    [RequirePermission(Permissions.Read)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PackageDto>>> GetTopDownloaded(
        [FromQuery] int take = 5,
        CancellationToken cancellationToken = default
    ) => Ok(await packages.GetTopDownloadedAsync(take, cancellationToken));

    [HttpGet("{id:long}")]
    [RequirePermission(Permissions.Read)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PackageDetailsDto>> GetById(
        long id,
        CancellationToken cancellationToken
    ) => Ok(await packages.GetByIdAsync(id, cancellationToken));

    [HttpPost]
    [RequirePermission(Permissions.Create)]
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

        Stopwatch stopwatch = Stopwatch.StartNew();
        try
        {
            PackageDto created = await packages.CreateAsync(input, cancellationToken);
            metrics.PackageCreated(created.LicensePrice);
            metrics.RecordDuration(CreateOperation, success: true, stopwatch.Elapsed.TotalMilliseconds);
            return CreatedAtAction(nameof(GetById), new { id = created.PackageId }, created);
        }
        catch (Exception) when (RecordFailure(CreateOperation, stopwatch))
        {
            throw;
        }
    }

    [HttpPut("{id:long}/name")]
    [RequirePermission(Permissions.Update)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Rename(
        long id,
        RenamePackageDto input,
        CancellationToken cancellationToken
    )
    {
        ValidationResult result = await renameValidator.ValidateAsync(input, cancellationToken);
        if (!result.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(result.ToDictionary()));
        }

        await packages.RenameAsync(id, input, cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:long}/license-price")]
    [RequirePermission(Permissions.Update)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangeLicensePrice(
        long id,
        ChangeLicensePriceDto input,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(input);

        ValidationResult result = await licensePriceValidator.ValidateAsync(input, cancellationToken);
        if (!result.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(result.ToDictionary()));
        }

        Stopwatch stopwatch = Stopwatch.StartNew();
        try
        {
            await packages.ChangeLicensePriceAsync(id, input, cancellationToken);
            metrics.LicensePriceChanged(input.LicensePrice);
            metrics.RecordDuration(ChangeLicensePriceOperation, success: true, stopwatch.Elapsed.TotalMilliseconds);
            return NoContent();
        }
        catch (Exception) when (RecordFailure(ChangeLicensePriceOperation, stopwatch))
        {
            throw;
        }
    }

    [HttpPost("{id:long}/downloads")]
    [RequirePermission(Permissions.Update)]
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
    [RequirePermission(Permissions.Publish)]
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
    [RequirePermission(Permissions.Publish)]
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
    [RequirePermission(Permissions.Update)]
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
    [RequirePermission(Permissions.Delete)]
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

    private bool RecordFailure(
        string operation,
        Stopwatch stopwatch
    )
    {
        metrics.RecordDuration(operation, success: false, stopwatch.Elapsed.TotalMilliseconds);
        return false;
    }
}
