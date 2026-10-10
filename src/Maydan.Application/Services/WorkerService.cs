using Maydan.Application.Common;
using Maydan.Application.DTOs.Common;
using Maydan.Application.DTOs.Workers;
using Maydan.Application.Interfaces;
using Maydan.Domain.Entities;
using Maydan.Domain.Enums;
using System.Text.RegularExpressions;
using System.Threading;

namespace Maydan.Application.Services;

public class WorkerService : IWorkerService
{
    private const int ViewWorkersPermissionId = 23;
    private const int ManageWorkersPermissionId = 24;

    private static readonly int[] ReadScope = { ViewWorkersPermissionId, ManageWorkersPermissionId };
    private static readonly int[] WriteScope = { ManageWorkersPermissionId };

    private static readonly Regex JordanMobileRegex = new(@"^(\+962|0)7[789]\d{7}$", RegexOptions.Compiled);

    private readonly IUnitOfWork _unitOfWork;
    private readonly ICivilIdHasher _civilIdHasher;

    public WorkerService(IUnitOfWork unitOfWork, ICivilIdHasher civilIdHasher)
    {
        _unitOfWork = unitOfWork;
        _civilIdHasher = civilIdHasher;
    }

    public async Task<ApiResponse<PagedResult<WorkerSummaryDto>>> GetAllAsync(int currentUserId, string? search, int? serviceId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var (currentUser, authError) = await AuthorizeAsync<PagedResult<WorkerSummaryDto>>(currentUserId, ReadScope, cancellationToken);
        if (authError is not null)
        {
            return authError;
        }

        var (safePage, safePageSize) = Paging.Normalize(page, pageSize);

        var associationScope = currentUser!.EntityType == EntityType.Association ? currentUser.EntityId : (int?)null;
        var (items, totalCount) = await _unitOfWork.Workers.GetAllProjectedAsync(associationScope, search, serviceId, safePage, safePageSize, cancellationToken);

        return ApiResponse<PagedResult<WorkerSummaryDto>>.SuccessResponse(
            new PagedResult<WorkerSummaryDto>(items, totalCount, safePage, safePageSize),
            "Workers retrieved successfully.",
            "تم جلب العمال بنجاح.");
    }

    public async Task<ApiResponse<WorkerDto>> GetByIdAsync(int currentUserId, int workerId, CancellationToken cancellationToken = default)
    {
        var (currentUser, authError) = await AuthorizeAsync<WorkerDto>(currentUserId, ReadScope, cancellationToken);
        if (authError is not null)
        {
            return authError;
        }

        var worker = await _unitOfWork.Workers.GetByIdProjectedAsync(workerId, cancellationToken);
        if (worker is null)
        {
            return ApiResponse<WorkerDto>.FailureResponse("Worker not found.", "العامل غير موجود.");
        }

        var scopeError = EnsureWithinScope<WorkerDto>(currentUser!, worker.AssociationId);
        if (scopeError is not null)
        {
            return scopeError;
        }

        return ApiResponse<WorkerDto>.SuccessResponse(worker, "Worker retrieved successfully.", "تم جلب بيانات العامل بنجاح.");
    }

