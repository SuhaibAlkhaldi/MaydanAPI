using Maydan.Application.DTOs.Onboarding;
using Maydan.Application.Interfaces;
using Maydan.Domain.Entities;
using Maydan.Domain.Enums;

namespace Maydan.Application.Services;

// Extracted 2026-09-29 (Association Admin User gap): EntityOnboardingService.OnboardAssociationAdminAsync
// (onboarding an admin onto an EXISTING, already admin-less association) and AssociationService.CreateAsync's
// new optional inline-admin path (creating a BRAND NEW association with its first admin in one call)
// both need to build the exact same kind of User row — same target role, same permission-copy, same
// forced-reset-password convention for an admin someone else is creating on the new admin's behalf.
// Shared here so neither duplicates that logic; each caller still does its own, genuinely different,
// surrounding checks (OnboardAssociationAdminAsync's "does this association already have an admin?"
// doesn't apply to a brand-new association, which can't have one yet).
internal static class AssociationAdminUserFactory
{
    // Matches RolePermissionSeedConfiguration.cs's Association const (RoleId 4).
    public const int AssociationRoleId = 4;

    public static void ValidatePayload(OnboardAssociationAdminDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.FirstNameEn) ||
            string.IsNullOrWhiteSpace(dto.LastNameEn) ||
            string.IsNullOrWhiteSpace(dto.FirstNameAr) ||
            string.IsNullOrWhiteSpace(dto.LastNameAr) ||
            string.IsNullOrWhiteSpace(dto.Email) ||
            string.IsNullOrWhiteSpace(dto.PhoneNumber) ||
            string.IsNullOrWhiteSpace(dto.InitialPassword))
        {
            throw new InvalidOperationException("All admin fields are required.");
        }

    }

    public static User Build(Role associationRole, int associationId, OnboardAssociationAdminDto dto, IPasswordHasher passwordHasher)
    {
        var user = new User
        {
            FirstNameEn = dto.FirstNameEn.Trim(),
            LastNameEn = dto.LastNameEn.Trim(),
            FirstNameAr = dto.FirstNameAr.Trim(),
            LastNameAr = dto.LastNameAr.Trim(),
            Email = dto.Email.Trim(),
            PhoneNumber = dto.PhoneNumber.Trim(),
            PasswordHash = passwordHasher.HashPassword(dto.InitialPassword),
            // Whoever is calling this (Bayt-AlUrdon onboarding staff, or the association creator) is
            // picking this password on the new admin's behalf, not the admin themselves — force a
            // reset on first login, same as UserManagementService.CreateUserAsync's own wizard-created
            // employees.
            MustResetPassword = true,
            IsActive = true,
            RoleId = associationRole.RoleId,
            EntityType = EntityType.Association,
            EntityId = associationId
        };

        foreach (var rolePermission in associationRole.RolePermissions.Where(rp => rp.IsActive && rp.Permission.IsActive))
        {
            user.UserPermissions.Add(new UserPermission { PermissionId = rolePermission.PermissionId, IsActive = true });
        }

        return user;
    }
}
