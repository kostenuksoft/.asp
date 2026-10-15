using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Telemetry.WebApi.Controllers;

[ApiController]
[Produces("application/json")]
public abstract class ApiController(ISender sender) : ControllerBase
{
    protected ISender Sender { get; } = sender;
}