    public async Task<ApiResponse<WorkerDto>> CreateAsync(int currentUserId, CreateWorkerDto dto, CancellationToken cancellationToken = default)
    {
        var (currentUser, authError) = await AuthorizeAsync<WorkerDto>(currentUserId, WriteScope, cancellationToken);
        if (authError is not null)
        {
            return authError;
        }

        if (string.IsNullOrWhiteSpace(dto.FirstName) || string.IsNullOrWhiteSpace(dto.LastName))
        {
            return ApiResponse<WorkerDto>.FailureResponse("First and last name are required.", "الاسم الأول والأخير مطلوبان.");
        }

        if (string.IsNullOrWhiteSpace(dto.CivilId))
        {
            return ApiResponse<WorkerDto>.FailureResponse("National ID is required.", "الرقم الوطني مطلوب.");
        }

        var phoneServiceError = ValidatePhoneAndServices<WorkerDto>(dto.PhoneNumber, dto.ServiceIds);
        if (phoneServiceError is not null)
        {
            return phoneServiceError;
        }

        var (associationId, associationError) = await ResolveAssociationIdAsync<WorkerDto>(currentUser!, dto.AssociationId, cancellationToken);
        if (associationError is not null)
        {
            return associationError;
        }

        var civilId = dto.CivilId.Trim();
        var civilIdHash = _civilIdHasher.ComputeHash(civilId);
        if (await _unitOfWork.Workers.GetByCivilIdHashAsync(civilIdHash, cancellationToken) is not null)
        {
            return ApiResponse<WorkerDto>.FailureResponse("Worker already exists.", "العامل مسجل مسبقًا بنفس الرقم الوطني.");
        }

        var countryCityError = await EnsureCountryAndCityExistAsync<WorkerDto>(dto.CountryId, dto.CityId, cancellationToken);
        if (countryCityError is not null)
        {
            return countryCityError;
        }

        var serviceIds = dto.ServiceIds.Distinct().ToList();
        var servicesError = await EnsureServicesExistAsync<WorkerDto>(serviceIds, cancellationToken);
        if (servicesError is not null)
        {
            return servicesError;
        }

        var worker = new Worker
        {
            FirstName = dto.FirstName.Trim(),
            MiddleName = string.IsNullOrWhiteSpace(dto.MiddleName) ? null : dto.MiddleName.Trim(),
            LastName = dto.LastName.Trim(),
            CivilId = civilId,
            CivilIdHash = civilIdHash,
            DateOfBirth = dto.DateOfBirth,
            Gender = dto.Gender,
            MaritalStatus = dto.MaritalStatus,
            Nationality = dto.Nationality.Trim(),
            CountryId = dto.CountryId,
            CityId = dto.CityId,
            PhoneNumber = dto.PhoneNumber.Trim(),
            YearsOfExperience = dto.YearsOfExperience,
            AssociationId = associationId,
            QrCode = Guid.NewGuid().ToString("N"),
            IsActive = true
        };

        foreach (var serviceId in serviceIds)
        {
            worker.WorkerServices.Add(new WorkerServiceLink { ServiceId = serviceId });
        }

        await _unitOfWork.Workers.AddAsync(worker, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var created = await _unitOfWork.Workers.GetByIdProjectedAsync(worker.Id, cancellationToken);
        return ApiResponse<WorkerDto>.SuccessResponse(created!, "Worker registered successfully.", "تم تسجيل العامل بنجاح.", 201);
    }

    public async Task<ApiResponse<WorkerDto>> UpdateAsync(int currentUserId, int workerId, UpdateWorkerDto dto, CancellationToken cancellationToken = default)
    {
        var (currentUser, authError) = await AuthorizeAsync<WorkerDto>(currentUserId, WriteScope, cancellationToken);
        if (authError is not null)
        {
            return authError;
        }

        if (string.IsNullOrWhiteSpace(dto.FirstName) || string.IsNullOrWhiteSpace(dto.LastName))
        {
            return ApiResponse<WorkerDto>.FailureResponse("First and last name are required.", "الاسم الأول والأخير مطلوبان.");
        }

        var phoneServiceError = ValidatePhoneAndServices<WorkerDto>(dto.PhoneNumber, dto.ServiceIds);
        if (phoneServiceError is not null)
        {
            return phoneServiceError;
        }

        var worker = await _unitOfWork.Workers.GetByIdWithServicesAsync(workerId, cancellationToken);
        if (worker is null)
        {
            return ApiResponse<WorkerDto>.FailureResponse("Worker not found.", "العامل غير موجود.");
        }

        var scopeError = EnsureWithinScope<WorkerDto>(currentUser!, worker.AssociationId);
        if (scopeError is not null)
        {
            return scopeError;
        }

        var countryCityError = await EnsureCountryAndCityExistAsync<WorkerDto>(dto.CountryId, dto.CityId, cancellationToken);
        if (countryCityError is not null)
        {
            return countryCityError;
        }

        var serviceIds = dto.ServiceIds.Distinct().ToList();
        var servicesError = await EnsureServicesExistAsync<WorkerDto>(serviceIds, cancellationToken);
        if (servicesError is not null)
        {
            return servicesError;
        }

        worker.FirstName = dto.FirstName.Trim();
        worker.MiddleName = string.IsNullOrWhiteSpace(dto.MiddleName) ? null : dto.MiddleName.Trim();
        worker.LastName = dto.LastName.Trim();
        worker.DateOfBirth = dto.DateOfBirth;
        worker.Gender = dto.Gender;
        worker.MaritalStatus = dto.MaritalStatus;
        worker.Nationality = dto.Nationality.Trim();
        worker.CountryId = dto.CountryId;
        worker.CityId = dto.CityId;
        worker.PhoneNumber = dto.PhoneNumber.Trim();
        worker.YearsOfExperience = dto.YearsOfExperience;

        SyncWorkerServices(worker, serviceIds);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var updated = await _unitOfWork.Workers.GetByIdProjectedAsync(worker.Id, cancellationToken);
        return ApiResponse<WorkerDto>.SuccessResponse(updated!, "Worker updated successfully.", "تم تحديث بيانات العامل بنجاح.");
    }

    public async Task<ApiResponse<object?>> DeleteAsync(int currentUserId, int workerId, CancellationToken cancellationToken = default)
    {
        var (currentUser, authError) = await AuthorizeAsync<object?>(currentUserId, WriteScope, cancellationToken);
        if (authError is not null)
        {
            return authError;
        }

        var worker = await _unitOfWork.Workers.GetByIdAsync(workerId, cancellationToken);
        if (worker is null)
        {
            return ApiResponse<object?>.FailureResponse("Worker not found.", "العامل غير موجود.");
        }

        var scopeError = EnsureWithinScope<object?>(currentUser!, worker.AssociationId);
        if (scopeError is not null)
        {
            return scopeError;
        }

        _unitOfWork.Workers.Remove(worker);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<object?>.SuccessResponse(null, "Worker deleted successfully.", "تم حذف العامل بنجاح.");
    }

    public async Task<ApiResponse<WorkerDto>> RestoreAsync(int currentUserId, int workerId, CancellationToken cancellationToken = default)
    {
        var (currentUser, authError) = await AuthorizeAsync<WorkerDto>(currentUserId, WriteScope, cancellationToken);
        if (authError is not null)
        {
            return authError;
        }

        var worker = await _unitOfWork.Workers.GetByIdIncludingDeletedAsync(workerId, cancellationToken);
        if (worker is null)
        {
            return ApiResponse<WorkerDto>.FailureResponse("Worker not found.", "العامل غير موجود.");
        }

        var scopeError = EnsureWithinScope<WorkerDto>(currentUser!, worker.AssociationId);
        if (scopeError is not null)
        {
            return scopeError;
        }

        if (!worker.IsDeleted)
        {
            return ApiResponse<WorkerDto>.FailureResponse("Worker is not deleted.", "العامل غير محذوف أصلًا.");
        }

        worker.IsDeleted = false;
        worker.DeletedAt = null;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var restored = await _unitOfWork.Workers.GetByIdProjectedAsync(worker.Id, cancellationToken);
        return ApiResponse<WorkerDto>.SuccessResponse(restored!, "Worker restored successfully.", "تم استرجاع العامل بنجاح.");
    }

    private async Task<(User? User, ApiResponse<T>? Error)> AuthorizeAsync<T>(int currentUserId, int[] anyOfPermissionIds, CancellationToken cancellationToken)
    {
        var currentUser = await _unitOfWork.Users.GetWithPermissionsAsync(currentUserId, cancellationToken);
        if (currentUser is null)
        {
            return (null, ApiResponse<T>.FailureResponse("Current user was not found.", "المستخدم الحالي غير موجود."));
        }

        if (!currentUser.IsActive)
        {
            return (null, ApiResponse<T>.FailureResponse("Current user is inactive.", "المستخدم الحالي غير فعّال."));
        }

        if (!GetEffectivePermissionIds(currentUser).Overlaps(anyOfPermissionIds))
        {
            return (null, ApiResponse<T>.FailureResponse("You do not hold the required Workers permission.", "لا تملك الصلاحية المطلوبة لإدارة العمال."));
        }

        return (currentUser, null);
    }

    private async Task<(int AssociationId, ApiResponse<T>? Error)> ResolveAssociationIdAsync<T>(User currentUser, int? requestedAssociationId, CancellationToken cancellationToken)
    {
        if (currentUser.EntityType == EntityType.Association)
        {
            return (currentUser.EntityId, null);
        }

        if (!requestedAssociationId.HasValue)
        {
            return (0, ApiResponse<T>.FailureResponse("You must select an association.", "يجب اختيار الجمعية عند إضافة عامل."));
        }

        var association = await _unitOfWork.Associations.GetByIdAsync(requestedAssociationId.Value, cancellationToken);
        if (association is null)
        {
            return (0, ApiResponse<T>.FailureResponse("Association not found.", "الجمعية غير موجودة."));
        }

        return (association.Id, null);
    }

    private static ApiResponse<T>? EnsureWithinScope<T>(User currentUser, int workerAssociationId)
    {
        if (currentUser.EntityType == EntityType.Association && workerAssociationId != currentUser.EntityId)
        {
            return ApiResponse<T>.FailureResponse("Cannot access a worker outside your own association.", "لا يمكنك الوصول إلى عامل خارج جمعيتك.");
        }

        return null;
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

    private async Task<ApiResponse<T>?> EnsureCountryAndCityExistAsync<T>(int countryId, int cityId, CancellationToken cancellationToken)
    {
        if (await _unitOfWork.Countries.GetByIdAsync(countryId, cancellationToken) is null)
        {
            return ApiResponse<T>.FailureResponse("Country not found.", "الدولة غير موجودة.");
        }

        if (await _unitOfWork.Cities.GetByIdAsync(cityId, cancellationToken) is null)
        {
            return ApiResponse<T>.FailureResponse("City not found.", "المدينة غير موجودة.");
        }

        return null;
    }

    private async Task<ApiResponse<T>?> EnsureServicesExistAsync<T>(List<int> serviceIds, CancellationToken cancellationToken)
    {
        foreach (var serviceId in serviceIds)
        {
            if (await _unitOfWork.Services.GetByIdAsync(serviceId, cancellationToken) is null)
            {
                return ApiResponse<T>.FailureResponse("One or more selected services were not found.", "إحدى الخدمات المحددة غير موجودة.");
            }
        }

        return null;
    }

    private static ApiResponse<T>? ValidatePhoneAndServices<T>(string phoneNumber, List<int> serviceIds)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber) || !JordanMobileRegex.IsMatch(phoneNumber.Trim()))
        {
            return ApiResponse<T>.FailureResponse("Phone number must be a valid Jordanian mobile number.", "رقم الهاتف غير صالح، يجب أن يكون رقم أردني.");
        }

