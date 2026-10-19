using Maydan.Application.DTOs.Common;
using Maydan.Application.DTOs.UserManagement;
using Maydan.Application.Interfaces;
using Maydan.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maydan.API.Controllers;

[Authorize]
[Route("api/users")]
public class UsersController : ApiControllerBase
{
    private readonly IUserManagementService _userManagementService;

    public UsersController(IUserManagementService userManagementService)
    {
        _userManagementService = userManagementService;
    }

    private ObjectResult UnauthorizedResponse()
    {
        var response = ApiResponse<object?>.FailureResponse("Unauthorized.", "المستخدم غير مصرح له.");
        response.StatusCode = 401;
        return StatusCode(401, response);
    }

    [HttpGet(Name = "Get Users")]
    public async Task<ActionResult> GetUsers(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        [FromQuery] EntityType? entityType,
        [FromQuery] int? entityId,
        [FromHeader(Name = "X-Current-User-Id")] int? currentUserHeader,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId)) return UnauthorizedResponse();

        try
        {
            var response = await _userManagementService.GetUsersPagedAsync(
                currentUserId, search, isActive, page == 0 ? 1 : page, pageSize == 0 ? 10 : pageSize,
                entityType, entityId, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    [HttpGet("current-entity", Name = "Get Current Entity Users")]
    public async Task<ActionResult> GetCurrentEntityUsers([FromHeader(Name = "X-Current-User-Id")] int? currentUserHeader, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId)) return UnauthorizedResponse();

        try
        {
            var response = await _userManagementService.GetUsersAsync(currentUserId, null, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    [HttpGet("search", Name = "Search Current Entity Users")]
    public async Task<ActionResult> SearchCurrentEntityUsers([FromQuery] string? search, [FromHeader(Name = "X-Current-User-Id")] int? currentUserHeader, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId)) return UnauthorizedResponse();

        try
        {
            var response = await _userManagementService.GetUsersAsync(currentUserId, search, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    [HttpGet("{userId:int}/details", Name = "Get User Details")]
    public async Task<ActionResult> GetUserDetails(int userId, [FromHeader(Name = "X-Current-User-Id")] int? currentUserHeader, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId)) return UnauthorizedResponse();

        try
        {
            var response = await _userManagementService.GetUserDetailsAsync(currentUserId, userId, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    [HttpPost("create", Name = "Create User")]
        public async Task<ActionResult> CreateUser(
             [FromBody] CreateEntityUserDto dto,
                [FromHeader(Name = "X-Current-User-Id")] int? currentUserHeader,
                     [FromHeader(Name = "X-Entity-Id")] int? entityIdHeader, 
                           CancellationToken cancellationToken)

    {
        if (!TryGetCurrentUserId(out var currentUserId)) return UnauthorizedResponse();

        try
        {
         
            if (entityIdHeader.HasValue && dto.EntityId <= 0)
            {
                dto.EntityId = entityIdHeader.Value;
            }

            var response = await _userManagementService.CreateUserAsync(currentUserId, dto, cancellationToken);
            if (response.Success && response.Data != null)
            {
                return Created($"/api/users/{response.Data.UserId}/details", response);
            }
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }
    [HttpGet("roles/available", Name = "Get Roles For Entity")]
    public async Task<ActionResult> GetRolesForEntity(
    [FromHeader(Name = "X-Target-Entity-Type")] EntityType? targetEntityTypeHeader,
      [FromHeader(Name = "X-Current-User-Id")] int? currentUserHeader,
      CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId)) return UnauthorizedResponse();

        try
        {
            var response = await _userManagementService.GetRolesForEntityAsync(currentUserId, targetEntityTypeHeader, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    [HttpPut("{userId:int}/direct-permissions/update", Name = "Update User Direct Permissions")]
    public async Task<ActionResult> UpdateUserDirectPermissions(int userId, [FromBody] UpdateUserPermissionsDto dto, [FromHeader(Name = "X-Current-User-Id")] int? currentUserHeader, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId)) return UnauthorizedResponse();

        try
        {
            var response = await _userManagementService.UpdateDirectPermissionsAsync(currentUserId, userId, dto, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    [HttpPut("{userId:int}/groups/update", Name = "Update User Groups")]
    public async Task<ActionResult> UpdateUserGroups(int userId, [FromBody] UpdateUserGroupsDto dto, [FromHeader(Name = "X-Current-User-Id")] int? currentUserHeader, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId)) return UnauthorizedResponse();

        try
        {
            var response = await _userManagementService.UpdateGroupsAsync(currentUserId, userId, dto, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    [HttpPatch("{userId:int}/status/update", Name = "Update User Status")]
    public async Task<ActionResult> UpdateUserStatus(int userId, [FromBody] UpdateUserStatusDto dto, [FromHeader(Name = "X-Current-User-Id")] int? currentUserHeader, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId)) return UnauthorizedResponse();

        try
        {
            var response = await _userManagementService.UpdateUserStatusAsync(currentUserId, userId, dto, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    [HttpGet("{userId:int}/effective-permissions", Name = "Get User Effective Permissions")]
    public async Task<ActionResult> GetUserEffectivePermissions(int userId, [FromHeader(Name = "X-Current-User-Id")] int? currentUserHeader, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId)) return UnauthorizedResponse();

        try
        {
            var response = await _userManagementService.GetEffectivePermissionsAsync(currentUserId, userId, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }
}