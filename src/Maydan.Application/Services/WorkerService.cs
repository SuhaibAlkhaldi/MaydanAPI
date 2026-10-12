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

        var response = await _unitOfWork.Workers.GetAllProjectedAsync(associationScope, search, serviceId, safePage, safePageSize, cancellationToken);

        var (items, totalCount) = response.Data;



        return ApiResponse<PagedResult<WorkerSummaryDto>>.SuccessResponse(

            new PagedResult<WorkerSummaryDto>(items, totalCount, safePage, safePageSize),

            "تم جلب العمال بنجاح.",

            "Workers retrieved successfully.");

    }



    public async Task<ApiResponse<WorkerDto>> GetByIdAsync(

        int currentUserId,

        int workerId,

        CancellationToken cancellationToken = default)

    {

        var (currentUser, authError) = await AuthorizeAsync<WorkerDto>(currentUserId, ReadScope, cancellationToken);

        if (authError is not null)

        {

            return authError;

        }



        var response = await _unitOfWork.Workers.GetByIdProjectedAsync(workerId, cancellationToken);

        var worker = response.Data;

        if (worker is null)

        {

            return ApiResponse<WorkerDto>.FailureResponse("العامل غير موجود.", "Worker not found.");

        }



        var scopeError = EnsureWithinScope<WorkerDto>(currentUser!, worker.AssociationId);

        if (scopeError is not null)

        {

            return scopeError;

        }



        return ApiResponse<WorkerDto>.SuccessResponse(worker, "تم جلب بيانات العامل بنجاح.", "Worker retrieved successfully.");

    }



    public async Task<ApiResponse<WorkerDto>> GetByCivilIdAsync(

        int currentUserId,

        string civilId,

        CancellationToken cancellationToken = default)

    {

        var (currentUser, authError) = await AuthorizeAsync<WorkerDto>(currentUserId, ReadScope, cancellationToken);

        if (authError is not null)

        {

            return authError;

        }



        if (string.IsNullOrWhiteSpace(civilId))

        {

            return ApiResponse<WorkerDto>.FailureResponse("الرقم الوطني مطلوب.", "National ID is required.");

        }



        var civilIdHash = _civilIdHasher.ComputeHash(civilId.Trim());

        var workerEntityResponse = await _unitOfWork.Workers.GetByCivilIdHashAsync(civilIdHash, cancellationToken);

        var workerEntity = workerEntityResponse.Data;

        if (workerEntity is null)

        {

            return ApiResponse<WorkerDto>.FailureResponse("العامل غير موجود.", "Worker not found.");

        }



        var scopeError = EnsureWithinScope<WorkerDto>(currentUser!, workerEntity.AssociationId);

        if (scopeError is not null)

        {

            return scopeError;

        }



        var workerDtoResponse = await _unitOfWork.Workers.GetByIdProjectedAsync(workerEntity.Id, cancellationToken);

        var workerDto = workerDtoResponse.Data;

        return ApiResponse<WorkerDto>.SuccessResponse(workerDto!, "تم جلب بيانات العامل بنجاح.", "Worker retrieved successfully.");

    }



    public async Task<ApiResponse<List<WorkerSummaryDto>>> GetByAssociationIdAsync(

        int currentUserId,

        int? associationId = null,

        CancellationToken cancellationToken = default)

    {

        var (currentUser, authError) = await AuthorizeAsync<List<WorkerSummaryDto>>(currentUserId, ReadScope, cancellationToken);

        if (authError is not null)

        {

            return authError;

        }



        var (targetAssociationId, associationError) = await ResolveAssociationIdAsync<List<WorkerSummaryDto>>(currentUser!, associationId, cancellationToken);

        if (associationError is not null)

        {

            return associationError;

        }



        var workersResponse = await _unitOfWork.Workers.GetByAssociationIdAsync(targetAssociationId, cancellationToken);

        var workers = workersResponse.Data ?? new List<WorkerSummaryDto>();

        return ApiResponse<List<WorkerSummaryDto>>.SuccessResponse(workers, "تم جلب عمال الجمعية بنجاح.", "Association workers retrieved successfully.");

    }



    public async Task<ApiResponse<List<WorkerSummaryDto>>> GetDeletedByAssociationIdAsync(

        int currentUserId,

        int? associationId = null,

        CancellationToken cancellationToken = default)

    {

        var (currentUser, authError) = await AuthorizeAsync<List<WorkerSummaryDto>>(currentUserId, ReadScope, cancellationToken);

        if (authError is not null)

        {

            return authError;

        }



        var (targetAssociationId, associationError) = await ResolveAssociationIdAsync<List<WorkerSummaryDto>>(currentUser!, associationId, cancellationToken);

        if (associationError is not null)

        {

            return associationError;

        }



        var deletedWorkersResponse = await _unitOfWork.Workers.GetDeletedByAssociationIdAsync(targetAssociationId, cancellationToken);

        var deletedWorkers = deletedWorkersResponse.Data ?? new List<Worker>();

        var dtos = deletedWorkers.Select(w => new WorkerSummaryDto(

            w.Id,

            w.FirstName,

            w.LastName,

            w.PhoneNumber ?? string.Empty,

            w.CivilId,

            w.WorkerServices.Select(ws => ws.Service.NameEn).ToList(),

            w.WorkerServices.Select(ws => ws.Service.NameAr).ToList(),

            w.YearsOfExperience,

            w.Association != null ? (w.Association.ArabicName ?? w.Association.EnglishName) : null

        )).ToList();



        return ApiResponse<List<WorkerSummaryDto>>.SuccessResponse(dtos, "تم جلب العمال المحذوفين للجمعية بنجاح.", "Deleted association workers retrieved successfully.");

    }



    public async Task<ApiResponse<List<WorkerSummaryDto>>> QueryAsync(

        int currentUserId,

        bool isDeleted = false,

        int? associationId = null,

        bool? orderByYearsOfExperienceAscending = null,

        bool? orderByNameEnAscending = null,

        bool? orderByNameArAscending = null,

        CancellationToken cancellationToken = default)

    {

        var (currentUser, authError) = await AuthorizeAsync<List<WorkerSummaryDto>>(currentUserId, ReadScope, cancellationToken);

        if (authError is not null)

        {

            return authError;

        }



        int? targetAssociationId = currentUser!.EntityType == EntityType.Association

            ? currentUser.EntityId

            : associationId;



        var workersResponse = await _unitOfWork.Workers.QueryAsync(

            isDeleted,

            targetAssociationId,

            orderByYearsOfExperienceAscending,

            orderByNameEnAscending,

            orderByNameArAscending,

            cancellationToken);



        var workers = workersResponse.Data ?? new List<Worker>();



        var dtos = workers.Select(w => new WorkerSummaryDto(

            w.Id,

            w.FirstName,

            w.LastName,

            w.PhoneNumber ?? string.Empty,

            w.CivilId,

            w.WorkerServices.Select(ws => ws.Service.NameEn).ToList(),

            w.WorkerServices.Select(ws => ws.Service.NameAr).ToList(),

            w.YearsOfExperience,

            w.Association != null ? (w.Association.ArabicName ?? w.Association.EnglishName) : null

        )).ToList();



        return ApiResponse<List<WorkerSummaryDto>>.SuccessResponse(dtos, "تم جلب العمال بنجاح.", "Workers retrieved successfully.");

    }



    public async Task<ApiResponse<WorkerDto>> CreateAsync(

        int currentUserId,

        CreateWorkerDto dto,

        CancellationToken cancellationToken = default)

    {

        var (currentUser, authError) = await AuthorizeAsync<WorkerDto>(currentUserId, WriteScope, cancellationToken);

        if (authError is not null)

        {

            return authError;

        }



        if (string.IsNullOrWhiteSpace(dto.FirstName) || string.IsNullOrWhiteSpace(dto.LastName))

        {

            return ApiResponse<WorkerDto>.FailureResponse("الاسم الأول والأخير مطلوبان.", "First and last name are required.");

        }



        if (string.IsNullOrWhiteSpace(dto.CivilId))

        {

            return ApiResponse<WorkerDto>.FailureResponse("الرقم الوطني مطلوب.", "National ID is required.");

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

        var existingWorker = await _unitOfWork.Workers.GetByCivilIdHashAsync(civilIdHash, cancellationToken);

        if (existingWorker.Data is not null)

        {

            return ApiResponse<WorkerDto>.FailureResponse("العامل مسجل مسبقًا بنفس الرقم الوطني.", "Worker already exists.");

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



        var createdResponse = await _unitOfWork.Workers.GetByIdProjectedAsync(worker.Id, cancellationToken);

        var created = createdResponse.Data;

        return ApiResponse<WorkerDto>.SuccessResponse(created!, "تم تسجيل العامل بنجاح.", "Worker registered successfully.", 201);

    }



    public async Task<ApiResponse<WorkerDto>> UpdateAsync(

        int currentUserId,

        int workerId,

        UpdateWorkerDto dto,

        CancellationToken cancellationToken = default)

    {

        var (currentUser, authError) = await AuthorizeAsync<WorkerDto>(currentUserId, WriteScope, cancellationToken);

        if (authError is not null)

        {

            return authError;

        }



        if (string.IsNullOrWhiteSpace(dto.FirstName) || string.IsNullOrWhiteSpace(dto.LastName))

        {

            return ApiResponse<WorkerDto>.FailureResponse("الاسم الأول والأخير مطلوبان.", "First and last name are required.");

        }



        var phoneServiceError = ValidatePhoneAndServices<WorkerDto>(dto.PhoneNumber, dto.ServiceIds);

        if (phoneServiceError is not null)

        {

            return phoneServiceError;

        }



        var workerResponse = await _unitOfWork.Workers.GetByIdWithServicesAsync(workerId, cancellationToken);

        var worker = workerResponse.Data;

        if (worker is null)

        {

            return ApiResponse<WorkerDto>.FailureResponse("العامل غير موجود.", "Worker not found.");

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



        var updatedResponse = await _unitOfWork.Workers.GetByIdProjectedAsync(worker.Id, cancellationToken);

        var updated = updatedResponse.Data;

        return ApiResponse<WorkerDto>.SuccessResponse(updated!, "تم تحديث بيانات العامل بنجاح.", "Worker updated successfully.");

    }



    public async Task<ApiResponse<object?>> DeleteAsync(

        int currentUserId,

        int workerId,

        CancellationToken cancellationToken = default)

    {

        var (currentUser, authError) = await AuthorizeAsync<object?>(currentUserId, WriteScope, cancellationToken);

        if (authError is not null)

        {

            return authError;

        }



        var workerResponse = await _unitOfWork.Workers.GetByIdAsync(workerId, cancellationToken);

        var worker = workerResponse.Data;

        if (worker is null)

        {

            return ApiResponse<object?>.FailureResponse("العامل غير موجود.", "Worker not found.");

        }



        var scopeError = EnsureWithinScope<object?>(currentUser!, worker.AssociationId);

        if (scopeError is not null)

        {

            return scopeError;

        }



        await _unitOfWork.Workers.Remove(worker, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);



        return ApiResponse<object?>.SuccessResponse(null, "تم حذف العامل بنجاح.", "Worker deleted successfully.");

    }



    public async Task<ApiResponse<WorkerDto>> RestoreAsync(

        int currentUserId,

        int workerId,

        CancellationToken cancellationToken = default)

    {

        var (currentUser, authError) = await AuthorizeAsync<WorkerDto>(currentUserId, WriteScope, cancellationToken);

        if (authError is not null)

        {

            return authError;

        }



        var workerResponse = await _unitOfWork.Workers.GetByIdIncludingDeletedAsync(workerId, cancellationToken);

        var worker = workerResponse.Data;

        if (worker is null)

        {

            return ApiResponse<WorkerDto>.FailureResponse("العامل غير موجود.", "Worker not found.");

        }



        var scopeError = EnsureWithinScope<WorkerDto>(currentUser!, worker.AssociationId);

        if (scopeError is not null)

        {

            return scopeError;

        }



        if (!worker.IsDeleted)

        {

            return ApiResponse<WorkerDto>.FailureResponse("العامل غير محذوف أصلًا.", "Worker is not deleted.");

        }



        worker.IsDeleted = false;

        worker.DeletedAt = null;



        await _unitOfWork.SaveChangesAsync(cancellationToken);



        var restoredResponse = await _unitOfWork.Workers.GetByIdProjectedAsync(worker.Id, cancellationToken);

        var restored = restoredResponse.Data;

        return ApiResponse<WorkerDto>.SuccessResponse(restored!, "تم استرجاع العامل بنجاح.", "Worker restored successfully.");

    }



    #region Helpers



    private async Task<(User? User, ApiResponse<T>? Error)> AuthorizeAsync<T>(int currentUserId, int[] anyOfPermissionIds, CancellationToken cancellationToken)

    {

        var currentUser = await _unitOfWork.Users.GetWithPermissionsAsync(currentUserId, cancellationToken);

        if (currentUser is null)

        {

            return (null, ApiResponse<T>.FailureResponse("المستخدم الحالي غير موجود.", "Current user was not found."));

        }



        if (!currentUser.IsActive)

        {

            return (null, ApiResponse<T>.FailureResponse("المستخدم الحالي غير فعّال.", "Current user is inactive."));

        }



        if (!GetEffectivePermissionIds(currentUser).Overlaps(anyOfPermissionIds))

        {

            return (null, ApiResponse<T>.FailureResponse("لا تملك الصلاحية المطلوبة لإدارة العمال.", "You do not hold the required Workers permission."));

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

            return (0, ApiResponse<T>.FailureResponse("يجب اختيار الجمعية عند إضافة عامل.", "You must select an association."));

        }



        var association = await _unitOfWork.Associations.GetByIdAsync(requestedAssociationId.Value, cancellationToken);

        if (association is null)

        {

            return (0, ApiResponse<T>.FailureResponse("الجمعية غير موجودة.", "Association not found."));

        }



        return (association.Id, null);

    }



    private static ApiResponse<T>? EnsureWithinScope<T>(User currentUser, int workerAssociationId)

    {

        if (currentUser.EntityType == EntityType.Association && workerAssociationId != currentUser.EntityId)

        {

            return ApiResponse<T>.FailureResponse("لا يمكنك الوصول إلى عامل خارج جمعيتك.", "Cannot access a worker outside your own association.");

        }



        return null;

    }



    private static HashSet<int> GetEffectivePermissionIds(User user)

    {

        var rolePermissionIds = user.Role?.RolePermissions?

            .Where(rp => rp.IsActive && rp.Permission.IsActive)

            .Select(rp => rp.PermissionId) ?? Enumerable.Empty<int>();



        var directPermissionIds = user.UserPermissions?

            .Where(up => up.IsActive && up.Permission.IsActive)

            .Select(up => up.PermissionId) ?? Enumerable.Empty<int>();



        var groupPermissionIds = user.UserGroups?

            .Where(ug => ug.Group.IsActive)

            .SelectMany(ug => ug.Group.GroupPermissions ?? Enumerable.Empty<GroupPermission>())

            .Where(gp => gp.IsActive && gp.Permission.IsActive)

            .Select(gp => gp.PermissionId) ?? Enumerable.Empty<int>();



        return rolePermissionIds.Concat(directPermissionIds).Concat(groupPermissionIds).ToHashSet();

    }



    private async Task<ApiResponse<T>?> EnsureCountryAndCityExistAsync<T>(

        int countryId,

        int cityId,

        CancellationToken cancellationToken)

    {

        var country = await _unitOfWork.Countries.GetByIdAsync(countryId, cancellationToken);

        if (country is null)

        {

            return ApiResponse<T>.FailureResponse("الدولة غير موجودة.", "Country not found.");

        }



        var city = await _unitOfWork.Cities.GetByIdAsync(cityId, cancellationToken);

        if (city is null)

        {

            return ApiResponse<T>.FailureResponse("المدينة غير موجودة.", "City not found.");

        }



        return null;

    }



    private async Task<ApiResponse<T>?> EnsureServicesExistAsync<T>(

        List<int> serviceIds,

        CancellationToken cancellationToken)

    {

        foreach (var serviceId in serviceIds)

        {

            var service = await _unitOfWork.Services.GetByIdAsync(serviceId, cancellationToken);

            if (service is null)

            {

                return ApiResponse<T>.FailureResponse("إحدى الخدمات المحددة غير موجودة.", "One or more selected services were not found.");

            }

        }



        return null;

    }



    private static ApiResponse<T>? ValidatePhoneAndServices<T>(string phoneNumber, List<int> serviceIds)

    {

        if (string.IsNullOrWhiteSpace(phoneNumber) || !JordanMobileRegex.IsMatch(phoneNumber.Trim()))

        {

            return ApiResponse<T>.FailureResponse("رقم الهاتف غير صالح، يجب أن يكون رقم أردني.", "Phone number must be a valid Jordanian mobile number.");

        }



        if (serviceIds is null || serviceIds.Count == 0)

        {

            return ApiResponse<T>.FailureResponse("يجب اختيار خدمة واحدة على الأقل.", "At least one service must be selected.");

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



    #endregion

}