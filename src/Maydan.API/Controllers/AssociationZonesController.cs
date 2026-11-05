using Maydan.Application.DTOs.Associations;
using Maydan.Application.DTOs.Common;
using Maydan.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maydan.API.Controllers;

[Authorize]
[Route("api/[controller]")]
public class AssociationZonesController : ApiControllerBase
{
    private readonly IAssociationZoneService _zoneService;

    public AssociationZonesController(IAssociationZoneService zoneService)
    {
        _zoneService = zoneService;
    }

    [HttpGet("~/api/associations/{associationId:int}/zones")]
    public async Task<ActionResult> GetByAssociation(int associationId, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
            return Unauthorized(new ApiResponse<object?>(false, "المستخدم غير مصرح له.", "Unauthorized.", null, 401));

        try
        {
            var response = await _zoneService.GetByAssociationAsync(currentUserId, associationId, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception ex) { return HandleException(ex); }
    }

    [HttpPost("~/api/associations/{associationId:int}/zones")]
    public async Task<ActionResult> Create(int associationId, [FromBody] CreateAssociationZoneDto dto, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
            return Unauthorized(new ApiResponse<object?>(false, "المستخدم غير مصرح له.", "Unauthorized.", null, 401));

        try
        {
            var response = await _zoneService.CreateAsync(currentUserId, associationId, dto, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception ex) { return HandleException(ex); }
    }

    [HttpPost("~/api/associations/{associationId:int}/zones/{zoneId:int}/book")]
    public async Task<ActionResult> Book(int associationId, int zoneId, [FromBody] BookZoneDto dto, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
            return Unauthorized(new ApiResponse<object?>(false, "المستخدم غير مصرح له.", "Unauthorized.", null, 401));

        try
        {
            var response = await _zoneService.BookAsync(currentUserId, associationId, zoneId, dto, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception ex) { return HandleException(ex); }
    }

    [HttpPost("book-by-location")]
    public async Task<ActionResult> BookByLocation([FromBody] BookByLocationDto dto, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
            return Unauthorized(new ApiResponse<object?>(false, "المستخدم غير مصرح له.", "Unauthorized.", null, 401));

        try
        {
            var response = await _zoneService.BookByLocationAsync(currentUserId, dto, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception ex) { return HandleException(ex); }
    }

    [HttpDelete("~/api/associations/{associationId:int}/zones/{zoneId:int}")]
    public async Task<ActionResult> Delete(int associationId, int zoneId, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
            return Unauthorized(new ApiResponse<object?>(false, "المستخدم غير مصرح له.", "Unauthorized.", null, 401));

        try
        {
            var response = await _zoneService.DeleteAsync(currentUserId, associationId, zoneId, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception ex) { return HandleException(ex); }
    }
}

