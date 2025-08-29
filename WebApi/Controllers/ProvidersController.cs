using Infrastructure.Providers;
using MessagingService.Models.Requests;
using Microsoft.AspNetCore.Mvc;

namespace MessagingService.Controllers;

[ApiController]
[Route("providers")]
public sealed class ProvidersController(IProviderRegistry registry) : ControllerBase
{
    [HttpGet]
    public IActionResult List() => Ok(registry.GetAll());

    [HttpPatch("{name}")]
    public IActionResult Update(string name, [FromBody] UpdateProviderDto body)
    {
        var ok = registry.TryUpdate(name, body.Enabled, body.Priority);
        return ok ? NoContent() : NotFound();
    }
}