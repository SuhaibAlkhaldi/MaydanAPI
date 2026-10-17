using Maydan.Application.DTOs.Common;
using Maydan.Application.DTOs.UserManagement;
using Maydan.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maydan.API.Controllers;

[Authorize]
[Route("api/groups")]
public class GroupsController : ApiControllerBase
{
    private readonly IUserManagementService _userManagementService;

    public GroupsController(IUserManagementService userManagementService)
    {
        _userManagementService = userManagementService;
    }

    private ObjectResult UnauthorizedResponse()
    {
        var response = ApiResponse<object?>.FailureResponse("Unauthorized.", "???????? ??? ???? ??.");
        response.StatusCode = 401;
        return StatusCode(401, response);
    }

    [HttpGet("current-entity", Name = "Get Current Entity Groups")]
    public async Task<ActionResult> GetCurrentEntityGroups([FromHeader(Name = "X-Current-User-Id")] int? currentUserHeader, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId)) return UnauthorizedResponse();

        try
        {
            var response = await _userManagementService.GetGroupsAsync(currentUserId, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    [HttpGet("{groupId:int}/details", Name = "Get Group Details")]
    public async Task<ActionResult> GetGroup(int groupId, [FromHeader(Name = "X-Current-User-Id")] int? currentUserHeader, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId)) return UnauthorizedResponse();

        try
        {
            var response = await _userManagementService.GetGroupAsync(currentUserId, groupId, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    [HttpPost("create", Name = "Create Permission Group")]
    public async Task<ActionResult> CreatePermissionGroup([FromBody] CreateGroupDto dto, [FromHeader(Name = "X-Current-User-Id")] int? currentUserHeader, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId)) return UnauthorizedResponse();

        try
        {
            var response = await _userManagementService.CreateGroupAsync(currentUserId, dto, cancellationToken);

            // ??? ??? ??????? ?????? ????? ??? Created ?? ?????? ???? Response ??????
            if (response.Success && response.Data != null)
            {
                return Created($"/api/groups/{response.Data.GroupId}/details", response);
            }

            return StatusCode(response.StatusCode, response);
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    [HttpPut("{groupId:int}/update", Name = "Update Permission Group")]
    public async Task<ActionResult> UpdatePermissionGroup(int groupId, [FromBody] UpdateGroupDto dto, [FromHeader(Name = "X-Current-User-Id")] int? currentUserHeader, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId)) return UnauthorizedResponse();

        try
        {
            var response = await _userManagementService.UpdateGroupAsync(currentUserId, groupId, dto, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    [HttpDelete("{groupId:int}/delete", Name = "Delete Permission Group")]
    public async Task<IActionResult> DeletePermissionGroup(int groupId, [FromHeader(Name = "X-Current-User-Id")] int? currentUserHeader, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId)) return UnauthorizedResponse();

        try
        {
            var response = await _userManagementService.DeleteGroupAsync(currentUserId, groupId, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }
}