        if (serviceIds.Count == 0)
        {
            return ApiResponse<T>.FailureResponse("At least one service must be selected.", "يجب اختيار خدمة واحدة على الأقل.");
        }

        return null;
    }

    private static void SyncWorkerServices(Worker worker, List<int> serviceIds)
    {
        var requestedIds = serviceIds.ToHashSet();
        var existingIds = worker.WorkerServices.Select(ws => ws.ServiceId).ToHashSet();

        foreach (var link in worker.WorkerServices.Where(ws => !requestedIds.Contains(ws.ServiceId)).ToList())
        {
            worker.WorkerServices.Remove(link);
        }

        foreach (var serviceId in requestedIds.Except(existingIds))
        {
            worker.WorkerServices.Add(new WorkerServiceLink { WorkerId = worker.Id, ServiceId = serviceId });
        }
    }

    private async Task<(User? CurrentUser, ApiResponse<T>? Error)> GetCurrentUserAsync<T>(int currentUserId, CancellationToken cancellationToken)
    {
        var currentUser = await _unitOfWork.Users.GetWithPermissionsAsync(currentUserId, cancellationToken);
        if (currentUser is null) { return (null, ApiResponse<T>.FailureResponse("لم يتم العثور على المستخدم الحالي.", "Current user was not found.")); }
        if (!currentUser.IsActive)
        {
            return (null, ApiResponse<T>.FailureResponse("حساب المستخدم الحالي غير نشط.", "Current user is inactive."));
        }
        return (currentUser, null);
    }
    public async Task<ApiResponse<List<WorkerDto>>> GetAvailableWorkersForRequestAsync(
     int currentUserId,
     int serviceRequestId,
     CancellationToken cancellationToken = default)
    {
        var request = await _unitOfWork.ServiceRequests
            .GetByIdAsync(serviceRequestId, cancellationToken);

        if (request is null)
        {
            return ApiResponse<List<WorkerDto>>.FailureResponse(
                "Request not found.",
                "الطلب غير موجود.");
        }

        // Check that the current user is an Association admin
        // belonging to the same association as the service request.
        var (currentUser, authError) =
            await GetCurrentUserAsync<List<WorkerDto>>(
                currentUserId,
                cancellationToken);

        if (authError is not null)
            return authError;

        if (currentUser!.EntityType != EntityType.Association ||
            currentUser.EntityId != request.AssociationId)
        {
            return ApiResponse<List<WorkerDto>>.FailureResponse(
                "You are not authorized to view workers for this service request.",
                "ليس لديك صلاحية لعرض عمال طلب الخدمة هذا.");
        }

        var availableWorkers =
            await _unitOfWork.Workers.GetAvailableWorkersForRequestAsync(
                request.AssociationId,
                request.ServiceId,
                request.StartDate,
                request.EndDate,
                cancellationToken);

        var workers = availableWorkers
            .Select(w => new WorkerDto(
                w.Id,
                w.FirstName,
                w.MiddleName,
                w.LastName,
                w.CivilId,
                w.DateOfBirth,
                w.Gender,
                w.MaritalStatus,
                w.Nationality,
                w.CountryId,
                w.Country?.EnglishName,
                w.CityId,
                w.City?.EnglishName,
                w.PhoneNumber,
                w.YearsOfExperience,
                w.AssociationId,
                w.Association.EnglishName ?? string.Empty,
                w.QrCode,
                w.WorkerServices
                    .Select(ws => ws.ServiceId)
                    .ToList(),
                w.WorkerServices
                    .Select(ws => ws.Service.NameEn)
                    .ToList(),
                w.IsActive
            ))
            .ToList();

        return ApiResponse<List<WorkerDto>>.SuccessResponse(
            workers,
            "Available workers retrieved successfully.",
            "تم جلب العمال المتاحين بنجاح."
        );
    }
}
