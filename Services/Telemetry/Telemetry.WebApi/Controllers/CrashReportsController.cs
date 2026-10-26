using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Telemetry.Application.Common.Models;
using Telemetry.Application.CrashReports.Commands.CreateCrashReport;
using Telemetry.Application.CrashReports.Queries.GetCrashReportById;
using Telemetry.Application.CrashReports.Queries.GetCrashReportsList;
using Telemetry.Domain.Enums;

namespace Telemetry.WebApi.Controllers;

[Authorize]
[Route("api/crash-reports")]
public sealed class CrashReportsController(ISender sender) : ApiController(sender)
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<CrashReportSummaryDto>>> GetList(
        [FromQuery] string? packageKey,
        [FromQuery] CrashReportKind? kind,
        [FromQuery] CrashSeverity? severity,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default
    ) => Ok(await Sender.Send(
        new GetCrashReportsListQuery(packageKey, kind, severity, page, pageSize),
        cancellationToken
    ));

    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CrashReportDto>> GetById(
        string id,
        CancellationToken cancellationToken
    ) => Ok(await Sender.Send(new GetCrashReportByIdQuery(id), cancellationToken));

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CrashReportDto>> Create(
        CreateCrashReportCommand command,
        CancellationToken cancellationToken
    )
    {
        CrashReportDto created = await Sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }
}
