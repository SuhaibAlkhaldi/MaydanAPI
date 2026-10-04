using System.Text.RegularExpressions;
using Maydan.Application.Common;
using Maydan.Application.DTOs.Common;
using Maydan.Application.DTOs.Workers;
using Maydan.Application.Interfaces;
using Maydan.Domain.Entities;
using Maydan.Domain.Enums;

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

        return Ok(new PagedResult<WorkerSummaryDto>(items, totalCount, safePage, safePageSize), "تم جلب العمال بنجاح.", "Workers retrieved successfully.");
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
            return Fail<WorkerDto>("العامل غير موجود.", "Worker not found.");
        }

        var scopeError = EnsureWithinScope<WorkerDto>(currentUser!, worker.AssociationId);
        if (scopeError is not null)
        {
            return scopeError;
        }

        return Ok(worker, "تم جلب بيانات العامل بنجاح.", "Worker retrieved successfully.");
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
            return Fail<WorkerDto>("الاسم الأول والأخير مطلوبان.", "First and last name are required.");
        }

        if (string.IsNullOrWhiteSpace(dto.CivilId))
        {
            return Fail<WorkerDto>("الرقم الوطني مطلوب.", "National ID is required.");
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
            return Fail<WorkerDto>("العامل مسجل مسبقًا بنفس الرقم الوطني.", "Worker already exists.");
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
        return Ok(created!, "تم تسجيل العامل بنجاح.", "Worker registered successfully.", statusCode: 201);
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
            return Fail<WorkerDto>("الاسم الأول والأخير مطلوبان.", "First and last name are required.");
        }

        var phoneServiceError = ValidatePhoneAndServices<WorkerDto>(dto.PhoneNumber, dto.ServiceIds);
        if (phoneServiceError is not null)
        {
            return phoneServiceError;
        }

        // Include(WorkerServices) here on purpose — this is a write that mutates the related
        // collection in place, the documented exception to preferring Select projections.
        var worker = await _unitOfWork.Workers.GetByIdWithServicesAsync(workerId, cancellationToken);
        if (worker is null)
        {
            return Fail<WorkerDto>("العامل غير موجود.", "Worker not found.");
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

        // AssociationId and CivilId are deliberately never touched here — UpdateWorkerDto doesn't
        // even carry them (see its own comment). One worker/one association and a stable national
        // ID are permanent facts once the worker is created.
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
        return Ok(updated!, "تم تحديث بيانات العامل بنجاح.", "Worker updated successfully.");
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
            return Fail<object?>("العامل غير موجود.", "Worker not found.");
        }

        var scopeError = EnsureWithinScope<object?>(currentUser!, worker.AssociationId);
        if (scopeError is not null)
        {
            return scopeError;
        }

        
        _unitOfWork.Workers.Remove(worker);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok<object?>(null, "تم حذف العامل بنجاح.", "Worker deleted successfully.");
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
            return Fail<WorkerDto>("العامل غير موجود.", "Worker not found.");
        }

        var scopeError = EnsureWithinScope<WorkerDto>(currentUser!, worker.AssociationId);
        if (scopeError is not null)
        {
            return scopeError;
        }

        if (!worker.IsDeleted)
        {
            return Fail<WorkerDto>("العامل غير محذوف أصلًا.", "Worker is not deleted.");
        }

        worker.IsDeleted = false;
        worker.DeletedAt = null;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var restored = await _unitOfWork.Workers.GetByIdProjectedAsync(worker.Id, cancellationToken);
        return Ok(restored!, "تم استرجاع العامل بنجاح.", "Worker restored successfully.");
    }




    #region Helpers
    private async Task<(User? User, ApiResponse<T>? Error)> AuthorizeAsync<T>(int currentUserId, int[] anyOfPermissionIds, CancellationToken cancellationToken)
    {
        var currentUser = await _unitOfWork.Users.GetWithPermissionsAsync(currentUserId, cancellationToken);
        if (currentUser is null)
        {
            return (null, Fail<T>("المستخدم الحالي غير موجود.", "Current user was not found."));
        }

        if (!currentUser.IsActive)
        {
            return (null, Fail<T>("المستخدم الحالي غير فعّال.", "Current user is inactive."));
        }

        if (!GetEffectivePermissionIds(currentUser).Overlaps(anyOfPermissionIds))
        {
            return (null, Fail<T>("لا تملك الصلاحية المطلوبة لإدارة العمال.", "You do not hold the required Workers permission."));
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
            return (0, Fail<T>("يجب اختيار الجمعية عند إضافة عامل.", "You must select an association."));
        }

        var association = await _unitOfWork.Associations.GetByIdAsync(requestedAssociationId.Value, cancellationToken);
        if (association is null)
        {
            return (0, Fail<T>("الجمعية غير موجودة.", "Association not found."));
        }

        return (association.Id, null);
    }

    
    private static ApiResponse<T>? EnsureWithinScope<T>(User currentUser, int workerAssociationId)
    {
        if (currentUser.EntityType == EntityType.Association && workerAssociationId != currentUser.EntityId)
        {
            return Fail<T>("لا يمكنك الوصول إلى عامل خارج جمعيتك.", "Cannot access a worker outside your own association.");
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
            return Fail<T>("الدولة غير موجودة.", "Country not found.");
        }

        if (await _unitOfWork.Cities.GetByIdAsync(cityId, cancellationToken) is null)
        {
            return Fail<T>("المدينة غير موجودة.", "City not found.");
        }

        return null;
    }

    private async Task<ApiResponse<T>?> EnsureServicesExistAsync<T>(List<int> serviceIds, CancellationToken cancellationToken)
    {
        foreach (var serviceId in serviceIds)
        {
            if (await _unitOfWork.Services.GetByIdAsync(serviceId, cancellationToken) is null)
            {
                return Fail<T>("إحدى الخدمات المحددة غير موجودة.", "One or more selected services were not found.");
            }
        }

        return null;
    }

    private static ApiResponse<T>? ValidatePhoneAndServices<T>(string phoneNumber, List<int> serviceIds)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber) || !JordanMobileRegex.IsMatch(phoneNumber.Trim()))
        {
            return Fail<T>("رقم الهاتف غير صالح، يجب أن يكون رقم أردني.", "Phone number must be a valid Jordanian mobile number.");
        }

        if (serviceIds is null || serviceIds.Count == 0)
        {
            return Fail<T>("يجب اختيار خدمة واحدة على الأقل.", "At least one service must be selected.");
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

    
    private static ApiResponse<T> Fail<T>(string messageAr, string messageEn) =>
        new(false, messageAr, messageEn, default, 400);

    private static ApiResponse<T> Ok<T>(T data, string messageAr, string messageEn, int statusCode = 200) =>
        new(true, messageAr, messageEn, data, statusCode);

    #endregion
}
