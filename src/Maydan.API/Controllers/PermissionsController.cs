using Maydan.Application.DTOs.Common;
using Maydan.Application.DTOs.UserManagement;
using Maydan.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maydan.API.Controllers;

[Authorize]
[Route("api/permissions")]
public class PermissionsController : ApiControllerBase
{
    private readonly IUserManagementService _userManagementService;

    public PermissionsController(IUserManagementService userManagementService)
    {
        _userManagementService = userManagementService;
    }

    private ObjectResult UnauthorizedResponse()
    {
        var response = ApiResponse<object?>.FailureResponse("Unauthorized.", "???????? ??? ???? ??.");
        response.StatusCode = 401;
        return StatusCode(401, response);
    }

    [HttpGet("available", Name = "Get Available Permissions")]
    public async Task<ActionResult> GetAvailablePermissions([FromQuery] int? roleId, [FromHeader(Name = "X-Current-User-Id")] int? currentUserHeader, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId)) return UnauthorizedResponse();

        try
        {
            var response = await _userManagementService.GetAvailablePermissionsAsync(currentUserId, roleId, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    [HttpGet("matrix", Name = "Get Permission Matrix")]
    public async Task<ActionResult> GetPermissionMatrix([FromHeader(Name = "X-Current-User-Id")] int? currentUserHeader, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId)) return UnauthorizedResponse();

        try
        {
            var response = await _userManagementService.GetPermissionMatrixAsync(currentUserId, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }
}