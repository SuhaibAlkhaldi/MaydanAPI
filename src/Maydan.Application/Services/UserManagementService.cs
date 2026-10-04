using Maydan.Application.DTOs.UserManagement;
using Maydan.Application.Interfaces;
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

    public async Task<List<UserSummaryDto>> GetUsersAsync(int currentUserId, string? search, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(currentUserId, cancellationToken);
        var users = await _unitOfWork.Users.GetByEntityAsync(currentUser.EntityType, currentUser.EntityId, search, cancellationToken);

        return users.Select(MapUserSummary).ToList();
    }

    // MAYD-20: real Users List page — search/status/pagination, entity-scoped per the real
    // Business Rule (MAYD-1): Bayt-AlUrdon and ASEZA may pass entityType/entityId to view a
    // DIFFERENT entity's users (ASEZA's is explicitly read-only by the BR — naturally true here
    // since this method only ever reads, never mutates); every other role may only view its own
    // entity and gets rejected for anything else. Matches PermissionSeedConfiguration.cs's real
    // ViewUsers(1)/ManageUsers(3) — same permission pair users.routes.ts's own roleGuard already
    // gates this page on — not a new ad hoc role check.
    public async Task<PagedUsersDto> GetUsersPagedAsync(
        int currentUserId, string? search, bool? isActive, int page, int pageSize,
        EntityType? entityType, int? entityId, CancellationToken cancellationToken = default)
    {
        var currentUser = await _unitOfWork.Users.GetWithPermissionsAsync(currentUserId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Current user was not found.");

        if (!currentUser.IsActive)
        {
            throw new UnauthorizedAccessException("Current user is inactive.");
        }

        if (!GetEffectivePermissionIds(currentUser).Overlaps(new[] { ViewUsersPermissionId, ManageUsersPermissionId }))
        {
            throw new UnauthorizedAccessException("Caller does not hold the View Users permission.");
        }

        var (targetEntityType, targetEntityId) = ResolveTargetEntity(currentUser, entityType, entityId);

        var safePage = page < 1 ? 1 : page;
        var safePageSize = pageSize is < 1 or > 100 ? 10 : pageSize;

        var (users, totalCount) = await _unitOfWork.Users.GetPagedByEntityAsync(
            targetEntityType, targetEntityId, search, isActive, safePage, safePageSize, cancellationToken);

        return new PagedUsersDto(users.Select(MapUserSummary).ToList(), totalCount, safePage, safePageSize);
    }

    public async Task<UserDetailsDto> GetUserDetailsAsync(int currentUserId, int userId, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(currentUserId, cancellationToken);
        var user = await GetViewableUserAsync(userId, currentUser, cancellationToken);

        return MapUserDetails(user);
    }

    public async Task<UserDetailsDto> CreateUserAsync(int currentUserId, CreateEntityUserDto dto, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(currentUserId, cancellationToken);
        ValidateUserPayload(dto);
        EnsureSameEntityCreation(currentUser, dto.RoleId);

        var email = dto.Email.Trim();
        if (await _unitOfWork.Users.EmailExistsAsync(email, cancellationToken))
        {
            throw new InvalidOperationException("User email already exists.");
        }

        var role = await _unitOfWork.Roles.GetWithPermissionsAsync(dto.RoleId, cancellationToken)
            ?? throw new KeyNotFoundException("Role was not found.");

        var permissionIds = NormalizeIds(dto.PermissionIds);
        await EnsurePermissionsExistAsync(permissionIds, cancellationToken);
        EnsurePermissionsAllowedForRole(role, permissionIds, GetCallerDelegatablePermissionIds(currentUser, role));

        // Same role as the creator (the only case every role but Bayt-AlUrdon can ever reach, per
        // EnsureSameEntityCreation above) keeps the exact prior behavior: the new user belongs to the
        // creator's own entity. Only Bayt-AlUrdon creating a DIFFERENT role reaches the resolved
        // branch — see ResolveEntityForRoleAsync's own comment.
        var (targetEntityType, targetEntityId) = role.RoleId == currentUser.RoleId
            ? (currentUser.EntityType, currentUser.EntityId)
            : await ResolveEntityForRoleAsync(role.RoleId, dto.EntityId, cancellationToken);

        var groupIds = NormalizeIds(dto.GroupIds);
        var groups = await EnsureGroupsInEntityAsync(groupIds, targetEntityType, targetEntityId, cancellationToken);

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
            EntityType = targetEntityType,
            EntityId = targetEntityId
        };

        foreach (var permissionId in permissionIds)
        {
            user.UserPermissions.Add(new UserPermission { PermissionId = permissionId, IsActive = true });
        }

        foreach (var group in groups)
        {
            user.UserGroups.Add(new UserGroup { GroupId = group.GroupId });
        }

        await _unitOfWork.Users.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var createdUser = await _unitOfWork.Users.GetDetailsReadOnlyAsync(user.UserId, cancellationToken)
            ?? throw new KeyNotFoundException("Created user was not found.");

        return MapUserDetails(createdUser);
    }

    public async Task<List<PermissionDto>> GetAvailablePermissionsAsync(int currentUserId, int? roleId, CancellationToken cancellationToken = default)
    {
        var caller = await GetCurrentUserAsync(currentUserId, cancellationToken);

        if (!roleId.HasValue)
        {
            var allPermissions = await _unitOfWork.Permissions.GetAllAsync(cancellationToken);
            return allPermissions.Select(MapPermission).ToList();
        }

        var role = await _unitOfWork.Roles.GetWithPermissionsAsync(roleId.Value, cancellationToken)
            ?? throw new KeyNotFoundException("Role was not found.");

        // MAYD-37: the Permission Picker's own catalog must offer exactly what
        // EnsurePermissionsAllowedForRole will actually accept on save — role's own catalog PLUS
        // whatever the caller can personally delegate (see GetCallerDelegatablePermissionIds's own
        // comment; empty unless caller and target share the same role, e.g. the ASEZA admin editing
        // a fellow ASEZA user). Sourced directly from the caller's already-loaded UserPermissions/
        // GroupPermissions navigation rather than a second DB round-trip.
        var delegatableIds = GetCallerDelegatablePermissionIds(caller, role);
        var delegatablePermissions = caller.UserPermissions
            .Where(up => up.IsActive && up.Permission.IsActive && delegatableIds.Contains(up.PermissionId))
            .Select(up => up.Permission)
            .Concat(caller.UserGroups
                .Where(ug => ug.Group.IsActive)
                .SelectMany(ug => ug.Group.GroupPermissions)
                .Where(gp => gp.IsActive && gp.Permission.IsActive && delegatableIds.Contains(gp.PermissionId))
                .Select(gp => gp.Permission));

        return role.RolePermissions
            .Where(rp => rp.IsActive && rp.Permission.IsActive)
            .Select(rp => rp.Permission)
            .Concat(delegatablePermissions)
            .DistinctBy(p => p.PermissionId)
            .Select(MapPermission)
            .OrderBy(p => p.Module)
            .ThenBy(p => p.PermissionNameEn)
            .ToList();
    }

    public async Task<PermissionMatrixDto> GetPermissionMatrixAsync(int currentUserId, CancellationToken cancellationToken = default)
    {
        await GetCurrentUserAsync(currentUserId, cancellationToken);

        var permissions = await _unitOfWork.Permissions.GetAllAsync(cancellationToken);
        var roles = await _unitOfWork.Roles.GetAllWithPermissionsAsync(cancellationToken);

        return new PermissionMatrixDto(
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
    }

    public async Task<List<GroupSummaryDto>> GetGroupsAsync(int currentUserId, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(currentUserId, cancellationToken);
        var groups = await _unitOfWork.Groups.GetByEntityAsync(currentUser.EntityType, currentUser.EntityId, cancellationToken);

        return groups.Select(MapGroupSummary).ToList();
    }

    public async Task<GroupDetailsDto> GetGroupAsync(int currentUserId, int groupId, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(currentUserId, cancellationToken);
        var group = await GetScopedGroupAsync(groupId, currentUser, cancellationToken);

        return MapGroupDetails(group);
    }

    public async Task<GroupDetailsDto> CreateGroupAsync(int currentUserId, CreateGroupDto dto, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(currentUserId, cancellationToken);
        ValidateGroupPayload(dto.GroupNameEn, dto.GroupNameAr);

        if (await _unitOfWork.Groups.NameExistsInEntityAsync(currentUser.EntityType, currentUser.EntityId, dto.GroupNameEn, dto.GroupNameAr, null, cancellationToken))
        {
            throw new InvalidOperationException("Group English or Arabic name already exists in this entity.");
        }

        var permissionIds = NormalizeIds(dto.PermissionIds);
        await EnsurePermissionsExistAsync(permissionIds, cancellationToken);

        var userIds = NormalizeIds(dto.UserIds);
        var users = await EnsureUsersInEntityAsync(userIds, currentUser, cancellationToken);

        var group = new Group
        {
            GroupNameEn = dto.GroupNameEn.Trim(),
            GroupNameAr = dto.GroupNameAr.Trim(),
            EntityType = currentUser.EntityType,
            EntityId = currentUser.EntityId,
            IsActive = true
        };

        foreach (var permissionId in permissionIds)
        {
            group.GroupPermissions.Add(new GroupPermission { PermissionId = permissionId, IsActive = true });
        }

        foreach (var user in users)
        {
            group.UserGroups.Add(new UserGroup { UserId = user.UserId });
        }

        await _unitOfWork.Groups.AddAsync(group, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetGroupAsync(currentUserId, group.GroupId, cancellationToken);
    }

    public async Task<GroupDetailsDto> UpdateGroupAsync(int currentUserId, int groupId, UpdateGroupDto dto, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(currentUserId, cancellationToken);
        var group = await GetScopedGroupAsync(groupId, currentUser, cancellationToken);
        ValidateGroupPayload(dto.GroupNameEn, dto.GroupNameAr);

        if (await _unitOfWork.Groups.NameExistsInEntityAsync(currentUser.EntityType, currentUser.EntityId, dto.GroupNameEn, dto.GroupNameAr, groupId, cancellationToken))
        {
            throw new InvalidOperationException("Group English or Arabic name already exists in this entity.");
        }

        var permissionIds = NormalizeIds(dto.PermissionIds);
        await EnsurePermissionsExistAsync(permissionIds, cancellationToken);

        var userIds = NormalizeIds(dto.UserIds);
        var users = await EnsureUsersInEntityAsync(userIds, currentUser, cancellationToken);

        group.GroupNameEn = dto.GroupNameEn.Trim();
        group.GroupNameAr = dto.GroupNameAr.Trim();
        SyncGroupPermissions(group, permissionIds);
        SyncGroupUsers(group, users.Select(u => u.UserId).ToList());

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetGroupAsync(currentUserId, groupId, cancellationToken);
    }

    public async Task DeleteGroupAsync(int currentUserId, int groupId, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(currentUserId, cancellationToken);
        var group = await GetScopedGroupAsync(groupId, currentUser, cancellationToken);

        var permissionIds = group.GroupPermissions
            .Where(gp => gp.IsActive && gp.Permission.IsActive)
            .Select(gp => gp.PermissionId)
            .Distinct()
            .ToList();

        foreach (var userGroup in group.UserGroups)
        {
            var existingDirectPermissionIds = userGroup.User.UserPermissions
                .Where(up => up.IsActive)
                .Select(up => up.PermissionId)
                .ToHashSet();

            foreach (var permissionId in permissionIds)
            {
                if (!existingDirectPermissionIds.Contains(permissionId))
                {
                    userGroup.User.UserPermissions.Add(new UserPermission
                    {
                        UserId = userGroup.UserId,
                        PermissionId = permissionId,
                        IsActive = true
                    });
                }
            }
        }

        _unitOfWork.Groups.Remove(group);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<UserDetailsDto> UpdateDirectPermissionsAsync(int currentUserId, int userId, UpdateUserPermissionsDto dto, CancellationToken cancellationToken = default)
    {
        // MAYD-34 verification gap fix (2026-09-24): UserManagementModule.md Phase 5.4 ("Prevent
        // Self Permission Modification") is a real, documented business rule — a User cannot modify
        // their own permissions even while holding AssignPermissions/Manage Users, and this must be
        // rejected here, not just hidden behind a disabled button (same "check the service layer,
        // not just the route guard" precedent as GetScopedUserAsync's entity check below). No
        // equivalent rule exists for groups (Phase 5.4 is titled "Self PERMISSION Modification" and
        // Part 10's own test checklist only lists it for permissions) — UpdateGroupsAsync is
        // deliberately left untouched.
        if (currentUserId == userId)
        {
            throw new UnauthorizedAccessException("You cannot modify your own permissions.");
        }

        var currentUser = await GetCurrentUserAsync(currentUserId, cancellationToken);
        var user = await GetScopedUserAsync(userId, currentUser, cancellationToken);

        var permissionIds = NormalizeIds(dto.PermissionIds);
        await EnsurePermissionsExistAsync(permissionIds, cancellationToken);
        await EnsurePermissionsAllowedForRoleAsync(currentUser, user.RoleId, permissionIds, cancellationToken);

        SyncUserPermissions(user, permissionIds);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetUserDetailsAsync(currentUserId, userId, cancellationToken);
    }

    public async Task<UserDetailsDto> UpdateGroupsAsync(int currentUserId, int userId, UpdateUserGroupsDto dto, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(currentUserId, cancellationToken);
        var user = await GetScopedUserAsync(userId, currentUser, cancellationToken);

        var groupIds = NormalizeIds(dto.GroupIds);
        await EnsureGroupsInEntityAsync(groupIds, currentUser.EntityType, currentUser.EntityId, cancellationToken);

        SyncUserGroups(user, groupIds);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetUserDetailsAsync(currentUserId, userId, cancellationToken);
    }

    public async Task<List<PermissionDto>> GetEffectivePermissionsAsync(int currentUserId, int userId, CancellationToken cancellationToken = default)
    {
        var user = await GetUserDetailsAsync(currentUserId, userId, cancellationToken);

        return user.EffectivePermissions;
    }

    // Activate/Deactivate User (UserManagementModule.md Phase 4.3): "Avoid hard delete unless
    // explicitly required by the business" — this only ever flips IsActive, never touches
    // IsDeleted/DeletedAt. No MAYD subtask covers this; built from that doc plus the two closest
    // real precedents already in this class:
    //   - Self-check FIRST, before any repository round trip, mirroring
    //     UpdateDirectPermissionsAsync's own "cannot modify your own permissions" rule (Phase 5.4) —
    //     a user cannot deactivate (or reactivate) their own account through this endpoint either,
    //     rejected here regardless of dto.IsActive's direction, not just for the downgrade case,
    //     since a deactivated caller could never reach this endpoint to reactivate themselves anyway.
    //   - Strict same-entity scoping via GetScopedUserAsync — the exact same write-path rule
    //     UpdateDirectPermissionsAsync/UpdateGroupsAsync already use, deliberately NOT given the
    //     Bayt-AlUrdon/ASEZA cross-entity VIEW override (see that method's own comment on why writes
    //     stay strict).
    // New here: an explicit Manage Users (PermissionId 3) gate. Every other write path in this class
    // only relies on [Authorize] + entity-scoping with no permission-bit check of its own — but this
    // ticket asked specifically for one, and ManageUsersPermissionId's one existing real usage
    // (GetUsersPagedAsync's read gate) is the only established pattern for checking it, so that's
    // what this reuses rather than inventing a different shape. GetWithPermissionsAsync (not
    // GetCurrentUserAsync's GetDetailsAsync) is required here for the same reason
    // GetUsersPagedAsync/RefreshTokenAsync already use it: GetEffectivePermissionIds reads
    // user.Role.RolePermissions, which GetDetailsAsync never loads.
    public async Task<UserDetailsDto> UpdateUserStatusAsync(int currentUserId, int userId, UpdateUserStatusDto dto, CancellationToken cancellationToken = default)
    {
        if (currentUserId == userId)
        {
            throw new UnauthorizedAccessException("You cannot change your own account status.");
        }

        var currentUser = await _unitOfWork.Users.GetWithPermissionsAsync(currentUserId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Current user was not found.");

        if (!currentUser.IsActive)
        {
            throw new UnauthorizedAccessException("Current user is inactive.");
        }

        // ManageUsersPermissionId (Bayt-AlUrdon/ASEZA today, though GetScopedUserAsync below still
        // confines them to their OWN entity — neither holds an Association's or ProductionCompany's
        // EntityType) OR the narrower ManageAssociationUsersPermissionId (the Association role's own
        // self-service grant) OR ManageProductionCompanyUsersPermissionId (the ProductionHouse role's
        // own self-service grant, same shape). Deliberately NOT extended on
        // UpdateDirectPermissionsAsync/UpdateGroupsAsync — this permission only ever covers
        // activate/deactivate.
        if (!GetEffectivePermissionIds(currentUser).Overlaps(new[] { ManageUsersPermissionId, ManageAssociationUsersPermissionId, ManageProductionCompanyUsersPermissionId }))
        {
            throw new UnauthorizedAccessException("Caller does not hold the Manage Users permission.");
        }

        var user = await GetScopedUserAsync(userId, currentUser, cancellationToken);

        user.IsActive = dto.IsActive;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetUserDetailsAsync(currentUserId, userId, cancellationToken);
    }

    // MAYD-37 (2026-09-24): now fetches via GetDetailsAsync instead of the bare GetByIdAsync, so
    // UserPermissions/UserGroups(.Group.GroupPermissions) are loaded for every caller of this
    // method — needed by GetCallerDelegatablePermissionIds below (CreateUserAsync,
    // UpdateDirectPermissionsAsync, GetAvailablePermissionsAsync all call this for the CALLER, not
    // just the target). Confirmed safe for every existing caller: every one of them only ever reads
    // currentUser's scalar fields (EntityType/EntityId/RoleId) or passes it to
    // GetEffectivePermissionIds — never relies on Role.RolePermissions being loaded from this
    // specific fetch (GetDetailsAsync doesn't include that; GetUsersPagedAsync's own
    // GetEffectivePermissionIds(currentUser) call uses a separate GetWithPermissionsAsync fetch that
    // does), and none of them mutate-and-save currentUser (always read-only here), so loading extra
    // navigation properties changes nothing about existing behavior.
    private async Task<User> GetCurrentUserAsync(int currentUserId, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetDetailsAsync(currentUserId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Current user was not found.");

        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("Current user is inactive.");
        }

        return user;
    }

    // MAYD-37 (2026-09-24, product-owner-confirmed): the necessary completion of "ASEZA Admin has
    // all view permissions by default; a regular ASEZA user gets a custom subset assigned by the
    // Admin, reusing the same Groups and Permissions pattern as MAYD-2" — confirmed against the real
    // code (not assumed) that this doesn't work without it. The ASEZA admin's new page-view
    // permissions are granted as individual UserPermissions, NOT via ASEZA's own RolePermissions
    // (see RolePermissionSeedConfiguration.cs's own comment on why a role-level grant would defeat
    // "custom subset" entirely). But EnsurePermissionsAllowedForRole below only ever allowed
    // assigning a permission that's in the TARGET user's OWN ROLE's RolePermissions catalog (Phase
    // 5.5, "Prevent Permission Escalation" — UserManagementModule.md) — meaning, unmodified, the
    // admin could never actually hand these permissions to a fellow ASEZA user via the existing,
    // already-verified User Details "Edit Permissions" flow (MAYD-34): every attempt would 400 with
    // "not available for the selected role", and the Permission Picker wouldn't even offer them
    // (GetAvailablePermissionsAsync's roleId branch has the exact same role-catalog-only shape).
    //
    // Resolution: a caller may additionally delegate any permission they PERSONALLY hold (direct or
    // via their own group), but ONLY when assigning to a user of their OWN SAME role — this is
    // strictly a widening, never a narrowing (nothing granted that the caller doesn't already
    // themselves hold), and the same-role restriction means it can never fire for a cross-role case
    // like Bayt-AlUrdon creating an Association user (MAYD-1) — that path is completely unaffected,
    // confirmed by this method returning an empty set whenever roles differ. Applied to
    // CreateUserAsync, UpdateDirectPermissionsAsync, and GetAvailablePermissionsAsync alike, so the
    // Permission Picker's displayed catalog and the actual backend validation never disagree.
    private static HashSet<int> GetCallerDelegatablePermissionIds(User caller, Role targetRole)
    {
        if (caller.RoleId != targetRole.RoleId)
        {
            return new HashSet<int>();
        }

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

    // Matches PermissionSeedConfiguration.cs ids 1/3.
    private const int ViewUsersPermissionId = 1;
    private const int ManageUsersPermissionId = 3;

    // Matches PermissionSeedConfiguration.cs id 39 — the narrow, self-service activate/deactivate
    // grant for the Association role (see that seed's own comment). Only ever OR'd alongside
    // ManageUsersPermissionId in UpdateUserStatusAsync below, same "specific permission OR the
    // broader one" shape AssociationService.GetAuthorizedUserAsync already uses — this does NOT
    // widen UpdateDirectPermissionsAsync/UpdateGroupsAsync, which stay ManageUsersPermissionId-only.
    private const int ManageAssociationUsersPermissionId = 39;

    // Matches PermissionSeedConfiguration.cs id 40 — the ProductionHouse role's own self-service
    // activate/deactivate grant, same shape and same reasoning as ManageAssociationUsersPermissionId
    // above (Phase 1 of the ProductionHouse self-service user-management gap).
    private const int ManageProductionCompanyUsersPermissionId = 40;

    // Matches RoleSeedConfiguration.cs / RolePermissionSeedConfiguration.cs's own RoleId constants.
    private const int BaytAlUrdonRoleId = 1;
    private const int AsezaRoleId = 2;
    private const int ProductionHouseRoleId = 3;
    private const int AssociationRoleId = 4;

    // MAYD-1 real fix (2026-09-24): RoleId and EntityType are two separate enumerations in this
    // codebase (RoleSeedConfiguration's ids don't line up with EntityType's own numeric values), and
    // until now nothing needed to convert one into the other — every created user's EntityType/
    // EntityId simply came from the caller's own session. Bayt-AlUrdon creating a user under a role
    // that ISN'T its own now needs this mapping for real. EntityId is always 1 for the three
    // single-instance-so-far entity types (Aseza/ProductionCompany/Association) — the same
    // placeholder convention every seeded test account for those entities already uses in this Dev
    // DB (see UserSeedConfiguration.cs's own comments: zero real Association/ProductionCompany rows
    // exist yet, a separate already-flagged gap), since there is no real entity-instance picker (or
    // data to pick from) for this ticket to build.
    // Association Management, Phase 2b (2026-09-26): the real gap this closes wasn't "AssociationUsers
    // linking" (that screen — workforcment's AssociationUsersComponent — was already deprecated by
    // its own author, MAYD-21 scope explicitly excludes it) but this: every Association-role user
    // ever created — every seed row, and every cross-entity create through this exact method — has
    // landed on EntityId = 1, a placeholder, because until Phase 2a there were no real Association
    // rows to reference (see UserSeedConfiguration.cs's own comments). The one path that already did
    // this right, EntityOnboardingService's admin-creation flow, sets EntityId = association.Id
    // because it always has a real, already-resolved Association in hand — this brings the SAME
    // real-id behavior to this method's own cross-entity Association-role branch, opt-in via
    // requestedEntityId so every other caller (every non-Association role, and any Association-role
    // create that doesn't pass it) is completely unaffected.
    //
    // requestedEntityId is validated against a real, non-deleted Association (GetByIdAsync is
    // already soft-delete-filtered — same pattern AssociationService.EnsureCityExistsAsync uses for
    // its own City FK) rather than trusted blindly. BaytAlUrdon/Aseza/ProductionHouse ignore it
    // entirely and keep the exact placeholder-1 convention — there's no real multi-instance data for
    // those entity types yet to make an override meaningful, and building one now would be guessing
    // ahead of an actual need (see this phase's own completion report).
    private async Task<(EntityType EntityType, int EntityId)> ResolveEntityForRoleAsync(int roleId, int? requestedEntityId, CancellationToken cancellationToken)
    {
        if (roleId == AssociationRoleId && requestedEntityId.HasValue)
        {
            var association = await _unitOfWork.Associations.GetByIdAsync(requestedEntityId.Value, cancellationToken)
                ?? throw new KeyNotFoundException("Association was not found.");

            return (EntityType.Association, association.Id);
        }

        return roleId switch
        {
            BaytAlUrdonRoleId => (EntityType.BaytAlUrdon, 1),
            AsezaRoleId => (EntityType.Aseza, 1),
            ProductionHouseRoleId => (EntityType.ProductionCompany, 1),
            AssociationRoleId => (EntityType.Association, 1),
            _ => throw new KeyNotFoundException("Role was not found.")
        };
    }

    // MAYD-20: Business Rule (MAYD-1) — only Bayt-AlUrdon and ASEZA may view an entity other than
    // their own; every other role is rejected for anything but its own entity. Requesting no
    // override (both null) always resolves to the caller's own entity, so existing callers of the
    // paginated endpoint that never pass these params keep the exact same "current entity" behavior
    // GetUsersAsync above already has.
    private static (EntityType EntityType, int EntityId) ResolveTargetEntity(User currentUser, EntityType? entityType, int? entityId)
    {
        if (entityType is null && entityId is null)
        {
            return (currentUser.EntityType, currentUser.EntityId);
        }

        if (entityType is null || entityId is null)
        {
            throw new InvalidOperationException("entityType and entityId must both be provided together.");
        }

        var isOwnEntity = entityType == currentUser.EntityType && entityId == currentUser.EntityId;

        if (!isOwnEntity && !CanViewOtherEntities(currentUser))
        {
            throw new UnauthorizedAccessException("Cannot view users outside your own entity.");
        }

        return (entityType.Value, entityId.Value);
    }

    // Same shape as EntityOnboardingService.GetEffectivePermissionIds — deliberately duplicated
    // rather than shared, matching this codebase's established per-service convention (see that
    // method's own history/comment on why).
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

    // MAYD-36 verification gap fix (2026-09-24): strictly same-entity, no override, for the two
    // WRITE paths (UpdateDirectPermissionsAsync/UpdateGroupsAsync) — deliberately NOT given the
    // Bayt-AlUrdon/ASEZA cross-entity override GetViewableUserAsync below has. Every real comment on
    // this Business Rule (MAYD-1) found in this codebase — GetUsersPagedAsync's own header comment,
    // ResolveTargetEntity below — only ever describes the override as a VIEW capability ("ASEZA's is
    // explicitly read-only by the BR"); nothing documents a cross-entity MANAGE/edit capability for
    // any role. Keeping writes strict-same-entity for everyone is the minimal, safest reading of the
    // actual documented rule — it doesn't invent a broader "edit anywhere" capability nobody asked for.
    private async Task<User> GetScopedUserAsync(int userId, User currentUser, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetDetailsAsync(userId, cancellationToken)
            ?? throw new KeyNotFoundException("User was not found.");

        if (user.EntityType != currentUser.EntityType || user.EntityId != currentUser.EntityId)
        {
            throw new UnauthorizedAccessException("Cannot manage users outside the current entity.");
        }

        return user;
    }

    // MAYD-36 verification gap fix (2026-09-24): a real scoping inconsistency this systematic sweep
    // caught that the piecemeal per-ticket passes (MAYD-20, MAYD-25) each missed by only testing
    // their own endpoint — GetUsersPagedAsync (the Users List) already lets Bayt-AlUrdon/ASEZA view a
    // DIFFERENT entity's users via entityType/entityId (see ResolveTargetEntity below), but
    // GetUserDetailsAsync (a single user's own Details page — the page that list's own rows link to)
    // used to go through the strict same-entity-only GetScopedUserAsync above, with no such override
    // at all. Confirmed live: as Ghaith, GET /api/users?entityType=ProductionCompany&entityId=1
    // returned 200 with the real Production House user, but GET /api/users/{thatUserId}/details on
    // that exact user 403'd — the List page could show a row you then couldn't click into. This is
    // the read counterpart of ResolveTargetEntity, used ONLY by GetUserDetailsAsync (and, through it,
    // GetEffectivePermissionsAsync) — never by the two write methods above, which keep the strict
    // check on purpose (see GetScopedUserAsync's own comment on why).
    private async Task<User> GetViewableUserAsync(int userId, User currentUser, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetDetailsAsync(userId, cancellationToken)
            ?? throw new KeyNotFoundException("User was not found.");

        var isOwnEntity = user.EntityType == currentUser.EntityType && user.EntityId == currentUser.EntityId;
        if (!isOwnEntity && !CanViewOtherEntities(currentUser))
        {
            throw new UnauthorizedAccessException("Cannot view users outside your own entity.");
        }

        return user;
    }

    // Shared by GetViewableUserAsync above and ResolveTargetEntity below — extracted so both real
    // cross-entity VIEW paths (single-user Details and the paginated List) apply the exact same
    // Business Rule (MAYD-1) check rather than two copies that could silently drift apart.
    private static bool CanViewOtherEntities(User currentUser) =>
        currentUser.EntityType is EntityType.BaytAlUrdon or EntityType.Aseza;

    private async Task<Group> GetScopedGroupAsync(int groupId, User currentUser, CancellationToken cancellationToken)
    {
        var group = await _unitOfWork.Groups.GetDetailsAsync(groupId, cancellationToken)
            ?? throw new KeyNotFoundException("Group was not found.");

        if (group.EntityType != currentUser.EntityType || group.EntityId != currentUser.EntityId)
        {
            throw new UnauthorizedAccessException("Cannot manage groups outside the current entity.");
        }

        return group;
    }

    private async Task EnsurePermissionsExistAsync(List<int> permissionIds, CancellationToken cancellationToken)
    {
        if (permissionIds.Count == 0)
        {
            return;
        }

        var permissions = await _unitOfWork.Permissions.GetByIdsAsync(permissionIds, cancellationToken);
        if (permissions.Count != permissionIds.Count)
        {
            throw new InvalidOperationException("One or more permissions were not found.");
        }
    }

    private async Task EnsurePermissionsAllowedForRoleAsync(User caller, int roleId, List<int> permissionIds, CancellationToken cancellationToken)
    {
        if (permissionIds.Count == 0)
        {
            return;
        }

        var role = await _unitOfWork.Roles.GetWithPermissionsAsync(roleId, cancellationToken)
            ?? throw new KeyNotFoundException("Role was not found.");

        EnsurePermissionsAllowedForRole(role, permissionIds, GetCallerDelegatablePermissionIds(caller, role));
    }

    private static void EnsurePermissionsAllowedForRole(Role role, List<int> permissionIds, HashSet<int>? callerDelegatableIds = null)
    {
        if (permissionIds.Count == 0)
        {
            return;
        }

        var allowedPermissionIds = role.RolePermissions
            .Where(rp => rp.IsActive && rp.Permission.IsActive)
            .Select(rp => rp.PermissionId)
            .ToHashSet();

        if (callerDelegatableIds is not null)
        {
            allowedPermissionIds.UnionWith(callerDelegatableIds);
        }

        if (permissionIds.Any(permissionId => !allowedPermissionIds.Contains(permissionId)))
        {
            throw new InvalidOperationException("One or more permissions are not available for the selected role.");
        }
    }

    // MAYD-1 real fix (2026-09-24): now takes an explicit target entity rather than a User, so
    // CreateUserAsync can scope group assignment to the NEW user's own target entity (which, for a
    // Bayt-AlUrdon cross-entity create, differs from the caller's) while UpdateGroupsAsync keeps
    // passing its own currentUser.EntityType/EntityId — same values, same behavior as before this
    // refactor, unchanged.
    private async Task<List<Group>> EnsureGroupsInEntityAsync(List<int> groupIds, EntityType entityType, int entityId, CancellationToken cancellationToken)
    {
        if (groupIds.Count == 0)
        {
            return new List<Group>();
        }

        var groups = await _unitOfWork.Groups.GetByIdsInEntityAsync(groupIds, entityType, entityId, cancellationToken);
        if (groups.Count != groupIds.Count)
        {
            throw new UnauthorizedAccessException("One or more groups were not found in the current entity.");
        }

        return groups;
    }

    private async Task<List<User>> EnsureUsersInEntityAsync(List<int> userIds, User currentUser, CancellationToken cancellationToken)
    {
        if (userIds.Count == 0)
        {
            return new List<User>();
        }

        var users = await _unitOfWork.Users.GetByIdsInEntityAsync(userIds, currentUser.EntityType, currentUser.EntityId, cancellationToken);
        if (users.Count != userIds.Count)
        {
            throw new UnauthorizedAccessException("One or more users were not found in the current entity.");
        }

        return users;
    }

    // MAYD-1 real fix (2026-09-24, product-owner-confirmed): Bayt-AlUrdon (Super Admin, RoleId 1) is
    // the one deliberate exception to the "same role as creator" rule below — its confirmed authority
    // is view + CREATE across every entity, not edit (UpdateDirectPermissionsAsync/UpdateGroupsAsync
    // are untouched by this fix and stay strict same-entity for every role, Bayt-AlUrdon included —
    // see those methods' own code, unchanged). ASEZA is NOT given this exception: its role is
    // oversight/view-only over Associations and ProductionHouse (already real, see
    // GetViewableUserAsync/ResolveTargetEntity), plus the ability to create users within its OWN
    // entity only once granted the real CreateUsers permission (RolePermissionSeedConfiguration.cs) —
    // it still creates same-role-as-itself like everyone but Bayt-AlUrdon.
    //
    // Superseded, no longer applies as of this fix: the "audit follow-up" comment this replaced
    // (5bc0ad0) rejected EVERY cross-role create outright specifically because CreateUserAsync's
    // EntityType/EntityId assignment always came from the caller, which would have silently
    // mis-scoped a Bayt-AlUrdon-created Association user as EntityType.BaytAlUrdon. That assignment
    // is fixed alongside this check (see CreateUserAsync's own comment) — the two changes are a pair.
    private static void EnsureSameEntityCreation(User currentUser, int requestedRoleId)
    {
        if (requestedRoleId == currentUser.RoleId)
        {
            return;
        }

        if (currentUser.RoleId == BaytAlUrdonRoleId)
        {
            return;
        }

        throw new InvalidOperationException("Cannot create a user with a different role than the current user's own role.");
    }

    private static void ValidateUserPayload(CreateEntityUserDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.FirstNameEn) ||
            string.IsNullOrWhiteSpace(dto.LastNameEn) ||
            string.IsNullOrWhiteSpace(dto.FirstNameAr) ||
            string.IsNullOrWhiteSpace(dto.LastNameAr) ||
            string.IsNullOrWhiteSpace(dto.Email) ||
            string.IsNullOrWhiteSpace(dto.PhoneNumber) ||
            string.IsNullOrWhiteSpace(dto.InitialPassword))
        {
            throw new InvalidOperationException("User mandatory fields are required.");
        }
    }

    private static void ValidateGroupPayload(string groupNameEn, string groupNameAr)
    {
        if (string.IsNullOrWhiteSpace(groupNameEn) || string.IsNullOrWhiteSpace(groupNameAr))
        {
            throw new InvalidOperationException("Group English and Arabic names are required.");
        }
    }

    private static List<int> NormalizeIds(IEnumerable<int> ids) =>
        ids.Where(id => id > 0).Distinct().OrderBy(id => id).ToList();

    private static void SyncUserPermissions(User user, List<int> permissionIds)
    {
        var requestedIds = permissionIds.ToHashSet();
        var existingIds = user.UserPermissions.Select(up => up.PermissionId).ToHashSet();

        foreach (var userPermission in user.UserPermissions.Where(up => !requestedIds.Contains(up.PermissionId)).ToList())
        {
            user.UserPermissions.Remove(userPermission);
        }

        foreach (var permissionId in requestedIds.Except(existingIds))
        {
            user.UserPermissions.Add(new UserPermission
            {
                UserId = user.UserId,
                PermissionId = permissionId,
                IsActive = true
            });
        }
    }

    private static void SyncUserGroups(User user, List<int> groupIds)
    {
        var requestedIds = groupIds.ToHashSet();
        var existingIds = user.UserGroups.Select(ug => ug.GroupId).ToHashSet();

        foreach (var userGroup in user.UserGroups.Where(ug => !requestedIds.Contains(ug.GroupId)).ToList())
        {
            user.UserGroups.Remove(userGroup);
        }

        foreach (var groupId in requestedIds.Except(existingIds))
        {
            user.UserGroups.Add(new UserGroup
            {
                UserId = user.UserId,
                GroupId = groupId
            });
        }
    }

    private static void SyncGroupPermissions(Group group, List<int> permissionIds)
    {
        var requestedIds = permissionIds.ToHashSet();
        var existingIds = group.GroupPermissions.Select(gp => gp.PermissionId).ToHashSet();

        foreach (var groupPermission in group.GroupPermissions.Where(gp => !requestedIds.Contains(gp.PermissionId)).ToList())
        {
            group.GroupPermissions.Remove(groupPermission);
        }

        foreach (var permissionId in requestedIds.Except(existingIds))
        {
            group.GroupPermissions.Add(new GroupPermission
            {
                GroupId = group.GroupId,
                PermissionId = permissionId,
                IsActive = true
            });
        }
    }

    private static void SyncGroupUsers(Group group, List<int> userIds)
    {
        var requestedIds = userIds.ToHashSet();
        var existingIds = group.UserGroups.Select(ug => ug.UserId).ToHashSet();

        foreach (var userGroup in group.UserGroups.Where(ug => !requestedIds.Contains(ug.UserId)).ToList())
        {
            group.UserGroups.Remove(userGroup);
        }

        foreach (var userId in requestedIds.Except(existingIds))
        {
            group.UserGroups.Add(new UserGroup
            {
                UserId = userId,
                GroupId = group.GroupId
            });
        }
    }

    // Internal rather than private (2026-09-29, Association details/users gap): AssociationService.
    // GetDetailsAsync reuses this exact mapping for the same real User rows (GetByEntityAsync) rather
    // than duplicating it — same assembly, so internal is enough, no interface needed for one method.
    internal static UserSummaryDto MapUserSummary(User user) =>
        new(
            user.UserId,
            user.FirstNameEn,
            user.LastNameEn,
            user.FirstNameAr,
            user.LastNameAr,
            user.Email,
            user.PhoneNumber,
            user.RoleId,
            user.Role.RoleNameEn,
            user.Role.RoleNameAr,
            user.MustResetPassword,
            user.IsActive);

    private static UserDetailsDto MapUserDetails(User user)
    {
        var directPermissions = user.UserPermissions
            .Where(up => up.IsActive && up.Permission.IsActive)
            .Select(up => up.Permission)
            .DistinctBy(p => p.PermissionId)
            .Select(MapPermission)
            .OrderBy(p => p.Module)
            .ThenBy(p => p.PermissionNameEn)
            .ToList();

        var groupPermissions = user.UserGroups
            .Select(ug => ug.Group)
            .Where(g => g.IsActive)
            .SelectMany(g => g.GroupPermissions)
            .Where(gp => gp.IsActive && gp.Permission.IsActive)
            .Select(gp => gp.Permission);

        var effectivePermissions = user.UserPermissions
            .Where(up => up.IsActive && up.Permission.IsActive)
            .Select(up => up.Permission)
            .Concat(groupPermissions)
            .DistinctBy(p => p.PermissionId)
            .Select(MapPermission)
            .OrderBy(p => p.Module)
            .ThenBy(p => p.PermissionNameEn)
            .ToList();

        return new UserDetailsDto(
            user.UserId,
            user.FirstNameEn,
            user.LastNameEn,
            user.FirstNameAr,
            user.LastNameAr,
            user.Email,
            user.PhoneNumber,
            user.RoleId,
            user.Role.RoleNameEn,
            user.Role.RoleNameAr,
            user.EntityType,
            user.EntityId,
            user.MustResetPassword,
            user.IsActive,
            directPermissions,
            user.UserGroups.Select(ug => MapGroupSummary(ug.Group)).ToList(),
            effectivePermissions);
    }

    // MAYD-31: matches GetAvailablePermissionsAsync's own Module-then-name ordering, so the same
    // permission always appears first in both this preview and the full catalog page — a
    // deterministic, real ordering rather than whatever order EF happened to materialize rows in.
    private const int GroupPermissionPreviewSize = 4;

    private static GroupSummaryDto MapGroupSummary(Group group)
    {
        var activePermissions = group.GroupPermissions
            .Where(gp => gp.IsActive && gp.Permission.IsActive)
            .Select(gp => gp.Permission)
            .OrderBy(p => p.Module)
            .ThenBy(p => p.PermissionNameEn)
            .ToList();

        return new(
            group.GroupId,
            group.GroupNameEn,
            group.GroupNameAr,
            activePermissions.Count,
            group.UserGroups.Count,
            activePermissions.Take(GroupPermissionPreviewSize).Select(MapPermission).ToList());
    }

    private static GroupDetailsDto MapGroupDetails(Group group) =>
        new(
            group.GroupId,
            group.GroupNameEn,
            group.GroupNameAr,
            group.EntityType,
            group.EntityId,
            group.GroupPermissions
                .Where(gp => gp.IsActive && gp.Permission.IsActive)
                .Select(gp => MapPermission(gp.Permission))
                .OrderBy(p => p.Module)
                .ThenBy(p => p.PermissionNameEn)
                .ToList(),
            group.UserGroups
                .Select(ug => MapUserSummary(ug.User))
                .OrderBy(u => u.FirstNameEn)
                .ThenBy(u => u.LastNameEn)
                .ToList());

    private static PermissionDto MapPermission(Permission permission) =>
        new(
            permission.PermissionId,
            permission.PermissionNameEn,
            permission.PermissionNameAr,
            permission.Module);
}
