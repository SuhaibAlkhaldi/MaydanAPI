using Maydan.Application.DTOs.Associations;
using Maydan.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maydan.API.Controllers;

// Association Management, Phase 2a (MAYD-4, MAYD-40..54) — see AssociationService's own header
// comment for the full scope story (what already existed, what this phase adds, what's
// deliberately deferred to later phases).
//
// Route/verb shape matches workforcment's associations.service.ts exactly, not a convention
// invented here: GET (list) + GET/{id}, POST, PUT (id in the body — same unusual-but-real
// convention ProjectsController.Update already uses), DELETE (?id=), the two order-by-worker-count
// endpoints, by-name search, the deleted-only list + its own by-name search, and PATCH restore
// (?id=). All gated behind [Authorize] — there is no anonymous/public consumer of this module
// (unlike Phase 1's Locations, which needed AllowAnonymous for the public signup flow) — with the
// real, existing Associations permission catalog (ViewAssociations/CreateAssociations/
// EditAssociations/DeleteAssociations/ManageAssociations) enforced inside AssociationService itself.
[Authorize]
[Route("api/[controller]")]
public class AssociationsController : ApiControllerBase
{
    private readonly IAssociationService _associationService;

    public AssociationsController(IAssociationService associationService)
    {
        _associationService = associationService;
    }

    [HttpGet]
    public async Task<ActionResult<List<AssociationDto>>> GetAll(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(new { message = "Current user id is required." });
        }

        try
        {
            return Ok(await _associationService.GetAllAsync(currentUserId, cancellationToken));
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    [HttpGet("order-by-worker-asc")]
    public async Task<ActionResult<List<AssociationDto>>> GetOrderedByWorkersCountAsc(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(new { message = "Current user id is required." });
        }

        try
        {
            return Ok(await _associationService.GetOrderedByWorkersCountAsync(currentUserId, ascending: true, cancellationToken));
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    [HttpGet("order-by-worker-desc")]
    public async Task<ActionResult<List<AssociationDto>>> GetOrderedByWorkersCountDesc(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(new { message = "Current user id is required." });
        }

        try
        {
            return Ok(await _associationService.GetOrderedByWorkersCountAsync(currentUserId, ascending: false, cancellationToken));
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    [HttpGet("by-name")]
    public async Task<ActionResult<List<AssociationDto>>> SearchByName([FromQuery] string name, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(new { message = "Current user id is required." });
        }

        try
        {
            return Ok(await _associationService.SearchByNameAsync(currentUserId, name, cancellationToken));
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    // Must be mapped before "{id:int}" would ever ambiguously apply — not actually a conflict here
    // since {id:int} has a numeric constraint and "deleted" isn't numeric, but kept as its own
    // explicit route (not nested under {id}) to match the frontend's flat /deleted and
    // /deleted/by-name paths exactly.
    [HttpGet("deleted")]
    public async Task<ActionResult<List<AssociationDto>>> GetDeleted(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(new { message = "Current user id is required." });
        }

        try
        {
            return Ok(await _associationService.GetDeletedAsync(currentUserId, cancellationToken));
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    [HttpGet("deleted/by-name")]
    public async Task<ActionResult<List<AssociationDto>>> SearchDeletedByName([FromQuery] string name, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(new { message = "Current user id is required." });
        }

        try
        {
            return Ok(await _associationService.SearchDeletedByNameAsync(currentUserId, name, cancellationToken));
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AssociationDto>> GetById(int id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(new { message = "Current user id is required." });
        }

        try
        {
            return Ok(await _associationService.GetByIdAsync(currentUserId, id, cancellationToken));
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    // Association Users/Details gap (2026-09-29): combines this Association's own fields with its
    // real Users (EntityType.Association-scoped) in one response — see AssociationService.GetDetailsAsync's
    // own comment for why this is a separate endpoint/DTO rather than added to GetById/AssociationDto.
    [HttpGet("{id:int}/details")]
    public async Task<ActionResult<AssociationDetailsDto>> GetDetails(int id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(new { message = "Current user id is required." });
        }

        try
        {
            return Ok(await _associationService.GetDetailsAsync(currentUserId, id, cancellationToken));
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    [HttpPost]
    public async Task<ActionResult<AssociationDto>> Create([FromBody] CreateAssociationDto dto, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(new { message = "Current user id is required." });
        }

        try
        {
            var result = await _associationService.CreateAsync(currentUserId, dto, cancellationToken);
            return Created($"/api/Associations/{result.Data!.Id}", result);
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    [HttpPut]
    public async Task<ActionResult<AssociationDto>> Update([FromBody] UpdateAssociationDto dto, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(new { message = "Current user id is required." });
        }

        try
        {
            return Ok(await _associationService.UpdateAsync(currentUserId, dto, cancellationToken));
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    [HttpDelete]
    public async Task<IActionResult> Delete([FromQuery] int id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(new { message = "Current user id is required." });
        }

        try
        {
            await _associationService.DeleteAsync(currentUserId, id, cancellationToken);
            return NoContent();
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    [HttpPatch("restore")]
    public async Task<ActionResult<AssociationDto>> Restore([FromQuery] int id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(new { message = "Current user id is required." });
        }

        try
        {
            return Ok(await _associationService.RestoreAsync(currentUserId, id, cancellationToken));
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }
}
