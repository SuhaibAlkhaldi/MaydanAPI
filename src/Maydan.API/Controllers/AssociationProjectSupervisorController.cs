using Maydan.Application.DTOs;
using Maydan.Application.DTOs.Associations;
using Maydan.Application.DTOs.Common;
using Maydan.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maydan.API.Controllers;

[Authorize]
[Route("api/[controller]")]
public class AssociationProjectSupervisorController : ApiControllerBase
{
    private readonly IAssociationProjectSupervisorService _supervisorService;

    public AssociationProjectSupervisorController(IAssociationProjectSupervisorService supervisorService)
    {
        _supervisorService = supervisorService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<AssociationProjectSupervisorDto>>>> GetAll(
        [FromQuery] int projectId, [FromQuery] int associationId, CancellationToken cancellationToken)
    {
        var result = await _supervisorService.GetAllAsync(projectId, associationId, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<AssociationProjectSupervisorDto>>> Create(
        [FromBody] CreateAssociationProjectSupervisorDto dto, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(ApiResponse<AssociationProjectSupervisorDto>.FailureResponse(
                "Current user id is required.",
                "معرف المستخدم الحالي مطلوب"
            ));
        }

        var result = await _supervisorService.CreateAsync(dto, currentUserId, cancellationToken);
        return Created($"/api/AssociationProjectSupervisor/{result.Data?.Id}", result);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(int id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(ApiResponse<bool>.FailureResponse(
                "Current user id is required.",
                "معرف المستخدم الحالي مطلوب"
            ));
        }

        var result = await _supervisorService.DeleteAsync(id, currentUserId, cancellationToken);
        return Ok(result);
    }
}