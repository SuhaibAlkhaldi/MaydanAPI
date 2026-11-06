using Maydan.Application.DTOs.SystemConfiguration;
using Maydan.Application.Interfaces;
using Maydan.Domain.Entities;
using Maydan.Domain.Enums;

namespace Maydan.Application.Services;

// System Configuration gate (MAYD-133, 2026-09-24): the admin screen's own read/update, plus the
// "is configuration complete" check the gate (SystemConfigurationGateService) calls for every
// super-admin request. Deliberately its own service, not folded into UserManagementService — this
// is system-wide singleton state, not scoped to any one entity/user the way everything in
// UserManagementService is.
public class SystemConfigurationService : ISystemConfigurationService
{
    // Matches PermissionSeedConfiguration.cs id 38 ("Manage System Configuration").
    private const int ManageSystemConfigurationPermissionId = 38;

    private readonly IUnitOfWork _unitOfWork;
    private readonly ISecretProtector _secretProtector;

    public SystemConfigurationService(IUnitOfWork unitOfWork, ISecretProtector secretProtector)
    {
        _unitOfWork = unitOfWork;
        _secretProtector = secretProtector;
    }

    public async Task<SystemConfigurationDto> GetAsync(int currentUserId, CancellationToken cancellationToken = default)
    {
        await EnsureCallerCanManageAsync(currentUserId, cancellationToken);

        var configuration = await _unitOfWork.SystemConfigurations.GetAsync(cancellationToken);
        return MapToDto(configuration);
    }

    public async Task<SystemConfigurationDto> UpdateAsync(int currentUserId, UpdateSystemConfigurationDto dto, CancellationToken cancellationToken = default)
    {
        await EnsureCallerCanManageAsync(currentUserId, cancellationToken);
        ValidatePayload(dto);

        var configuration = await _unitOfWork.SystemConfigurations.GetAsync(cancellationToken);

        if (configuration is null)
        {
            // First-ever save: a password is required — there is nothing to fall back to.
            if (string.IsNullOrWhiteSpace(dto.SmtpPassword))
            {
                throw new InvalidOperationException("SMTP password is required.");
            }

            configuration = new SystemConfiguration
            {
                IsActive = true
            };

            ApplyFields(configuration, dto);
            await _unitOfWork.SystemConfigurations.AddAsync(configuration, cancellationToken);
        }
        else
        {
            // Blank SmtpPassword on an update means "keep the existing one" — the frontend never
            // gets the real value back (SystemConfigurationDto.HasSmtpPassword only), so it can't
            // round-trip it, and re-saving the other fields shouldn't force re-entering it.
            ApplyFields(configuration, dto);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(configuration);
    }

    public async Task<bool> IsConfiguredAsync(CancellationToken cancellationToken = default)
    {
        var configuration = await _unitOfWork.SystemConfigurations.GetAsync(cancellationToken);
        return IsComplete(configuration);
    }

    private void ApplyFields(SystemConfiguration configuration, UpdateSystemConfigurationDto dto)
    {
        configuration.SmtpHost = dto.SmtpHost.Trim();
        configuration.SmtpPort = dto.SmtpPort;
        configuration.SmtpUsername = dto.SmtpUsername.Trim();
        configuration.SenderEmail = dto.SenderEmail.Trim();
        configuration.SenderDisplayName = dto.SenderDisplayName.Trim();

        if (!string.IsNullOrWhiteSpace(dto.SmtpPassword))
        {
            configuration.SmtpPasswordProtected = _secretProtector.Protect(dto.SmtpPassword);
        }
    }

    // Single source of truth for "is configuration complete" — computed, not a stored flag, so it
    // can never drift out of sync with the actual field values.
    private static bool IsComplete(SystemConfiguration? configuration) =>
        configuration is not null
        && !string.IsNullOrWhiteSpace(configuration.SmtpHost)
        && configuration.SmtpPort > 0
        && !string.IsNullOrWhiteSpace(configuration.SmtpUsername)
        && !string.IsNullOrWhiteSpace(configuration.SmtpPasswordProtected)
        && !string.IsNullOrWhiteSpace(configuration.SenderEmail)
        && !string.IsNullOrWhiteSpace(configuration.SenderDisplayName);

    private static SystemConfigurationDto MapToDto(SystemConfiguration? configuration)
    {
        if (configuration is null)
        {
            return new SystemConfigurationDto(false, string.Empty, 0, string.Empty, false, string.Empty, string.Empty);
        }

        return new SystemConfigurationDto(
            IsComplete(configuration),
            configuration.SmtpHost,
            configuration.SmtpPort,
            configuration.SmtpUsername,
            !string.IsNullOrWhiteSpace(configuration.SmtpPasswordProtected),
            configuration.SenderEmail,
            configuration.SenderDisplayName);
    }

    private static void ValidatePayload(UpdateSystemConfigurationDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.SmtpHost) ||
            string.IsNullOrWhiteSpace(dto.SmtpUsername) ||
            string.IsNullOrWhiteSpace(dto.SenderEmail) ||
            string.IsNullOrWhiteSpace(dto.SenderDisplayName))
        {
            throw new InvalidOperationException("All configuration fields are required.");
        }

        if (dto.SmtpPort is <= 0 or > 65535)
        {
            throw new InvalidOperationException("SMTP port must be between 1 and 65535.");
        }
    }

    // Same dual-check shape as EntityOnboardingService.EnsureCallerCanOnboardAsync (EntityType +
    // a dedicated permission) — the established pattern in this codebase for "only this one
    // specific real-world entity type, holding this one specific permission, may do this".
    private async Task<User> EnsureCallerCanManageAsync(int currentUserId, CancellationToken cancellationToken)
    {
        var currentUser = await _unitOfWork.Users.GetWithPermissionsAsync(currentUserId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Current user was not found.");

        if (!currentUser.IsActive)
        {
            throw new UnauthorizedAccessException("Current user is inactive.");
        }

        if (currentUser.EntityType != EntityType.BaytAlUrdon)
        {
            throw new UnauthorizedAccessException("Only Bayt-AlUrdon staff can manage the system configuration.");
        }

        if (!GetEffectivePermissionIds(currentUser).Contains(ManageSystemConfigurationPermissionId))
        {
            throw new UnauthorizedAccessException("Caller does not hold the Manage System Configuration permission.");
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
}
