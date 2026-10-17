using Maydan.Application.DTOs.Common;
using Maydan.Application.DTOs.UserManagement;
using Maydan.Application.Interfaces;
using Maydan.Domain.Constants;
using Maydan.Domain.Entities;
using Maydan.Domain.Enums;

namespace Maydan.Application.Services;

public class UserManagementService : IUserManagementService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;

    public UserManagementService(IUnitOfWork unitOfWork, IPasswordHasher passwordHasher)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
    }

    public async Task<ApiResponse<List<UserSummaryDto>>> GetUsersAsync(int currentUserId, string? search, CancellationToken cancellationToken = default)
    {
        var (currentUser, error) = await GetCurrentUserAsync<List<UserSummaryDto>>(currentUserId, cancellationToken);
        if (error != null) return error;

        var users = await _unitOfWork.Users.GetByEntityAsync(currentUser!.EntityType, currentUser.EntityId, search, cancellationToken);

        return ApiResponse<List<UserSummaryDto>>.SuccessResponse(
            users.Select(MapUserSummary).ToList(),
            "Users retrieved successfully.",
            "تم استرجاع قائمة المستخدمين بنجاح.");
    }

    public async Task<ApiResponse<PagedUsersDto>> GetUsersPagedAsync(
        int currentUserId, string? search, bool? isActive, int page, int pageSize,
        EntityType? entityType, int? entityId, CancellationToken cancellationToken = default)
    {
        var currentUser = await _unitOfWork.Users.GetWithPermissionsAsync(currentUserId, cancellationToken);
        if (currentUser == null)
            return ApiResponse<PagedUsersDto>.FailureResponse("Current user was not found.", "المستخدم الحالي غير موجود.");

        if (!currentUser.IsActive)
            return ApiResponse<PagedUsersDto>.FailureResponse("Current user is inactive.", "المستخدم الحالي غير فعّال.");

        if (!GetEffectivePermissionIds(currentUser).Overlaps(new[] { ViewUsersPermissionId, ManageUsersPermissionId }))
            return ApiResponse<PagedUsersDto>.FailureResponse("Caller does not hold the View Users permission.", "لا تملك الصلاحية المطلوبة لعرض المستخدمين.");

        var (targetEntityType, targetEntityId, targetError) = ResolveTargetEntity<PagedUsersDto>(currentUser, entityType, entityId);
        if (targetError != null) return targetError;

        var safePage = page < 1 ? 1 : page;
        var safePageSize = pageSize is < 1 or > 100 ? 10 : pageSize;

        var (users, totalCount) = await _unitOfWork.Users.GetPagedByEntityAsync(
            targetEntityType!.Value, targetEntityId!.Value, search, isActive, safePage, safePageSize, cancellationToken);

        return ApiResponse<PagedUsersDto>.SuccessResponse(
            new PagedUsersDto(users.Select(MapUserSummary).ToList(), totalCount, safePage, safePageSize),
            "Paged users retrieved successfully.",
            "تم استرجاع المستخدمين بنجاح.");
    }

    public async Task<ApiResponse<UserDetailsDto>> GetUserDetailsAsync(int currentUserId, int userId, CancellationToken cancellationToken = default)
    {
        var (currentUser, error) = await GetCurrentUserAsync<UserDetailsDto>(currentUserId, cancellationToken);
        if (error != null) return error;

        var (user, viewError) = await GetViewableUserAsync<UserDetailsDto>(userId, currentUser!, cancellationToken);
        if (viewError != null) return viewError;

        return ApiResponse<UserDetailsDto>.SuccessResponse(MapUserDetails(user!), "User details retrieved successfully.", "تم استرجاع تفاصيل المستخدم بنجاح.");
    }

    public async Task<ApiResponse<UserDetailsDto>> CreateUserAsync(int currentUserId, CreateEntityUserDto dto, CancellationToken cancellationToken = default)
    {
        var (currentUser, error) = await GetCurrentUserAsync<UserDetailsDto>(currentUserId, cancellationToken);
        if (error != null) return error;

        var valError = ValidateUserPayload<UserDetailsDto>(dto);
        if (valError != null) return valError;

        var email = dto.Email.Trim();
        if (await _unitOfWork.Users.EmailExistsAsync(email, cancellationToken))
            return ApiResponse<UserDetailsDto>.FailureResponse("User email already exists.", "البريد الإلكتروني موجود مسبقاً.");

        var role = await _unitOfWork.Roles.GetWithPermissionsAsync(dto.RoleId, cancellationToken);
        if (role == null)
            return ApiResponse<UserDetailsDto>.FailureResponse("Role was not found.", "الدور غير موجود.");

        var creationError = EnsureEntityCreationAllowed<UserDetailsDto>(currentUser!, role);
        if (creationError != null) return creationError;

        var permissionIds = NormalizeIds(dto.PermissionIds);
        var permError = await EnsurePermissionsExistAsync<UserDetailsDto>(permissionIds, cancellationToken);
        if (permError != null) return permError;

        if (currentUser!.RoleId != BaytAlUrdonRoleId)
        {
            var roleAllowError = EnsurePermissionsAllowedForRole<UserDetailsDto>(role, permissionIds, GetCallerDelegatablePermissionIds(currentUser, role));
            if (roleAllowError != null) return roleAllowError;
        }

        var (targetEntityType, targetEntityId, entityError) = role.RoleId == currentUser!.RoleId
            ? (currentUser.EntityType, currentUser.EntityId, null)
            : await ResolveEntityForRoleAsync<UserDetailsDto>(role.RoleId, dto.EntityId, cancellationToken);
        if (entityError != null) return entityError;

        var groupIds = NormalizeIds(dto.GroupIds);
        var (groups, groupError) = await EnsureGroupsInEntityAsync<UserDetailsDto>(groupIds, targetEntityType!.Value, targetEntityId!.Value, cancellationToken);
        if (groupError != null) return groupError;

        var user = new User
        {
            FirstNameEn = dto.FirstNameEn.Trim(),
            LastNameEn = dto.LastNameEn.Trim(),
            FirstNameAr = dto.FirstNameAr.Trim(),
            LastNameAr = dto.LastNameAr.Trim(),
            Email = email,
            PhoneNumber = dto.PhoneNumber.Trim(),
            PasswordHash = _passwordHasher.HashPassword(dto.InitialPassword),
            MustResetPassword = true,
            IsActive = true,
            RoleId = role.RoleId,
            EntityType = targetEntityType.Value,
            EntityId = targetEntityId.Value
        };

        foreach (var permissionId in permissionIds)
            user.UserPermissions.Add(new UserPermission { PermissionId = permissionId, IsActive = true });

        foreach (var group in groups!)
            user.UserGroups.Add(new UserGroup { GroupId = group.GroupId });

        await _unitOfWork.Users.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var createdUser = await _unitOfWork.Users.GetDetailsReadOnlyAsync(user.UserId, cancellationToken);
        if (createdUser == null)
            return ApiResponse<UserDetailsDto>.FailureResponse("Created user was not found.", "المستخدم المنشأ غير موجود.");

        return ApiResponse<UserDetailsDto>.SuccessResponse(MapUserDetails(createdUser), "User created successfully.", "تم إنشاء المستخدم بنجاح.", 201);
    }

    public async Task<ApiResponse<List<PermissionDto>>> GetAvailablePermissionsAsync(int currentUserId, int? roleId, CancellationToken cancellationToken = default)
    {
        var (caller, error) = await GetCurrentUserAsync<List<PermissionDto>>(currentUserId, cancellationToken);
        if (error != null) return error;

        // التعديل هون: إذا ما انبعت roleId أو كان اليوزر هو أدمن بيت الأردن، رجع كل صلاحيات النظام الفعالة مرتبة
        if (!roleId.HasValue || caller!.RoleId == BaytAlUrdonRoleId)
        {
            var allPermissions = await _unitOfWork.Permissions.GetAllAsync(cancellationToken);
            var activePermissions = allPermissions
                .Where(p => p.IsActive)
                .Select(MapPermission)
                .OrderBy(p => p.Module)
                .ThenBy(p => p.PermissionNameEn)
                .ToList();

            return ApiResponse<List<PermissionDto>>.SuccessResponse(activePermissions, "Permissions retrieved successfully.", "تم استرجاع الصلاحيات بنجاح.");
        }

        var role = await _unitOfWork.Roles.GetWithPermissionsAsync(roleId.Value, cancellationToken);
        if (role == null)
            return ApiResponse<List<PermissionDto>>.FailureResponse("Role was not found.", "الدور غير موجود.");

        var delegatableIds = GetCallerDelegatablePermissionIds(caller!, role);
        var delegatablePermissions = caller!.UserPermissions
            .Where(up => up.IsActive && up.Permission.IsActive && delegatableIds.Contains(up.PermissionId))
            .Select(up => up.Permission)
            .Concat(caller.UserGroups
                .Where(ug => ug.Group.IsActive)
                .SelectMany(ug => ug.Group.GroupPermissions)
                .Where(gp => gp.IsActive && gp.Permission.IsActive && delegatableIds.Contains(gp.PermissionId))
                .Select(gp => gp.Permission));

        var result = role.RolePermissions
            .Where(rp => rp.IsActive && rp.Permission.IsActive)
            .Select(rp => rp.Permission)
            .Concat(delegatablePermissions)
            .DistinctBy(p => p.PermissionId)
            .Select(MapPermission)
            .OrderBy(p => p.Module)
            .ThenBy(p => p.PermissionNameEn)
            .ToList();

        return ApiResponse<List<PermissionDto>>.SuccessResponse(result, "Available permissions retrieved successfully.", "تم استرجاع الصلاحيات المتاحة بنجاح.");
    }

    public async Task<ApiResponse<PermissionMatrixDto>> GetPermissionMatrixAsync(int currentUserId, CancellationToken cancellationToken = default)
    {
        var (currentUser, error) = await GetCurrentUserAsync<PermissionMatrixDto>(currentUserId, cancellationToken);
        if (error != null) return error;

        var permissions = await _unitOfWork.Permissions.GetAllAsync(cancellationToken);
        var roles = await _unitOfWork.Roles.GetAllWithPermissionsAsync(cancellationToken);

        if (currentUser!.EntityType is EntityType.ProductionCompany or EntityType.Association)
        {
            roles = roles.Where(role =>
                role.TargetEntityType == currentUser.EntityType ||
                (currentUser.EntityType == EntityType.ProductionCompany && role.RoleId == ProductionHouseRoleId) ||
                (currentUser.EntityType == EntityType.Association && role.RoleId == AssociationRoleId)
            ).ToList();
        }

        var matrix = new PermissionMatrixDto(
            permissions.Select(MapPermission).ToList(),
            roles.Select(role => new PermissionMatrixRoleDto(
                role.RoleId,
                role.RoleNameEn,
                role.RoleNameAr,
                role.RolePermissions
                    .Where(rp => rp.IsActive && rp.Permission.IsActive)
                    .Select(rp => rp.PermissionId)
                    .Distinct()
                    .OrderBy(id => id)
                    .ToList()))
                .ToList());

        return ApiResponse<PermissionMatrixDto>.SuccessResponse(matrix, "Permission matrix retrieved successfully.", "تم استرجاع مصفوفة الصلاحيات بنجاح.");
    }

    public async Task<ApiResponse<List<GroupSummaryDto>>> GetGroupsAsync(int currentUserId, CancellationToken cancellationToken = default)
    {
        var (currentUser, error) = await GetCurrentUserAsync<List<GroupSummaryDto>>(currentUserId, cancellationToken);
        if (error != null) return error;

        var groups = await _unitOfWork.Groups.GetByEntityAsync(currentUser!.EntityType, currentUser.EntityId, cancellationToken);

        return ApiResponse<List<GroupSummaryDto>>.SuccessResponse(groups.Select(MapGroupSummary).ToList(), "Groups retrieved successfully.", "تم استرجاع المجموعات بنجاح.");
    }

    public async Task<ApiResponse<GroupDetailsDto>> GetGroupAsync(int currentUserId, int groupId, CancellationToken cancellationToken = default)
    {
        var (currentUser, error) = await GetCurrentUserAsync<GroupDetailsDto>(currentUserId, cancellationToken);
        if (error != null) return error;

        var (group, groupError) = await GetScopedGroupAsync<GroupDetailsDto>(groupId, currentUser!, cancellationToken);
        if (groupError != null) return groupError;

        return ApiResponse<GroupDetailsDto>.SuccessResponse(MapGroupDetails(group!), "Group retrieved successfully.", "تم استرجاع المجموعة بنجاح.");
    }

    public async Task<ApiResponse<GroupDetailsDto>> CreateGroupAsync(int currentUserId, CreateGroupDto dto, CancellationToken cancellationToken = default)
    {
        var (currentUser, error) = await GetCurrentUserAsync<GroupDetailsDto>(currentUserId, cancellationToken);
        if (error != null) return error;

        var valError = ValidateGroupPayload<GroupDetailsDto>(dto.GroupNameEn, dto.GroupNameAr);
        if (valError != null) return valError;

        if (await _unitOfWork.Groups.NameExistsInEntityAsync(currentUser!.EntityType, currentUser.EntityId, dto.GroupNameEn, dto.GroupNameAr, null, cancellationToken))
            return ApiResponse<GroupDetailsDto>.FailureResponse("Group English or Arabic name already exists in this entity.", "اسم المجموعة مكرر في هذا الكيان.");

        var permissionIds = NormalizeIds(dto.PermissionIds);
        var permError = await EnsurePermissionsExistAsync<GroupDetailsDto>(permissionIds, cancellationToken);
        if (permError != null) return permError;

        var userIds = NormalizeIds(dto.UserIds);
        var (users, userError) = await EnsureUsersInEntityAsync<GroupDetailsDto>(userIds, currentUser, cancellationToken);
        if (userError != null) return userError;

        var group = new Group
        {
            GroupNameEn = dto.GroupNameEn.Trim(),
            GroupNameAr = dto.GroupNameAr.Trim(),
            EntityType = currentUser.EntityType,
            EntityId = currentUser.EntityId,
            IsActive = true
        };

        foreach (var permissionId in permissionIds)
            group.GroupPermissions.Add(new GroupPermission { PermissionId = permissionId, IsActive = true });

        foreach (var user in users!)
            group.UserGroups.Add(new UserGroup { UserId = user.UserId });

        await _unitOfWork.Groups.AddAsync(group, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetGroupAsync(currentUserId, group.GroupId, cancellationToken);
    }

    public async Task<ApiResponse<GroupDetailsDto>> UpdateGroupAsync(int currentUserId, int groupId, UpdateGroupDto dto, CancellationToken cancellationToken = default)
    {
        var (currentUser, error) = await GetCurrentUserAsync<GroupDetailsDto>(currentUserId, cancellationToken);
        if (error != null) return error;

        var (group, groupError) = await GetScopedGroupAsync<GroupDetailsDto>(groupId, currentUser!, cancellationToken);
        if (groupError != null) return groupError;

        var valError = ValidateGroupPayload<GroupDetailsDto>(dto.GroupNameEn, dto.GroupNameAr);
        if (valError != null) return valError;

        if (await _unitOfWork.Groups.NameExistsInEntityAsync(currentUser!.EntityType, currentUser.EntityId, dto.GroupNameEn, dto.GroupNameAr, groupId, cancellationToken))
            return ApiResponse<GroupDetailsDto>.FailureResponse("Group English or Arabic name already exists in this entity.", "اسم المجموعة مكرر في هذا الكيان.");

        var permissionIds = NormalizeIds(dto.PermissionIds);
        var permError = await EnsurePermissionsExistAsync<GroupDetailsDto>(permissionIds, cancellationToken);
        if (permError != null) return permError;

        var userIds = NormalizeIds(dto.UserIds);
        var (users, userError) = await EnsureUsersInEntityAsync<GroupDetailsDto>(userIds, currentUser, cancellationToken);
        if (userError != null) return userError;

        group!.GroupNameEn = dto.GroupNameEn.Trim();
        group.GroupNameAr = dto.GroupNameAr.Trim();
        SyncGroupPermissions(group, permissionIds);
        SyncGroupUsers(group, users!.Select(u => u.UserId).ToList());

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetGroupAsync(currentUserId, groupId, cancellationToken);
    }

    public async Task<ApiResponse<object?>> DeleteGroupAsync(int currentUserId, int groupId, CancellationToken cancellationToken = default)
    {
        var (currentUser, error) = await GetCurrentUserAsync<object?>(currentUserId, cancellationToken);
        if (error != null) return error;

        var (group, groupError) = await GetScopedGroupAsync<object?>(groupId, currentUser!, cancellationToken);
        if (groupError != null) return groupError;

        var permissionIds = group!.GroupPermissions
            .Where(gp => gp.IsActive && (gp.Permission == null || gp.Permission.IsActive))
            .Select(gp => gp.PermissionId)
            .Distinct()
            .ToList();

        if (permissionIds.Any() && group.UserGroups != null)
        {
            foreach (var userGroup in group.UserGroups)
            {
                var user = userGroup.User;
                if (user == null) continue;

                var existingDirectPermissionIds = user.UserPermissions
                    .Where(up => up.IsActive)
                    .Select(up => up.PermissionId)
                    .ToHashSet();

                foreach (var permissionId in permissionIds)
                {
                    if (existingDirectPermissionIds.Add(permissionId))
                    {
                        user.UserPermissions.Add(new UserPermission
                        {
                            UserId = user.UserId,
                            PermissionId = permissionId,
                            IsActive = true
                        });
                    }
                }
            }
        }

        _unitOfWork.Groups.Remove(group);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<object?>.SuccessResponse(null, "Group deleted successfully.", "تم حذف المجموعة بنجاح.");
    }

    public async Task<ApiResponse<UserDetailsDto>> UpdateDirectPermissionsAsync(int currentUserId, int userId, UpdateUserPermissionsDto dto, CancellationToken cancellationToken = default)
    {
        if (currentUserId == userId)
            return ApiResponse<UserDetailsDto>.FailureResponse("You cannot modify your own permissions.", "لا يمكنك تعديل صلاحياتك الخاصة.");

        var (currentUser, error) = await GetCurrentUserAsync<UserDetailsDto>(currentUserId, cancellationToken);
        if (error != null) return error;

        var (user, scopeError) = await GetScopedUserAsync<UserDetailsDto>(userId, currentUser!, cancellationToken);
        if (scopeError != null) return scopeError;

        var permissionIds = NormalizeIds(dto.PermissionIds);
        var permError = await EnsurePermissionsExistAsync<UserDetailsDto>(permissionIds, cancellationToken);
        if (permError != null) return permError;

        var authError = await EnsurePermissionsAllowedForRoleAsync<UserDetailsDto>(currentUser!, user!.RoleId, permissionIds, cancellationToken);
        if (authError != null) return authError;

        SyncUserPermissions(user, permissionIds);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetUserDetailsAsync(currentUserId, userId, cancellationToken);
    }

    public async Task<ApiResponse<UserDetailsDto>> UpdateGroupsAsync(int currentUserId, int userId, UpdateUserGroupsDto dto, CancellationToken cancellationToken = default)
    {
        var (currentUser, error) = await GetCurrentUserAsync<UserDetailsDto>(currentUserId, cancellationToken);
        if (error != null) return error;

        var (user, scopeError) = await GetScopedUserAsync<UserDetailsDto>(userId, currentUser!, cancellationToken);
        if (scopeError != null) return scopeError;

        var groupIds = NormalizeIds(dto.GroupIds);
        var (_, groupError) = await EnsureGroupsInEntityAsync<UserDetailsDto>(groupIds, currentUser!.EntityType, currentUser.EntityId, cancellationToken);
        if (groupError != null) return groupError;

        SyncUserGroups(user!, groupIds);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetUserDetailsAsync(currentUserId, userId, cancellationToken);
    }

    public async Task<ApiResponse<List<PermissionDto>>> GetEffectivePermissionsAsync(int currentUserId, int userId, CancellationToken cancellationToken = default)
    {
        var response = await GetUserDetailsAsync(currentUserId, userId, cancellationToken);
        if (!response.Success)
            return ApiResponse<List<PermissionDto>>.FailureResponse(response.MessageEn, response.MessageAr);

        return ApiResponse<List<PermissionDto>>.SuccessResponse(response.Data!.EffectivePermissions, "Permissions retrieved successfully.", "تم استرجاع الصلاحيات بنجاح.");
    }

    public async Task<ApiResponse<UserDetailsDto>> UpdateUserStatusAsync(int currentUserId, int userId, UpdateUserStatusDto dto, CancellationToken cancellationToken = default)
    {
        if (currentUserId == userId)
            return ApiResponse<UserDetailsDto>.FailureResponse("You cannot change your own account status.", "لا يمكنك تغيير حالة حسابك الخاص.");

        var currentUser = await _unitOfWork.Users.GetWithPermissionsAsync(currentUserId, cancellationToken);
        if (currentUser == null)
            return ApiResponse<UserDetailsDto>.FailureResponse("Current user was not found.", "المستخدم الحالي غير موجود.");

        if (!currentUser.IsActive)
            return ApiResponse<UserDetailsDto>.FailureResponse("Current user is inactive.", "المستخدم الحالي غير فعّال.");

        if (!GetEffectivePermissionIds(currentUser).Overlaps(new[] { ManageUsersPermissionId, ManageAssociationUsersPermissionId, ManageProductionCompanyUsersPermissionId }))
            return ApiResponse<UserDetailsDto>.FailureResponse("Caller does not hold the Manage Users permission.", "لا تملك صلاحية إدارة المستخدمين.");

        var (user, scopeError) = await GetScopedUserAsync<UserDetailsDto>(userId, currentUser, cancellationToken);
        if (scopeError != null) return scopeError;

        user!.IsActive = dto.IsActive;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetUserDetailsAsync(currentUserId, userId, cancellationToken);
    }

    #region Helpers (Refactored to No-Exceptions)

    private async Task<(User? User, ApiResponse<T>? Error)> GetCurrentUserAsync<T>(int currentUserId, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetDetailsAsync(currentUserId, cancellationToken);
        if (user == null)
            return (null, ApiResponse<T>.FailureResponse("Current user was not found.", "المستخدم الحالي غير موجود."));

        if (!user.IsActive)
            return (null, ApiResponse<T>.FailureResponse("Current user is inactive.", "المستخدم الحالي غير فعّال."));

        return (user, null);
    }

    private static HashSet<int> GetCallerDelegatablePermissionIds(User caller, Role targetRole)
    {
        if (caller.RoleId != targetRole.RoleId) return new HashSet<int>();

        var directIds = caller.UserPermissions
            .Where(up => up.IsActive && up.Permission.IsActive)
            .Select(up => up.PermissionId);

        var groupIds = caller.UserGroups
            .Where(ug => ug.Group.IsActive)
            .SelectMany(ug => ug.Group.GroupPermissions)
            .Where(gp => gp.IsActive && gp.Permission.IsActive)
            .Select(gp => gp.PermissionId);

        return directIds.Concat(groupIds).ToHashSet();
    }

    private const int ViewUsersPermissionId = 1;
    private const int ManageUsersPermissionId = 3;
    private const int ManageAssociationUsersPermissionId = 39;
    private const int ManageProductionCompanyUsersPermissionId = 40;
    private const int BaytAlUrdonRoleId = 1;
    private const int AsezaRoleId = 2;
    private const int ProductionHouseRoleId = 3;
    private const int AssociationRoleId = 4;

    private async Task<(EntityType? EntityType, int? EntityId, ApiResponse<T>? Error)> ResolveEntityForRoleAsync<T>(
      int roleId, int? requestedEntityId, CancellationToken cancellationToken)
    {
        if (roleId == SystemRoles.Association && requestedEntityId.HasValue)
        {
            var association = await _unitOfWork.Associations.GetByIdAsync(requestedEntityId.Value, cancellationToken);
            if (association == null)
                return (null, null, ApiResponse<T>.FailureResponse("Association was not found.", "الجمعية غير موجودة."));

            return (EntityType.Association, association.Id, null);
        }

        if (roleId == SystemRoles.Producer && requestedEntityId.HasValue)
            return (EntityType.ProductionCompany, requestedEntityId.Value, null);

        var result = roleId switch
        {
            SystemRoles.BaytAlUrdon => (EntityType.BaytAlUrdon, 1),
            SystemRoles.Aseza => (EntityType.Aseza, 1),
            SystemRoles.ProductionHouse => (EntityType.ProductionCompany, 1),
            SystemRoles.Association => (EntityType.Association, 1),
            SystemRoles.Producer => (EntityType.ProductionCompany, requestedEntityId ?? 1),
            SystemRoles.LocationManager => (EntityType.ProductionCompany, requestedEntityId ?? 1),
            SystemRoles.WorkerManager => (EntityType.Association, requestedEntityId ?? 1),
            SystemRoles.Worker => (EntityType.Association, requestedEntityId ?? 1),
            SystemRoles.AttendanceOfficer => (EntityType.Association, requestedEntityId ?? 1),
            _ => ((EntityType, int)?)null
        };

        if (result == null)
            return (null, null, ApiResponse<T>.FailureResponse("Role was not found.", "الدور غير موجود."));

        return (result.Value.Item1, result.Value.Item2, null);
    }

    private static (EntityType? EntityType, int? EntityId, ApiResponse<T>? Error) ResolveTargetEntity<T>(User currentUser, EntityType? entityType, int? entityId)
    {
        if (entityType is null && entityId is null)
            return (currentUser.EntityType, currentUser.EntityId, null);

        if (entityType is null || entityId is null)
            return (null, null, ApiResponse<T>.FailureResponse("entityType and entityId must both be provided together.", "يجب إرسال نوع الكيان ورقمه معاً."));

        var isOwnEntity = entityType == currentUser.EntityType && entityId == currentUser.EntityId;
        if (!isOwnEntity && !CanViewOtherEntities(currentUser))
            return (null, null, ApiResponse<T>.FailureResponse("Cannot view users outside your own entity.", "لا يمكنك عرض مستخدمين خارج كيانك."));

        return (entityType.Value, entityId.Value, null);
    }

    private static HashSet<int> GetEffectivePermissionIds(User user)
    {
        var rolePermissionIds = user.Role.RolePermissions
            .Where(rp => rp.IsActive && rp.Permission.IsActive)
            .Select(rp => rp.PermissionId);

        var directPermissionIds = user.UserPermissions
            .Where(up => up.IsActive && up.Permission.IsActive)
            .Select(up => up.PermissionId);

        var groupPermissionIds = user.UserGroups
            .Where(ug => ug.Group.IsActive)
            .SelectMany(ug => ug.Group.GroupPermissions)
            .Where(gp => gp.IsActive && gp.Permission.IsActive)
            .Select(gp => gp.PermissionId);

        return rolePermissionIds.Concat(directPermissionIds).Concat(groupPermissionIds).ToHashSet();
    }

    private async Task<(User? User, ApiResponse<T>? Error)> GetScopedUserAsync<T>(int userId, User currentUser, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetDetailsAsync(userId, cancellationToken);
        if (user == null)
            return (null, ApiResponse<T>.FailureResponse("User was not found.", "المستخدم غير موجود."));

        if (currentUser.RoleId != 1 && (user.EntityType != currentUser.EntityType || user.EntityId != currentUser.EntityId))
            return (null, ApiResponse<T>.FailureResponse("Cannot manage users outside the current entity.", "لا يمكنك إدارة مستخدمين خارج كيانك."));

        return (user, null);
    }

    private async Task<(User? User, ApiResponse<T>? Error)> GetViewableUserAsync<T>(int userId, User currentUser, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetDetailsAsync(userId, cancellationToken);
        if (user == null)
            return (null, ApiResponse<T>.FailureResponse("User was not found.", "المستخدم غير موجود."));

        var isOwnEntity = user.EntityType == currentUser.EntityType && user.EntityId == currentUser.EntityId;
        if (!isOwnEntity && !CanViewOtherEntities(currentUser))
            return (null, ApiResponse<T>.FailureResponse("Cannot view users outside your own entity.", "لا يمكنك عرض مستخدمين خارج كيانك."));

        return (user, null);
    }

    private static bool CanViewOtherEntities(User currentUser) =>
        currentUser.EntityType is EntityType.BaytAlUrdon or EntityType.Aseza;

    private async Task<(Group? Group, ApiResponse<T>? Error)> GetScopedGroupAsync<T>(int groupId, User currentUser, CancellationToken cancellationToken)
    {
        var group = await _unitOfWork.Groups.GetDetailsAsync(groupId, cancellationToken);
        if (group == null)
            return (null, ApiResponse<T>.FailureResponse("Group was not found.", "المجموعة غير موجودة."));

        if (group.EntityType != currentUser.EntityType || group.EntityId != currentUser.EntityId)
            return (null, ApiResponse<T>.FailureResponse("Cannot manage groups outside the current entity.", "لا يمكنك إدارة مجموعات خارج كيانك."));

        return (group, null);
    }

    private async Task<ApiResponse<T>?> EnsurePermissionsExistAsync<T>(List<int> permissionIds, CancellationToken cancellationToken)
    {
        if (permissionIds.Count == 0) return null;

        var permissions = await _unitOfWork.Permissions.GetByIdsAsync(permissionIds, cancellationToken);
        if (permissions.Count != permissionIds.Count)
            return ApiResponse<T>.FailureResponse("One or more permissions were not found.", "إحدى الصلاحيات غير موجودة.");

        return null;
    }

    private async Task<ApiResponse<T>?> EnsurePermissionsAllowedForRoleAsync<T>(User caller, int roleId, List<int> permissionIds, CancellationToken cancellationToken)
    {
        if (permissionIds.Count == 0) return null;

        // التعديل هون: أدمن بيت الأردن مسموح له يعطي أي صلاحية لأي يوزر حتى لو خارج نطاق دوره
        if (caller.RoleId == BaytAlUrdonRoleId) return null;

        var role = await _unitOfWork.Roles.GetWithPermissionsAsync(roleId, cancellationToken);
        if (role == null)
            return ApiResponse<T>.FailureResponse("Role was not found.", "الدور غير موجود.");

        return EnsurePermissionsAllowedForRole<T>(role, permissionIds, GetCallerDelegatablePermissionIds(caller, role));
    }

    private static ApiResponse<T>? EnsurePermissionsAllowedForRole<T>(Role role, List<int> permissionIds, HashSet<int>? callerDelegatableIds = null)
    {
        if (permissionIds.Count == 0) return null;

        var allowedPermissionIds = role.RolePermissions
            .Where(rp => rp.IsActive && rp.Permission.IsActive)
            .Select(rp => rp.PermissionId)
            .ToHashSet();

        if (callerDelegatableIds is not null)
            allowedPermissionIds.UnionWith(callerDelegatableIds);

        if (permissionIds.Any(permissionId => !allowedPermissionIds.Contains(permissionId)))
            return ApiResponse<T>.FailureResponse("One or more permissions are not available for the selected role.", "بعض الصلاحيات غير متاحة لهذا الدور.");

        return null;
    }

    private async Task<(List<Group>? Groups, ApiResponse<T>? Error)> EnsureGroupsInEntityAsync<T>(List<int> groupIds, EntityType entityType, int entityId, CancellationToken cancellationToken)
    {
        if (groupIds.Count == 0) return (new List<Group>(), null);

        var groups = await _unitOfWork.Groups.GetByIdsInEntityAsync(groupIds, entityType, entityId, cancellationToken);
        if (groups.Count != groupIds.Count)
            return (null, ApiResponse<T>.FailureResponse("One or more groups were not found in the current entity.", "إحدى المجموعات غير موجودة في كيانك."));

        return (groups, null);
    }

    private async Task<(List<User>? Users, ApiResponse<T>? Error)> EnsureUsersInEntityAsync<T>(List<int> userIds, User currentUser, CancellationToken cancellationToken)
    {
        if (userIds.Count == 0) return (new List<User>(), null);

        var users = await _unitOfWork.Users.GetByIdsInEntityAsync(userIds, currentUser.EntityType, currentUser.EntityId, cancellationToken);
        if (users.Count != userIds.Count)
            return (null, ApiResponse<T>.FailureResponse("One or more users were not found in the current entity.", "أحد المستخدمين غير موجود في كيانك."));

        return (users, null);
    }

    private static ApiResponse<T>? EnsureEntityCreationAllowed<T>(User currentUser, Role role)
    {
        if (currentUser.RoleId == BaytAlUrdonRoleId) return null;
        if (role.RoleId == currentUser.RoleId) return null;
        if (role.TargetEntityType.HasValue && role.TargetEntityType == currentUser.EntityType) return null;

        return ApiResponse<T>.FailureResponse("Cannot create a user with a role outside your entity type.", "لا يمكنك إنشاء مستخدم خارج نطاق كيانك.");
    }

    private static ApiResponse<T>? ValidateUserPayload<T>(CreateEntityUserDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.FirstNameEn) || string.IsNullOrWhiteSpace(dto.LastNameEn) ||
            string.IsNullOrWhiteSpace(dto.FirstNameAr) || string.IsNullOrWhiteSpace(dto.LastNameAr) ||
            string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.PhoneNumber) ||
            string.IsNullOrWhiteSpace(dto.InitialPassword))
        {
            return ApiResponse<T>.FailureResponse("User mandatory fields are required.", "جميع الحقول الأساسية مطلوبة.");
        }
        return null;
    }

    private static ApiResponse<T>? ValidateGroupPayload<T>(string groupNameEn, string groupNameAr)
    {
        if (string.IsNullOrWhiteSpace(groupNameEn) || string.IsNullOrWhiteSpace(groupNameAr))
            return ApiResponse<T>.FailureResponse("Group English and Arabic names are required.", "الاسم الإنجليزي والعربي للمجموعة مطلوبان.");
        return null;
    }

    private static List<int> NormalizeIds(IEnumerable<int> ids) =>
        ids.Where(id => id > 0).Distinct().OrderBy(id => id).ToList();

    private static void SyncUserPermissions(User user, List<int> permissionIds)
    {
        var requestedIds = permissionIds.ToHashSet();
        var existingIds = user.UserPermissions.Select(up => up.PermissionId).ToHashSet();

        foreach (var userPermission in user.UserPermissions.Where(up => !requestedIds.Contains(up.PermissionId)).ToList())
            user.UserPermissions.Remove(userPermission);

        foreach (var permissionId in requestedIds.Except(existingIds))
        {
            user.UserPermissions.Add(new UserPermission { UserId = user.UserId, PermissionId = permissionId, IsActive = true });
        }
    }

    private static void SyncUserGroups(User user, List<int> groupIds)
    {
        var requestedIds = groupIds.ToHashSet();
        var existingIds = user.UserGroups.Select(ug => ug.GroupId).ToHashSet();

        foreach (var userGroup in user.UserGroups.Where(ug => !requestedIds.Contains(ug.GroupId)).ToList())
            user.UserGroups.Remove(userGroup);

        foreach (var groupId in requestedIds.Except(existingIds))
            user.UserGroups.Add(new UserGroup { UserId = user.UserId, GroupId = groupId });
    }

    private static void SyncGroupPermissions(Group group, List<int> permissionIds)
    {
        var requestedIds = permissionIds.ToHashSet();
        var existingIds = group.GroupPermissions.Select(gp => gp.PermissionId).ToHashSet();

        foreach (var groupPermission in group.GroupPermissions.Where(gp => !requestedIds.Contains(gp.PermissionId)).ToList())
            group.GroupPermissions.Remove(groupPermission);

        foreach (var permissionId in requestedIds.Except(existingIds))
            group.GroupPermissions.Add(new GroupPermission { GroupId = group.GroupId, PermissionId = permissionId, IsActive = true });
    }

    private static void SyncGroupUsers(Group group, List<int> userIds)
    {
        var requestedIds = userIds.ToHashSet();
        var existingIds = group.UserGroups.Select(ug => ug.UserId).ToHashSet();

        foreach (var userGroup in group.UserGroups.Where(ug => !requestedIds.Contains(ug.UserId)).ToList())
            group.UserGroups.Remove(userGroup);

        foreach (var userId in requestedIds.Except(existingIds))
            group.UserGroups.Add(new UserGroup { UserId = userId, GroupId = group.GroupId });
    }

    internal static UserSummaryDto MapUserSummary(User user) =>
        new(user.UserId, user.FirstNameEn, user.LastNameEn, user.FirstNameAr, user.LastNameAr, user.Email, user.PhoneNumber, user.RoleId, user.Role.RoleNameEn, user.Role.RoleNameAr, user.MustResetPassword, user.IsActive);

    private static UserDetailsDto MapUserDetails(User user)
    {
        var directPermissions = user.UserPermissions.Where(up => up.IsActive && up.Permission.IsActive)
            .Select(up => up.Permission).DistinctBy(p => p.PermissionId).Select(MapPermission).OrderBy(p => p.Module).ThenBy(p => p.PermissionNameEn).ToList();

        var groupPermissions = user.UserGroups.Select(ug => ug.Group).Where(g => g.IsActive)
            .SelectMany(g => g.GroupPermissions).Where(gp => gp.IsActive && gp.Permission.IsActive).Select(gp => gp.Permission);

        var effectivePermissions = user.UserPermissions.Where(up => up.IsActive && up.Permission.IsActive).Select(up => up.Permission)
            .Concat(groupPermissions).DistinctBy(p => p.PermissionId).Select(MapPermission).OrderBy(p => p.Module).ThenBy(p => p.PermissionNameEn).ToList();

        return new UserDetailsDto(user.UserId, user.FirstNameEn, user.LastNameEn, user.FirstNameAr, user.LastNameAr, user.Email, user.PhoneNumber,
            user.RoleId, user.Role.RoleNameEn, user.Role.RoleNameAr, user.EntityType, user.EntityId, user.MustResetPassword, user.IsActive,
            directPermissions, user.UserGroups.Select(ug => MapGroupSummary(ug.Group)).ToList(), effectivePermissions);
    }

    private const int GroupPermissionPreviewSize = 4;

    private static GroupSummaryDto MapGroupSummary(Group group)
    {
        var activePermissions = group.GroupPermissions.Where(gp => gp.IsActive && gp.Permission.IsActive)
            .Select(gp => gp.Permission).OrderBy(p => p.Module).ThenBy(p => p.PermissionNameEn).ToList();

        return new(group.GroupId, group.GroupNameEn, group.GroupNameAr, activePermissions.Count, group.UserGroups.Count,
            activePermissions.Take(GroupPermissionPreviewSize).Select(MapPermission).ToList());
    }

    private static GroupDetailsDto MapGroupDetails(Group group) =>
        new(group.GroupId, group.GroupNameEn, group.GroupNameAr, group.EntityType, group.EntityId,
            group.GroupPermissions.Where(gp => gp.IsActive && gp.Permission.IsActive).Select(gp => MapPermission(gp.Permission)).OrderBy(p => p.Module).ThenBy(p => p.PermissionNameEn).ToList(),
            group.UserGroups.Select(ug => MapUserSummary(ug.User)).OrderBy(u => u.FirstNameEn).ThenBy(u => u.LastNameEn).ToList());

    private static PermissionDto MapPermission(Permission permission) =>
        new(permission.PermissionId, permission.PermissionNameEn, permission.PermissionNameAr, permission.Module);

    #endregion
}