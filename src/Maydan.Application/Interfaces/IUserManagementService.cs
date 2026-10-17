using Maydan.Application.DTOs.Common;
using Maydan.Application.DTOs.UserManagement;
using Maydan.Domain.Enums;

namespace Maydan.Application.Interfaces;

public interface IUserManagementService
{
    Task<ApiResponse<List<UserSummaryDto>>> GetUsersAsync(int currentUserId, string? search, CancellationToken cancellationToken = default);

    Task<ApiResponse<PagedUsersDto>> GetUsersPagedAsync(
        int currentUserId, string? search, bool? isActive, int page, int pageSize,
        EntityType? entityType, int? entityId, CancellationToken cancellationToken = default);

    Task<ApiResponse<UserDetailsDto>> GetUserDetailsAsync(int currentUserId, int userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<UserDetailsDto>> CreateUserAsync(int currentUserId, CreateEntityUserDto dto, CancellationToken cancellationToken = default);

    Task<ApiResponse<List<PermissionDto>>> GetAvailablePermissionsAsync(int currentUserId, int? roleId, CancellationToken cancellationToken = default);
    Task<ApiResponse<PermissionMatrixDto>> GetPermissionMatrixAsync(int currentUserId, CancellationToken cancellationToken = default);

    Task<ApiResponse<List<GroupSummaryDto>>> GetGroupsAsync(int currentUserId, CancellationToken cancellationToken = default);
    Task<ApiResponse<GroupDetailsDto>> GetGroupAsync(int currentUserId, int groupId, CancellationToken cancellationToken = default);
    Task<ApiResponse<GroupDetailsDto>> CreateGroupAsync(int currentUserId, CreateGroupDto dto, CancellationToken cancellationToken = default);
    Task<ApiResponse<GroupDetailsDto>> UpdateGroupAsync(int currentUserId, int groupId, UpdateGroupDto dto, CancellationToken cancellationToken = default);
    Task<ApiResponse<object?>> DeleteGroupAsync(int currentUserId, int groupId, CancellationToken cancellationToken = default);

    Task<ApiResponse<UserDetailsDto>> UpdateDirectPermissionsAsync(int currentUserId, int userId, UpdateUserPermissionsDto dto, CancellationToken cancellationToken = default);
    Task<ApiResponse<UserDetailsDto>> UpdateGroupsAsync(int currentUserId, int userId, UpdateUserGroupsDto dto, CancellationToken cancellationToken = default);
    Task<ApiResponse<List<PermissionDto>>> GetEffectivePermissionsAsync(int currentUserId, int userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<UserDetailsDto>> UpdateUserStatusAsync(int currentUserId, int userId, UpdateUserStatusDto dto, CancellationToken cancellationToken = default);
}