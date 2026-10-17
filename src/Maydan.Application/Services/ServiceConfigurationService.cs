using Maydan.Application.DTOs.Services;
using Maydan.Application.Interfaces;
using Maydan.Domain.Entities;
using Maydan.Domain.Enums;
using System.Globalization;

namespace Maydan.Application.Services;

public class ServiceConfigurationService : IServiceConfigurationService
{
    private const int ManageSystemConfigurationPermissionId = 38;

    private readonly IUnitOfWork _unitOfWork;

    public ServiceConfigurationService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<ServiceDto>> GetAllServicesAsync(int currentUserId, CancellationToken cancellationToken = default)
    {
        await EnsureCallerCanManageAsync(currentUserId, cancellationToken);
        var services = await _unitOfWork.Services.GetAllAsync(cancellationToken);
        return services.Select(MapToDto).ToList();
    }

    public async Task<List<ServiceDto>> GetActiveServicesAsync(CancellationToken cancellationToken = default)
    {
        var services = await _unitOfWork.Services.GetActiveAsync(cancellationToken);
        return services.Select(MapToDto).ToList();
    }

    public async Task<ServiceDto> GetServiceByIdAsync(int currentUserId, int id, CancellationToken cancellationToken = default)
    {
        await EnsureCallerCanManageAsync(currentUserId, cancellationToken);
        var service = await _unitOfWork.Services.GetByIdAsync(id, cancellationToken)
            ?? throw new Maydan.Application.Exceptions.BilingualNotFoundException("الخدمة غير موجودة.", "Service not found.");
        return MapToDto(service);
    }

    public async Task<ServiceDto> CreateServiceAsync(int currentUserId, CreateServiceDto dto, CancellationToken cancellationToken = default)
    {
        await EnsureCallerCanManageAsync(currentUserId, cancellationToken);
        ValidatePayload(dto);

        // Duplicate checks exclude deleted rows via repository normal behavior (soft-delete filter)
        if (await _unitOfWork.Services.FindByNameEnAsync(dto.NameEn.Trim(), cancellationToken) is not null)
        {
            throw new Maydan.Application.Exceptions.BilingualException("اسم الخدمة الإنجليزي موجود بالفعل.", "English service name already exists.");
        }

        if (await _unitOfWork.Services.FindByNameArAsync(dto.NameAr.Trim(), cancellationToken) is not null)
        {
            throw new Maydan.Application.Exceptions.BilingualException("اسم الخدمة العربي موجود بالفعل.", "Arabic service name already exists.");
        }

        var service = new Service
        {
            NameEn = dto.NameEn.Trim(),
            NameAr = dto.NameAr.Trim(),
            CalculationType = dto.CalculationType,
            Price = dto.Price,
            IsActive = true
        };

        await _unitOfWork.Services.AddAsync(service, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(service);
    }

    public async Task<ServiceDto> UpdateServiceAsync(int currentUserId, int id, UpdateServiceDto dto, CancellationToken cancellationToken = default)
    {
        await EnsureCallerCanManageAsync(currentUserId, cancellationToken);
        ValidatePayload(dto);

        var service = await _unitOfWork.Services.GetByIdAsync(id, cancellationToken)
            ?? throw new Maydan.Application.Exceptions.BilingualNotFoundException("الخدمة غير موجودة.", "Service not found.");

        var otherByEn = await _unitOfWork.Services.FindByNameEnAsync(dto.NameEn.Trim(), cancellationToken);
        if (otherByEn is not null && otherByEn.Id != id)
        {
            throw new Maydan.Application.Exceptions.BilingualException("اسم الخدمة الإنجليزي موجود بالفعل.", "English service name already exists.");
        }

        var otherByAr = await _unitOfWork.Services.FindByNameArAsync(dto.NameAr.Trim(), cancellationToken);
        if (otherByAr is not null && otherByAr.Id != id)
        {
            throw new Maydan.Application.Exceptions.BilingualException("اسم الخدمة العربي موجود بالفعل.", "Arabic service name already exists.");
        }

        service.NameEn = dto.NameEn.Trim();
        service.NameAr = dto.NameAr.Trim();
        service.CalculationType = dto.CalculationType;
        service.Price = dto.Price;
        service.IsActive = dto.IsActive;

        _unitOfWork.Services.Update(service);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(service);
    }

    public async Task DeleteServiceAsync(int currentUserId, int id, CancellationToken cancellationToken = default)
    {
        await EnsureCallerCanManageAsync(currentUserId, cancellationToken);
        var service = await _unitOfWork.Services.GetByIdAsync(id, cancellationToken)
            ?? throw new Maydan.Application.Exceptions.BilingualNotFoundException("الخدمة غير موجودة.", "Service not found.");

        _unitOfWork.Services.Remove(service);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static ServiceDto MapToDto(Service s) => new(s.Id, s.NameEn, s.NameAr, s.CalculationType, s.Price, s.IsActive);

    private static void ValidatePayload(CreateServiceDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.NameEn)) throw new Maydan.Application.Exceptions.BilingualException("اسم الخدمة بالإنجليزية مطلوب.", "NameEn is required.");
        if (string.IsNullOrWhiteSpace(dto.NameAr)) throw new Maydan.Application.Exceptions.BilingualException("اسم الخدمة بالعربية مطلوب.", "NameAr is required.");
        if (dto.Price < 0) throw new Maydan.Application.Exceptions.BilingualException("يجب أن تكون السعر غير سالبة.", "Price must be non-negative.");
        if (!Enum.IsDefined(typeof(CalculationType), dto.CalculationType)) throw new Maydan.Application.Exceptions.BilingualException("نوع الحساب غير صالح.", "Invalid CalculationType.");
    }

