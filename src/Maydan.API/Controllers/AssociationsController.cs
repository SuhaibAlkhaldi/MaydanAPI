using Maydan.Application.DTOs.Associations;
using Maydan.Application.DTOs.Common;
using Maydan.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maydan.API.Controllers;

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
    public async Task<ActionResult<ApiResponse<List<AssociationDto>>>> GetAll(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(new ApiResponse<List<AssociationDto>>(false, "معرف المستخدم الحالي مطلوب", "Current user id is required", null!));
        }

        var result = await _associationService.GetAllAsync(currentUserId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("order-by-worker-asc")]
    public async Task<ActionResult<ApiResponse<List<AssociationDto>>>> GetOrderedByWorkersCountAsc(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(new ApiResponse<List<AssociationDto>>(false, "معرف المستخدم الحالي مطلوب", "Current user id is required", null!));
        }

        var result = await _associationService.GetOrderedByWorkersCountAsync(currentUserId, ascending: true, cancellationToken);
        return Ok(result);
    }

    [HttpGet("order-by-worker-desc")]
    public async Task<ActionResult<ApiResponse<List<AssociationDto>>>> GetOrderedByWorkersCountDesc(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(new ApiResponse<List<AssociationDto>>(false, "معرف المستخدم الحالي مطلوب", "Current user id is required", null!));
        }

        var result = await _associationService.GetOrderedByWorkersCountAsync(currentUserId, ascending: false, cancellationToken);
        return Ok(result);
    }

    [HttpGet("by-name")]
    public async Task<ActionResult<ApiResponse<List<AssociationDto>>>> SearchByName([FromQuery] string name, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(new ApiResponse<List<AssociationDto>>(false, "معرف المستخدم الحالي مطلوب", "Current user id is required", null!));
        }

        var result = await _associationService.SearchByNameAsync(currentUserId, name, cancellationToken);
        return Ok(result);
    }

    [HttpGet("deleted")]
    public async Task<ActionResult<ApiResponse<List<AssociationDto>>>> GetDeleted(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(new ApiResponse<List<AssociationDto>>(false, "معرف المستخدم الحالي مطلوب", "Current user id is required", null!));
        }

        var result = await _associationService.GetDeletedAsync(currentUserId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("deleted/by-name")]
    public async Task<ActionResult<ApiResponse<List<AssociationDto>>>> SearchDeletedByName([FromQuery] string name, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(new ApiResponse<List<AssociationDto>>(false, "معرف المستخدم الحالي مطلوب", "Current user id is required", null!));
        }

        var result = await _associationService.SearchDeletedByNameAsync(currentUserId, name, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<AssociationDto>>> GetById(int id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(new ApiResponse<AssociationDto>(false, "معرف المستخدم الحالي مطلوب", "Current user id is required", null!));
        }

        var result = await _associationService.GetByIdAsync(currentUserId, id, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpGet("{id:int}/details")]
    public async Task<ActionResult<ApiResponse<AssociationDetailsDto>>> GetDetails(int id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(new ApiResponse<AssociationDetailsDto>(false, "معرف المستخدم الحالي مطلوب", "Current user id is required", null!));
        }

        var result = await _associationService.GetDetailsAsync(currentUserId, id, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<AssociationDto>>> Create([FromBody] CreateAssociationDto dto, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(new ApiResponse<AssociationDto>(false, "معرف المستخدم الحالي مطلوب", "Current user id is required", null!));
        }

        var result = await _associationService.CreateAsync(currentUserId, dto, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Created($"/api/Associations/{result.Data?.Id}", result);
    }

    [HttpPut]
    public async Task<ActionResult<ApiResponse<AssociationDto>>> Update([FromBody] UpdateAssociationDto dto, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(new ApiResponse<AssociationDto>(false, "معرف المستخدم الحالي مطلوب", "Current user id is required", null!));
        }

        var result = await _associationService.UpdateAsync(currentUserId, dto, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpDelete]
    public async Task<ActionResult<ApiResponse<bool>>> Delete([FromQuery] int id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(new ApiResponse<bool>(false, "معرف المستخدم الحالي مطلوب", "Current user id is required", false));
        }

        var result = await _associationService.DeleteAsync(currentUserId, id, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpPatch("restore")]
    public async Task<ActionResult<ApiResponse<AssociationDto>>> Restore([FromQuery] int id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(new ApiResponse<AssociationDto>(false, "معرف المستخدم الحالي مطلوب", "Current user id is required", null!));
        }

        var result = await _associationService.RestoreAsync(currentUserId, id, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}