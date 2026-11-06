using Maydan.API.Models.Projects;
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
    public async Task<ActionResult> GetAll(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
            return Unauthorized(new ApiResponse<object?>(false, "المستخدم غير مصرح له.", "Unauthorized.", null, 401));

        try
        {
            var response = await _associationService.GetAllAsync(currentUserId, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception exception) { return HandleException(exception); }
    }

    [HttpGet("order-by-worker-asc")]
    public async Task<ActionResult> GetOrderedByWorkersCountAsc(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
            return Unauthorized(new ApiResponse<object?>(false, "المستخدم غير مصرح له.", "Unauthorized.", null, 401));

        try
        {
            var response = await _associationService.GetOrderedByWorkersCountAsync(currentUserId, ascending: true, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception exception) { return HandleException(exception); }
    }

    [HttpGet("order-by-worker-desc")]
    public async Task<ActionResult> GetOrderedByWorkersCountDesc(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
            return Unauthorized(new ApiResponse<object?>(false, "المستخدم غير مصرح له.", "Unauthorized.", null, 401));

        try
        {
            var response = await _associationService.GetOrderedByWorkersCountAsync(currentUserId, ascending: false, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception exception) { return HandleException(exception); }
    }

    [HttpGet("by-name")]
    public async Task<ActionResult> SearchByName([FromQuery] string name, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
            return Unauthorized(new ApiResponse<object?>(false, "المستخدم غير مصرح له.", "Unauthorized.", null, 401));

        try
        {
            var response = await _associationService.SearchByNameAsync(currentUserId, name, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception exception) { return HandleException(exception); }
    }

    [HttpGet("deleted")]
    public async Task<ActionResult> GetDeleted(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
            return Unauthorized(new ApiResponse<object?>(false, "المستخدم غير مصرح له.", "Unauthorized.", null, 401));

        try
        {
            var response = await _associationService.GetDeletedAsync(currentUserId, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception exception) { return HandleException(exception); }
    }

    [HttpGet("deleted/by-name")]
    public async Task<ActionResult> SearchDeletedByName([FromQuery] string name, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
            return Unauthorized(new ApiResponse<object?>(false, "المستخدم غير مصرح له.", "Unauthorized.", null, 401));

        try
        {
            var response = await _associationService.SearchDeletedByNameAsync(currentUserId, name, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception exception) { return HandleException(exception); }
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
            return Unauthorized(new ApiResponse<object?>(false, "المستخدم غير مصرح له.", "Unauthorized.", null, 401));

        try
        {
            var response = await _associationService.GetByIdAsync(currentUserId, id, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception exception) { return HandleException(exception); }
    }

    [HttpGet("{id:int}/details")]
    public async Task<ActionResult> GetDetails(int id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
            return Unauthorized(new ApiResponse<object?>(false, "المستخدم غير مصرح له.", "Unauthorized.", null, 401));

        try
        {
            var response = await _associationService.GetDetailsAsync(currentUserId, id, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception exception) { return HandleException(exception); }
    }

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] CreateAssociationDto dto, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
            return Unauthorized(new ApiResponse<object?>(false, "المستخدم غير مصرح له.", "Unauthorized.", null, 401));

        try
        {
            var response = await _associationService.CreateAsync(currentUserId, dto, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception exception) { return HandleException(exception); }
    }

    [HttpPut]
    public async Task<ActionResult> Update([FromBody] UpdateAssociationDto dto, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
            return Unauthorized(new ApiResponse<object?>(false, "المستخدم غير مصرح له.", "Unauthorized.", null, 401));

        try
        {
            var response = await _associationService.UpdateAsync(currentUserId, dto, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception exception) { return HandleException(exception); }
    }

    [HttpDelete]
    public async Task<ActionResult> Delete([FromQuery] int id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
            return Unauthorized(new ApiResponse<object?>(false, "المستخدم غير مصرح له.", "Unauthorized.", null, 401));

        try
        {
            var response = await _associationService.DeleteAsync(currentUserId, id, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception exception) { return HandleException(exception); }
    }

    [HttpPatch("restore")]
    public async Task<ActionResult> Restore([FromQuery] int id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
            return Unauthorized(new ApiResponse<object?>(false, "المستخدم غير مصرح له.", "Unauthorized.", null, 401));

        try
        {
            var response = await _associationService.RestoreAsync(currentUserId, id, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception exception) { return HandleException(exception); }
    }
}