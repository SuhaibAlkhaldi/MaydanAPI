using Maydan.Application.DTOs.Common;
using Maydan.Application.DTOs.Services;
using Maydan.Application.Interfaces;
using Maydan.Domain.Entities;
using Maydan.Domain.Enums;

namespace Maydan.Application.Services;

public class ServiceConfigurationService : IServiceConfigurationService
{
    private const int ManageSystemConfigurationPermissionId = 38;

    private readonly IUnitOfWork _unitOfWork;

    public ServiceConfigurationService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<List<ServiceDto>>> GetAllServicesAsync(int currentUserId, CancellationToken cancellationToken = default)
    {
        var authError = await EnsureCallerCanManageAsync<List<ServiceDto>>(currentUserId, cancellationToken);
        if (authError is not null)
        {
            return authError;
        }

        var services = await _unitOfWork.Services.GetAllAsync(cancellationToken);
        return ApiResponse<List<ServiceDto>>.SuccessResponse(services.Select(MapToDto).ToList(), "Services retrieved successfully.", "تم جلب الخدمات بنجاح.");
    }

    public async Task<ApiResponse<List<ServiceDto>>> GetActiveServicesAsync(CancellationToken cancellationToken = default)
    {
        var services = await _unitOfWork.Services.GetActiveAsync(cancellationToken);
        return ApiResponse<List<ServiceDto>>.SuccessResponse(services.Select(MapToDto).ToList(), "Active services retrieved successfully.", "تم جلب الخدمات المتاحة بنجاح.");
    }

    public async Task<ApiResponse<ServiceDto>> GetServiceByIdAsync(int currentUserId, int id, CancellationToken cancellationToken = default)
    {
        var authError = await EnsureCallerCanManageAsync<ServiceDto>(currentUserId, cancellationToken);
        if (authError is not null)
        {
            return authError;
        }

        var service = await _unitOfWork.Services.GetByIdAsync(id, cancellationToken);
        if (service is null)
        {
            return ApiResponse<ServiceDto>.FailureResponse("Service not found.", "الخدمة غير موجودة.");
        }

        return ApiResponse<ServiceDto>.SuccessResponse(MapToDto(service), "Service retrieved successfully.", "تم جلب الخدمة بنجاح.");
    }

