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

    private ObjectResult UnauthorizedResponse()
    {
        var response = ApiResponse<object?>.FailureResponse("Unauthorized.", "المستخدم غير مصرح له.");
        response.StatusCode = 401;
        return StatusCode(401, response);
    }

    [HttpGet]
    public async Task<ActionResult> GetAll(
         [FromQuery] int projectId,
         [FromQuery] int associationId,
         CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId)) return UnauthorizedResponse();

        try
        {
            var response = await _supervisorService.GetAllAsync(currentUserId, projectId, associationId, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    [HttpPost]
    public async Task<ActionResult> Create(
        [FromBody] CreateAssociationProjectSupervisorDto dto, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId)) return UnauthorizedResponse();

        try
        {
            var response = await _supervisorService.CreateAsync(dto, currentUserId, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId)) return UnauthorizedResponse();

        try
        {
            var response = await _supervisorService.DeleteAsync(id, currentUserId, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }
}