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

    // MAYD-20: the real Users List page's endpoint — paginated, status-filterable, searchable by
    // name/email/phone, entity-scoped per the real Business Rule (Bayt-AlUrdon/ASEZA may pass
    // entityType/entityId to view a different entity; everyone else may not — enforced in
    // UserManagementService.GetUsersPagedAsync, not here). Deliberately a separate action from
    // current-entity/search below rather than adding params to either of those — both have other
    // real callers (the UserOption dropdown API) that expect a plain array, not this paginated
    // envelope, and neither needs pagination for that lightweight use.
    [HttpGet(Name = "Get Users")]
    public async Task<ActionResult<PagedUsersDto>> GetUsers(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        [FromQuery] EntityType? entityType,
        [FromQuery] int? entityId,
        [FromHeader(Name = "X-Current-User-Id")] int? currentUserHeader,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(new { message = "Current user id is required." });
        }

        try
        {
            return Ok(await _userManagementService.GetUsersPagedAsync(
                currentUserId, search, isActive, page == 0 ? 1 : page, pageSize == 0 ? 10 : pageSize,
                entityType, entityId, cancellationToken));
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    [HttpGet("current-entity", Name = "Get Current Entity Users")]
    public async Task<ActionResult<List<UserSummaryDto>>> GetCurrentEntityUsers([FromHeader(Name = "X-Current-User-Id")] int? currentUserHeader, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(new { message = "Current user id is required." });
        }

        try
        {
            return Ok(await _userManagementService.GetUsersAsync(currentUserId, null, cancellationToken));
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    [HttpGet("search", Name = "Search Current Entity Users")]
    public async Task<ActionResult<List<UserSummaryDto>>> SearchCurrentEntityUsers([FromQuery] string? search, [FromHeader(Name = "X-Current-User-Id")] int? currentUserHeader, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(new { message = "Current user id is required." });
        }

        try
        {
            return Ok(await _userManagementService.GetUsersAsync(currentUserId, search, cancellationToken));
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    [HttpGet("{userId:int}/details", Name = "Get User Details")]
    public async Task<ActionResult<UserDetailsDto>> GetUserDetails(int userId, [FromHeader(Name = "X-Current-User-Id")] int? currentUserHeader, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(new { message = "Current user id is required." });
        }

        try
        {
            return Ok(await _userManagementService.GetUserDetailsAsync(currentUserId, userId, cancellationToken));
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    [HttpPost("register", Name = "Register User")]
    public async Task<ActionResult<UserDetailsDto>> RegisterUser([FromBody] CreateEntityUserDto dto, [FromHeader(Name = "X-Current-User-Id")] int? currentUserHeader, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(new { message = "Current user id is required." });
        }

        try
        {
            var user = await _userManagementService.CreateUserAsync(currentUserId, dto, cancellationToken);
            return Created($"/api/users/{user.UserId}/details", user);
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    [HttpPut("{userId:int}/direct-permissions/update", Name = "Update User Direct Permissions")]
    public async Task<ActionResult<UserDetailsDto>> UpdateUserDirectPermissions(int userId, [FromBody] UpdateUserPermissionsDto dto, [FromHeader(Name = "X-Current-User-Id")] int? currentUserHeader, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(new { message = "Current user id is required." });
        }

        try
        {
            return Ok(await _userManagementService.UpdateDirectPermissionsAsync(currentUserId, userId, dto, cancellationToken));
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    [HttpPut("{userId:int}/groups/update", Name = "Update User Groups")]
    public async Task<ActionResult<UserDetailsDto>> UpdateUserGroups(int userId, [FromBody] UpdateUserGroupsDto dto, [FromHeader(Name = "X-Current-User-Id")] int? currentUserHeader, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(new { message = "Current user id is required." });
        }

        try
        {
            return Ok(await _userManagementService.UpdateGroupsAsync(currentUserId, userId, dto, cancellationToken));
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    // Activate/Deactivate User (UserManagementModule.md Phase 4.3) — no MAYD subtask covers this
    // directly, see UserManagementService.UpdateUserStatusAsync's own comment for the full rationale
    // (self-check, entity-scoping, and the new Manage Users permission gate). PATCH, not PUT, since
    // this only ever flips one field rather than replacing a whole collection the way
    // direct-permissions/update and groups/update do — same "...update" naming shape kept anyway for
    // consistency with those two siblings.
    [HttpPatch("{userId:int}/status/update", Name = "Update User Status")]
    public async Task<ActionResult<UserDetailsDto>> UpdateUserStatus(int userId, [FromBody] UpdateUserStatusDto dto, [FromHeader(Name = "X-Current-User-Id")] int? currentUserHeader, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(new { message = "Current user id is required." });
        }

        try
        {
            return Ok(await _userManagementService.UpdateUserStatusAsync(currentUserId, userId, dto, cancellationToken));
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    [HttpGet("{userId:int}/effective-permissions", Name = "Get User Effective Permissions")]
    public async Task<ActionResult<List<PermissionDto>>> GetUserEffectivePermissions(int userId, [FromHeader(Name = "X-Current-User-Id")] int? currentUserHeader, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(new { message = "Current user id is required." });
        }

        try
        {
            return Ok(await _userManagementService.GetEffectivePermissionsAsync(currentUserId, userId, cancellationToken));
        } 
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }
}
