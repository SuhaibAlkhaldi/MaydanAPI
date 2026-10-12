using Maydan.Application.DTOs.Associations;
using Maydan.Application.DTOs.Common;
using Maydan.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maydan.API.Controllers;

[Authorize]
[Route("api/[controller]")]
public class AssociationsControllerV2 : ApiControllerBase
{
    private readonly IAssociationServiceV2 _associationService;

    public AssociationsControllerV2(IAssociationServiceV2 associationService)
    {
        _associationService = associationService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<AssociationDtoV2>>>> GetAll(CancellationToken cancellationToken)
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
    public async Task<ActionResult<ApiResponse<List<AssociationDtoV2>>>> GetOrderedByWorkersCountAsc(CancellationToken cancellationToken)
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
    public async Task<ActionResult<ApiResponse<List<AssociationDtoV2>>>> GetOrderedByWorkersCountDesc(CancellationToken cancellationToken)
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
    public async Task<ActionResult<ApiResponse<List<AssociationDtoV2>>>> SearchByName([FromQuery] string name, CancellationToken cancellationToken)
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

    [HttpGet("deleted")]
    public async Task<ActionResult<ApiResponse<List<AssociationDtoV2>>>> GetDeleted(CancellationToken cancellationToken)
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
    public async Task<ActionResult<ApiResponse<List<AssociationDtoV2>>>> SearchDeletedByName([FromQuery] string name, CancellationToken cancellationToken)
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
    public async Task<ActionResult<ApiResponse<AssociationDtoV2>>> GetById(int id, CancellationToken cancellationToken)
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

    [HttpGet("{id:int}/details")]
    public async Task<ActionResult<ApiResponse<AssociationDetailsDtoV2>>> GetDetails(int id, CancellationToken cancellationToken)
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
    public async Task<ActionResult<ApiResponse<AssociationDtoV2>>> Create([FromBody] CreateAssociationDtoV2 dto, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(new { message = "Current user id is required." });
        }

        try
        {
            return Ok(await _associationService.CreateAsync(currentUserId, dto, cancellationToken));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<AssociationDtoV2>.FailureResponse(ex.Message, "حدث خطأ غير متوقع."));
        }
    }

    [HttpPut]
    public async Task<ActionResult<ApiResponse<AssociationDtoV2>>> Update([FromBody] UpdateAssociationDtoV2 dto, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(new { message = "Current user id is required." });
        }

        try
        {
            return Ok(await _associationService.UpdateAsync(currentUserId, dto, cancellationToken));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<AssociationDtoV2>.FailureResponse(ex.Message, "حدث خطأ غير متوقع."));
        }
    }

    [HttpDelete]
    public async Task<ActionResult<ApiResponse<bool>>> Delete([FromQuery] int id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(new { message = "Current user id is required." });
        }

        try
        {
            return Ok(await _associationService.DeleteAsync(currentUserId, id, cancellationToken));
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    [HttpPatch("restore")]
    public async Task<ActionResult<ApiResponse<AssociationDtoV2>>> Restore([FromQuery] int id, CancellationToken cancellationToken)
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