    public async Task<ApiResponse<ServiceDto>> CreateServiceAsync(int currentUserId, CreateServiceDto dto, CancellationToken cancellationToken = default)
    {
        var authError = await EnsureCallerCanManageAsync<ServiceDto>(currentUserId, cancellationToken);
        if (authError is not null)
        {
            return authError;
        }

        var validationError = ValidatePayload<ServiceDto>(dto.NameEn, dto.NameAr, dto.Price, dto.CalculationType);
        if (validationError is not null)
        {
            return validationError;
        }

        if (await _unitOfWork.Services.FindByNameEnAsync(dto.NameEn.Trim(), cancellationToken) is not null)
        {
            return ApiResponse<ServiceDto>.FailureResponse("English service name already exists.", "اسم الخدمة الإنجليزي موجود بالفعل.");
        }

        if (await _unitOfWork.Services.FindByNameArAsync(dto.NameAr.Trim(), cancellationToken) is not null)
        {
            return ApiResponse<ServiceDto>.FailureResponse("Arabic service name already exists.", "اسم الخدمة العربي موجود بالفعل.");
        }

        var service = new Service
        {
            NameEn = dto.NameEn.Trim(),
            NameAr = dto.NameAr.Trim(),
            CalculationType = dto.CalculationType,
            Price = dto.Price,
            RequiresDocuments = dto.RequiresDocuments,
            IsActive = true
        };

        await _unitOfWork.Services.AddAsync(service, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<ServiceDto>.SuccessResponse(MapToDto(service), "Service created successfully.", "تم إنشاء الخدمة بنجاح.", 201);
    }

    public async Task<ApiResponse<ServiceDto>> UpdateServiceAsync(int currentUserId, int id, UpdateServiceDto dto, CancellationToken cancellationToken = default)
    {
        var authError = await EnsureCallerCanManageAsync<ServiceDto>(currentUserId, cancellationToken);
        if (authError is not null)
        {
            return authError;
        }

        var service = await _unitOfWork.Services.GetByIdAsync(id, cancellationToken);
        if (service is null)
        {
            return ApiResponse<ServiceDto>.FailureResponse("Service not found.", "الخدمة غير موجودة.");
        }

        if (dto.NameEn is not null)
        {
            if (string.IsNullOrWhiteSpace(dto.NameEn))
            {
                return ApiResponse<ServiceDto>.FailureResponse("NameEn is required.", "اسم الخدمة بالإنجليزية مطلوب.");
            }

            var nameEn = dto.NameEn.Trim();
            var otherByEn = await _unitOfWork.Services.FindByNameEnAsync(nameEn, cancellationToken);
            if (otherByEn is not null && otherByEn.Id != id)
            {
                return ApiResponse<ServiceDto>.FailureResponse("English service name already exists.", "اسم الخدمة الإنجليزي موجود بالفعل.");
            }

            service.NameEn = nameEn;
        }

        if (dto.NameAr is not null)
        {
            if (string.IsNullOrWhiteSpace(dto.NameAr))
            {
                return ApiResponse<ServiceDto>.FailureResponse("NameAr is required.", "اسم الخدمة بالعربية مطلوب.");
            }

            var nameAr = dto.NameAr.Trim();
            var otherByAr = await _unitOfWork.Services.FindByNameArAsync(nameAr, cancellationToken);
            if (otherByAr is not null && otherByAr.Id != id)
            {
                return ApiResponse<ServiceDto>.FailureResponse("Arabic service name already exists.", "اسم الخدمة العربي موجود بالفعل.");
            }

            service.NameAr = nameAr;
        }

        if (dto.Price is not null)
        {
            if (dto.Price <= 0)
            {
                return ApiResponse<ServiceDto>.FailureResponse("Price must be greater than zero.", "يجب أن يكون السعر أكبر من صفر.");
            }

            service.Price = dto.Price.Value;
        }

        if (dto.CalculationType is not null)
        {
            if (!Enum.IsDefined(typeof(CalculationType), dto.CalculationType.Value))
            {
                return ApiResponse<ServiceDto>.FailureResponse("Invalid CalculationType.", "نوع الحساب غير صالح.");
            }

            service.CalculationType = dto.CalculationType.Value;
        }

        if (dto.RequiresDocuments is not null)
        {
            service.RequiresDocuments = dto.RequiresDocuments.Value;
        }

        if (dto.IsActive is not null)
        {
            service.IsActive = dto.IsActive.Value;
        }

        _unitOfWork.Services.Update(service);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<ServiceDto>.SuccessResponse(MapToDto(service), "Service updated successfully.", "تم تحديث الخدمة بنجاح.");
    }

    public async Task<ApiResponse<object?>> DeleteServiceAsync(int currentUserId, int id, CancellationToken cancellationToken = default)
    {
        var authError = await EnsureCallerCanManageAsync<object?>(currentUserId, cancellationToken);
        if (authError is not null)
        {
            return authError;
        }

        var service = await _unitOfWork.Services.GetByIdAsync(id, cancellationToken);
        if (service is null)
        {
            return ApiResponse<object?>.FailureResponse("Service not found.", "الخدمة غير موجودة.");
        }

        service.IsActive = false;
        _unitOfWork.Services.Remove(service);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<object?>.SuccessResponse(null, "Service deleted successfully.", "تم حذف الخدمة بنجاح.");
    }




    #region Helpers
    private static ServiceDto MapToDto(Service s) => new(s.Id, s.NameEn, s.NameAr, s.CalculationType, s.Price, s.RequiresDocuments, s.IsActive);

    private static ApiResponse<T>? ValidatePayload<T>(string nameEn, string nameAr, decimal price, CalculationType calculationType)
    {
        if (string.IsNullOrWhiteSpace(nameEn))
        {
            return ApiResponse<T>.FailureResponse("NameEn is required.", "اسم الخدمة بالإنجليزية مطلوب.");
        }

        if (string.IsNullOrWhiteSpace(nameAr))
        {
            return ApiResponse<T>.FailureResponse("NameAr is required.", "اسم الخدمة بالعربية مطلوب.");
        }

        if (price <= 0)
        {
            return ApiResponse<T>.FailureResponse("Price must be greater than zero.", "يجب أن يكون السعر أكبر من صفر.");
        }

        if (!Enum.IsDefined(typeof(CalculationType), calculationType))
        {
            return ApiResponse<T>.FailureResponse("Invalid CalculationType.", "نوع الحساب غير صالح.");
        }

        return null;
    }


    private async Task<ApiResponse<T>?> EnsureCallerCanManageAsync<T>(int currentUserId, CancellationToken cancellationToken)
    {
        var currentUser = await _unitOfWork.Users.GetWithPermissionsAsync(currentUserId, cancellationToken);
        if (currentUser is null)
        {
            return ApiResponse<T>.FailureResponse("Current user was not found.", "المستخدم الحالي غير موجود.");
        }

        if (!currentUser.IsActive)
        {
            return ApiResponse<T>.FailureResponse("Current user is inactive.", "المستخدم الحالي غير فعّال.");
        }

        if (currentUser.EntityType != EntityType.BaytAlUrdon)
        {
            return ApiResponse<T>.FailureResponse("Only Bayt-AlUrdon staff can manage the system configuration.", "فقط موظفو بيت الأردن يمكنهم إدارة إعدادات النظام.");
        }

        var effective = GetEffectivePermissionIds(currentUser);
        if (!effective.Contains(ManageSystemConfigurationPermissionId))
        {
            return ApiResponse<T>.FailureResponse("Caller does not hold the Manage System Configuration permission.", "لا تملك صلاحية إدارة إعدادات النظام.");
        }

        return null;
    }

    private static HashSet<int> GetEffectivePermissionIds(User user)
    {
        var rolePermissionIds = user.Role.RolePermissions?.Select(rp => rp.PermissionId) ?? Enumerable.Empty<int>();
        var userPermissionIds = user.UserPermissions?.Select(up => up.PermissionId) ?? Enumerable.Empty<int>();
        var groupPermissionIds = user.UserGroups?.SelectMany(ug => ug.Group.GroupPermissions.Select(gp => gp.PermissionId)) ?? Enumerable.Empty<int>();
        return new HashSet<int>(rolePermissionIds.Concat(userPermissionIds).Concat(groupPermissionIds));
    }
    #endregion
}
