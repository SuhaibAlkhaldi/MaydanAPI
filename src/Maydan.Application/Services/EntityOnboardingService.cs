using Maydan.Application.DTOs.Common;
using Maydan.Application.DTOs.Onboarding;
using Maydan.Application.DTOs.UserManagement;
using Maydan.Application.Interfaces;
using Maydan.Domain.Entities;
using Maydan.Domain.Enums;

namespace Maydan.Application.Services;

public class EntityOnboardingService : IEntityOnboardingService
{
    private const int OnboardEntitiesPermissionId = 37;

    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;

    public EntityOnboardingService(IUnitOfWork unitOfWork, IPasswordHasher passwordHasher)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
    }

    public async Task<ApiResponse<UserDetailsDto>> OnboardAssociationAdminAsync(int currentUserId, int associationId, OnboardAssociationAdminDto dto, CancellationToken cancellationToken = default)
    {
        var (currentUser, authError) = await EnsureCallerCanOnboardAsync(currentUserId, cancellationToken);
        if (authError != null) return authError;

        try
        {
            AssociationAdminUserFactory.ValidatePayload(dto);
        }
        catch (InvalidOperationException ex)
        {
            return ApiResponse<UserDetailsDto>.FailureResponse(ex.Message, "بيانات التسجيل غير صالحة.");
        }

        var association = await _unitOfWork.Associations.GetByIdAsync(associationId, cancellationToken);
        if (association == null)
            return ApiResponse<UserDetailsDto>.FailureResponse("Association was not found.", "الجمعية غير موجودة.");

        var existingUsers = await _unitOfWork.Users.GetByEntityAsync(EntityType.Association, association.Id, null, cancellationToken);
        if (existingUsers.Count > 0)
        {
            return ApiResponse<UserDetailsDto>.FailureResponse("This Association already has an admin.", "هذه الجمعية تمتلك مسؤولاً بالفعل.");
        }

        var email = dto.Email.Trim();
        if (await _unitOfWork.Users.EmailExistsAsync(email, cancellationToken))
        {
            return ApiResponse<UserDetailsDto>.FailureResponse("A user with this email already exists.", "يوجد مستخدم مسجل بهذا البريد الإلكتروني مسبقاً.");
        }

        var role = await _unitOfWork.Roles.GetWithPermissionsAsync(AssociationAdminUserFactory.AssociationRoleId, cancellationToken);
        if (role == null)
            return ApiResponse<UserDetailsDto>.FailureResponse("Association role was not found.", "دور الجمعية غير موجود.");

        var user = AssociationAdminUserFactory.Build(role, association.Id, dto, _passwordHasher);

        await _unitOfWork.Users.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var createdUser = await _unitOfWork.Users.GetDetailsReadOnlyAsync(user.UserId, cancellationToken);
        if (createdUser == null)
            return ApiResponse<UserDetailsDto>.FailureResponse("Created user was not found.", "المستخدم المنشأ غير موجود.");

        return ApiResponse<UserDetailsDto>.SuccessResponse(MapUserDetails(createdUser), "Admin onboarded successfully.", "تم تسجيل المسؤول بنجاح.", 201);
    }

    public async Task<ApiResponse<List<AssociationWithoutAdminDto>>> GetAssociationsWithoutAdminAsync(int currentUserId, CancellationToken cancellationToken = default)
    {
        var (currentUser, authError) = await EnsureCallerCanOnboardAsync<List<AssociationWithoutAdminDto>>(currentUserId, cancellationToken);
        if (authError != null) return authError;

        var associations = await _unitOfWork.Associations.GetAllAsync(cancellationToken);
        var result = new List<AssociationWithoutAdminDto>();

        foreach (var association in associations)
        {
            var existingUsers = await _unitOfWork.Users.GetByEntityAsync(EntityType.Association, association.Id, null, cancellationToken);
            if (existingUsers.Count == 0)
            {
                result.Add(new AssociationWithoutAdminDto(association.Id, association.EnglishName, association.ArabicName, association.CityId));
            }
        }

        return ApiResponse<List<AssociationWithoutAdminDto>>.SuccessResponse(result, "Associations retrieved successfully.", "تم استرجاع الجمعيات بنجاح.");
    }

    private async Task<(User? User, ApiResponse<T>? Error)> EnsureCallerCanOnboardAsync<T>(int currentUserId, CancellationToken cancellationToken)
    {
        var currentUser = await _unitOfWork.Users.GetWithPermissionsAsync(currentUserId, cancellationToken);
        if (currentUser == null)
            return (null, ApiResponse<T>.FailureResponse("Current user was not found.", "المستخدم الحالي غير موجود."));

        if (!currentUser.IsActive)
            return (null, ApiResponse<T>.FailureResponse("Current user is inactive.", "المستخدم الحالي غير فعّال."));

        if (currentUser.EntityType != EntityType.BaytAlUrdon)
            return (null, ApiResponse<T>.FailureResponse("Only Bayt-AlUrdon staff can onboard a new entity admin.", "فقط موظفو بيت الأردن يمكنهم تسجيل مسؤول كيان جديد."));

        if (!GetEffectivePermissionIds(currentUser).Contains(OnboardEntitiesPermissionId))
            return (null, ApiResponse<T>.FailureResponse("Caller does not hold the Onboard Entities permission.", "لا تملك الصلاحية المطلوبة لتسجيل الكيانات."));

        return (currentUser, null);
    }

    private async Task<(User? User, ApiResponse<UserDetailsDto>? Error)> EnsureCallerCanOnboardAsync(int currentUserId, CancellationToken cancellationToken)
    {
        return await EnsureCallerCanOnboardAsync<UserDetailsDto>(currentUserId, cancellationToken);
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