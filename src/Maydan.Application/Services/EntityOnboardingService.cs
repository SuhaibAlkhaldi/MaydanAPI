using Maydan.Application.DTOs.Onboarding;
using Maydan.Application.DTOs.UserManagement;
using Maydan.Application.Interfaces;
using Maydan.Domain.Entities;
using Maydan.Domain.Enums;

namespace Maydan.Application.Services;

// Entity onboarding Stage 2 (2026-09-22): Bayt-AlUrdon picking an existing, admin-less Association
// (Associations are pre-seeded from GIS data — never created here, unlike Stage 1's
// ProductionCompany self-registration) and creating its first admin. Its own service, not a
// UserManagementService method: UserManagementService.CreateUserAsync's GetScopedUserAsync/
// EnsureSameEntityCreation assume the caller and the new user share one entity — the whole point
// here is a Bayt-AlUrdon caller creating a user scoped to a DIFFERENT entity (the target
// Association), which that guard exists specifically to reject in the normal wizard.
public class EntityOnboardingService : IEntityOnboardingService
{
    // Matches PermissionSeedConfiguration.cs id 37 ("Onboard Entities"). The target role id
    // (RoleId 4) now lives on AssociationAdminUserFactory, shared with AssociationService's own
    // inline-admin path — see that class's own comment.
    private const int OnboardEntitiesPermissionId = 37;

    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;

    public EntityOnboardingService(IUnitOfWork unitOfWork, IPasswordHasher passwordHasher)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
    }

    public async Task<UserDetailsDto> OnboardAssociationAdminAsync(int currentUserId, int associationId, OnboardAssociationAdminDto dto, CancellationToken cancellationToken = default)
    {
        await EnsureCallerCanOnboardAsync(currentUserId, cancellationToken);
        AssociationAdminUserFactory.ValidatePayload(dto);

        var association = await _unitOfWork.Associations.GetByIdAsync(associationId, cancellationToken)
            ?? throw new KeyNotFoundException("Association was not found.");

        var existingUsers = await _unitOfWork.Users.GetByEntityAsync(EntityType.Association, association.Id, null, cancellationToken);
        if (existingUsers.Count > 0)
        {
            throw new InvalidOperationException("This Association already has an admin.");
        }

        var email = dto.Email.Trim();
        if (await _unitOfWork.Users.EmailExistsAsync(email, cancellationToken))
        {
            throw new InvalidOperationException("A user with this email already exists.");
        }

        var role = await _unitOfWork.Roles.GetWithPermissionsAsync(AssociationAdminUserFactory.AssociationRoleId, cancellationToken)
            ?? throw new KeyNotFoundException("Association role was not found.");

        var user = AssociationAdminUserFactory.Build(role, association.Id, dto, _passwordHasher);

        // Unlike Stage 1: no create-parent-then-fetch-generated-id dance, and so no transaction
        // wrapper — the Association already exists and associationId is already real, so EntityId
        // is known up front. One SaveChangesAsync creating just the User is genuinely enough.
        await _unitOfWork.Users.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var createdUser = await _unitOfWork.Users.GetDetailsReadOnlyAsync(user.UserId, cancellationToken)
            ?? throw new KeyNotFoundException("Created user was not found.");

        return MapUserDetails(createdUser);
    }

    public async Task<List<AssociationWithoutAdminDto>> GetAssociationsWithoutAdminAsync(int currentUserId, CancellationToken cancellationToken = default)
    {
        await EnsureCallerCanOnboardAsync(currentUserId, cancellationToken);

        var associations = await _unitOfWork.Associations.GetAllAsync(cancellationToken);
        var result = new List<AssociationWithoutAdminDto>();

        // N+1 by design: this is a small, infrequent Bayt-AlUrdon admin list (one row per
        // GIS-seeded Association), not a hot path — a purpose-built "associations with no users"
        // repository query would be premature optimization for a Stage 4 admin screen.
        foreach (var association in associations)
        {
            var existingUsers = await _unitOfWork.Users.GetByEntityAsync(EntityType.Association, association.Id, null, cancellationToken);
            if (existingUsers.Count == 0)
            {
                result.Add(new AssociationWithoutAdminDto(association.Id, association.EnglishName, association.ArabicName, association.CityId));
            }
        }

        return result;
    }

    // Framework-level authorization here is just [Authorize] (any valid JWT) — this codebase has
    // no per-permission policy/attribute mechanism anywhere (Program.cs's AddAuthorization() has
    // zero custom policies; Stage 1's session and the RBAC gaps work both found permission
    // enforcement is otherwise client-side only). So the real gate is here, re-fetching the
    // caller's role/permissions from the DB rather than trusting JWT claims — same shape as
    // UserManagementService.EnsureSameEntityCreation guarding the existing wizard.
    private async Task<User> EnsureCallerCanOnboardAsync(int currentUserId, CancellationToken cancellationToken)
    {
        var currentUser = await _unitOfWork.Users.GetWithPermissionsAsync(currentUserId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Current user was not found.");

        if (!currentUser.IsActive)
        {
            throw new UnauthorizedAccessException("Current user is inactive.");
        }

        if (currentUser.EntityType != EntityType.BaytAlUrdon)
        {
            throw new UnauthorizedAccessException("Only Bayt-AlUrdon staff can onboard a new entity admin.");
        }

        if (!GetEffectivePermissionIds(currentUser).Contains(OnboardEntitiesPermissionId))
        {
            throw new UnauthorizedAccessException("Caller does not hold the Onboard Entities permission.");
        }

        return currentUser;
    }

    private static HashSet<int> GetEffectivePermissionIds(User user)
    {
        var rolePermissionIds = user.Role?.RolePermissions?
         .Where(rp => rp.IsActive && (rp.Permission == null || rp.Permission.IsActive))
         .Select(rp => rp.PermissionId) ?? Enumerable.Empty<int>();

        var directPermissionIds = user.UserPermissions?
            .Where(up => up.IsActive && (up.Permission == null || up.Permission.IsActive))
            .Select(up => up.PermissionId) ?? Enumerable.Empty<int>();

        var groupPermissionIds = user.UserGroups?
            .Where(ug => ug.Group != null && ug.Group.IsActive)
            .SelectMany(ug => ug.Group.GroupPermissions ?? new List<GroupPermission>())
            .Where(gp => gp.IsActive && (gp.Permission == null || gp.Permission.IsActive))
            .Select(gp => gp.PermissionId) ?? Enumerable.Empty<int>();

        return rolePermissionIds.Concat(directPermissionIds).Concat(groupPermissionIds).ToHashSet();
    }

    // Structurally the same projection as UserManagementService.MapUserDetails (private there) —
    // not reused directly because GetUserDetailsAsync's GetScopedUserAsync would reject this
    // exact cross-entity case (Bayt-AlUrdon caller, Association-scoped new user) by design.
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

    // MAYD-31: matches UserManagementService's own MapGroupSummary shape — the newly onboarded
    // admin this response describes never has any groups yet (see this method's own caller), so
    // group.GroupPermissions is always empty here in practice; kept consistent with the real DTO
    // shape anyway rather than special-casing an empty preview.
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
            activePermissions.Take(4).Select(MapPermission).ToList());
    }

    private static PermissionDto MapPermission(Permission permission) =>
        new(
            permission.PermissionId,
            permission.PermissionNameEn,
            permission.PermissionNameAr,
            permission.Module);
}
