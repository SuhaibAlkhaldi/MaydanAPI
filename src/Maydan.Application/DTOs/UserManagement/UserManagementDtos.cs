using Maydan.Domain.Enums;

namespace Maydan.Application.DTOs.UserManagement;

public record PermissionDto(
    int PermissionId,
    string PermissionNameEn,
    string PermissionNameAr,
    string Module);

// MAYD-31: PermissionsPreview added — the real ticket asks for "a preview of permission chips" on
// each card, not just a count. Capped at PermissionSummaryPreviewSize (4, matching the old mock UI's
// "4 chips + N more" pattern the frontend's own dormant `groupsList.more` i18n key was already
// prepared for) — see UserManagementService.MapGroupSummary for how it's populated in the same
// query as PermissionCount, not a second per-group fetch.
public record GroupSummaryDto(
    int GroupId,
    string GroupNameEn,
    string GroupNameAr,
    int PermissionCount,
    int UserCount,
    List<PermissionDto> PermissionsPreview);

public record GroupDetailsDto(
    int GroupId,
    string GroupNameEn,
    string GroupNameAr,
    EntityType EntityType,
    int EntityId,
    List<PermissionDto> Permissions,
    List<UserSummaryDto> Users);

public record UserSummaryDto(
    int UserId,
    string FirstNameEn,
    string LastNameEn,
    string FirstNameAr,
    string LastNameAr,
    string Email,
    string PhoneNumber,
    int RoleId,
    string RoleNameEn,
    string RoleNameAr,
    bool MustResetPassword,
    bool IsActive);

// MAYD-20: real paginated list contract — matches the BRD's own explicit response shape
// (`{ items, totalCount, page, pageSize }`, see Claude outputs/maydan-backend-requirements.md
// section 1.2 in the frontend repo) exactly, camelCase over the wire same as every other DTO here.
public record PagedUsersDto(
    List<UserSummaryDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public record UserDetailsDto(
    int UserId,
    string FirstNameEn,
    string LastNameEn,
    string FirstNameAr,
    string LastNameAr,
    string Email,
    string PhoneNumber,
    int RoleId,
    string RoleNameEn,
    string RoleNameAr,
    EntityType EntityType,
    int EntityId,
    bool MustResetPassword,
    bool IsActive,
    List<PermissionDto> DirectPermissions,
    List<GroupSummaryDto> Groups,
    List<PermissionDto> EffectivePermissions);

public record PermissionMatrixRoleDto(
    int RoleId,
    string RoleNameEn,
    string RoleNameAr,
    List<int> PermissionIds);

public record PermissionMatrixDto(
    List<PermissionDto> Permissions,
    List<PermissionMatrixRoleDto> Roles);

public class CreateEntityUserDto
{
    public string FirstNameEn { get; set; } = string.Empty;
    public string LastNameEn { get; set; } = string.Empty;
    public string FirstNameAr { get; set; } = string.Empty;
    public string LastNameAr { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string InitialPassword { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public List<int> PermissionIds { get; set; } = new();
    public List<int> GroupIds { get; set; } = new();

    // Association Management, Phase 2b: optional real-association override for a cross-entity
    // Association-role create (Bayt-AlUrdon only — see EnsureSameEntityCreation). Null (every
    // existing caller, and every non-Association-role create) keeps the exact prior placeholder-1
    // behavior — see UserManagementService.ResolveEntityForRoleAsync's own comment.
    public int? EntityId { get; set; }
}

public class CreateGroupDto
{
    public string GroupNameEn { get; set; } = string.Empty;
    public string GroupNameAr { get; set; } = string.Empty;
    public List<int> PermissionIds { get; set; } = new();
    public List<int> UserIds { get; set; } = new();
}

public class UpdateGroupDto
{
    public string GroupNameEn { get; set; } = string.Empty;
    public string GroupNameAr { get; set; } = string.Empty;
    public List<int> PermissionIds { get; set; } = new();
    public List<int> UserIds { get; set; } = new();
}

public class UpdateUserPermissionsDto
{
    public List<int> PermissionIds { get; set; } = new();
}

public class UpdateUserGroupsDto
{
    public List<int> GroupIds { get; set; } = new();
}

public class UpdateUserStatusDto
{
    public bool IsActive { get; set; }
}
public record RoleDto(
    int RoleId=0,
    string RoleNameEn="",
    string RoleNameAr = "");
