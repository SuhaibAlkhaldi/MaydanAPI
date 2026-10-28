using Maydan.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maydan.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class LookupsController : ApiControllerBase
{
    private readonly ILookupService _lookupService;

    public LookupsController(ILookupService lookupService)
    {
        _lookupService = lookupService;
    }

    [HttpGet("{categoryCode}")]
    public async Task<ActionResult> GetByCategory(string categoryCode, CancellationToken cancellationToken)
    {
        var response = await _lookupService.GetByCategoryAsync(categoryCode, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }
}