    private static void ValidatePayload(UpdateServiceDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.NameEn)) throw new Maydan.Application.Exceptions.BilingualException("اسم الخدمة بالإنجليزية مطلوب.", "NameEn is required.");
        if (string.IsNullOrWhiteSpace(dto.NameAr)) throw new Maydan.Application.Exceptions.BilingualException("اسم الخدمة بالعربية مطلوب.", "NameAr is required.");
        if (dto.Price < 0) throw new Maydan.Application.Exceptions.BilingualException("يجب أن تكون السعر غير سالبة.", "Price must be non-negative.");
        if (!Enum.IsDefined(typeof(CalculationType), dto.CalculationType)) throw new Maydan.Application.Exceptions.BilingualException("نوع الحساب غير صالح.", "Invalid CalculationType.");
    }

    // Reuse SystemConfigurationService's authorization shape: only BaytAlUrdon users with ManageSystemConfiguration
    // may manage services. Replicate the same checks here since SystemConfigurationService.EnsureCallerCanManageAsync
    // is private there.
    private async Task EnsureCallerCanManageAsync(int currentUserId, CancellationToken cancellationToken)
    {
        var currentUser = await _unitOfWork.Users.GetWithPermissionsAsync(currentUserId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Current user was not found.");

        if (!currentUser.IsActive) throw new UnauthorizedAccessException("Current user is inactive.");

        if (currentUser.EntityType != EntityType.BaytAlUrdon)
        {
            throw new UnauthorizedAccessException("Only Bayt-AlUrdon staff can manage the system configuration.");
        }

        var effective = GetEffectivePermissionIds(currentUser);
        if (!effective.Contains(ManageSystemConfigurationPermissionId))
        {
            throw new UnauthorizedAccessException("Caller does not hold the Manage System Configuration permission.");
        }
    }

    private static HashSet<int> GetEffectivePermissionIds(User user)
    {
        var rolePermissionIds = user.Role.RolePermissions?.Select(rp => rp.PermissionId) ?? Enumerable.Empty<int>();
        var userPermissionIds = user.UserPermissions?.Select(up => up.PermissionId) ?? Enumerable.Empty<int>();
        var groupPermissionIds = user.UserGroups?.SelectMany(ug => ug.Group.GroupPermissions.Select(gp => gp.PermissionId)) ?? Enumerable.Empty<int>();
        return new HashSet<int>(rolePermissionIds.Concat(userPermissionIds).Concat(groupPermissionIds));
    }
}
