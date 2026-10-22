using System.Globalization;
using Maydan.Application.DTOs.Associations;
using Maydan.Application.DTOs.Common;
using Maydan.Application.DTOs.UserManagement;
using Maydan.Application.DTOs.Workers;
using Maydan.Application.Interfaces;
using Maydan.Domain.Entities;
using Maydan.Domain.Enums;

namespace Maydan.Application.Services;

public class AssociationService : IAssociationService
{
    private const int MaxNameLength = 200;

    private const int ViewAssociationsPermissionId = 9;
    private const int CreateAssociationsPermissionId = 10;
    private const int EditAssociationsPermissionId = 11;
    private const int DeleteAssociationsPermissionId = 12;
    private const int ManageAssociationsPermissionId = 13;
    private const int ViewAssociationUsersPermissionId = 14;

    private static readonly int[] ReadScope = { ViewAssociationsPermissionId, ManageAssociationsPermissionId };
    private static readonly int[] CreateScope = { CreateAssociationsPermissionId, ManageAssociationsPermissionId };
    private static readonly int[] EditScope = { EditAssociationsPermissionId, ManageAssociationsPermissionId };
    private static readonly int[] DeleteScope = { DeleteAssociationsPermissionId, ManageAssociationsPermissionId };
    private static readonly int[] ViewUsersScope = { ViewAssociationUsersPermissionId, ManageAssociationsPermissionId };

    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;

    public AssociationService(IUnitOfWork unitOfWork, IPasswordHasher passwordHasher)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
    }

    public async Task<ApiResponse<List<AssociationDto>>> GetAllAsync(
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        var (_, authError) = await AuthorizeAsync<List<AssociationDto>>(currentUserId, ReadScope, cancellationToken);
        if (authError is not null)
        {
            return authError;
        }

        var results = await _unitOfWork.Associations.QueryAsync(isDeleted: false, cancellationToken: cancellationToken);
        var data = results.Select(r => MapToDto(r.Association, r.WorkersCount)).ToList();

        return ApiResponse<List<AssociationDto>>.SuccessResponse(data, "تم جلب الجمعيات بنجاح", "Associations retrieved successfully");
    }

    public async Task<ApiResponse<AssociationDto>> GetByIdAsync(
        int currentUserId,
        int associationId,
        CancellationToken cancellationToken = default)
    {
        var (_, authError) = await AuthorizeAsync<AssociationDto>(currentUserId, ReadScope, cancellationToken);
        if (authError is not null)
        {
            return authError;
        }

        var result = await _unitOfWork.Associations.GetByIdWithWorkersCountAsync(associationId, cancellationToken);
        if (result is null || !result.HasValue)
        {
            return ApiResponse<AssociationDto>.FailureResponse("لم يتم العثور على الجمعية.", "Association not found.");
        }

        var data = MapToDto(result.Value.Association, result.Value.WorkersCount);

        return ApiResponse<AssociationDto>.SuccessResponse(data, "تم جلب بيانات الجمعية بنجاح", "Association retrieved successfully");
    }

    public async Task<ApiResponse<AssociationDetailsDto>> GetDetailsAsync(
         int currentUserId,
         int associationId,
         CancellationToken cancellationToken = default)
    {
        var (_, authError) = await AuthorizeAsync<AssociationDetailsDto>(currentUserId, ViewUsersScope, cancellationToken);
        if (authError is not null)
        {
            return authError;
        }

        var result = await _unitOfWork.Associations.GetByIdWithWorkersCountAsync(associationId, cancellationToken);
        if (result is null || !result.HasValue)
        {
            return ApiResponse<AssociationDetailsDto>.FailureResponse("لم يتم العثور على الجمعية.", "Association not found.");
        }

        var (association, workersCount) = result.Value;
        var users = await _unitOfWork.Users.GetByEntityAsync(EntityType.Association, associationId, search: null, cancellationToken);
        // Worker repository returns Worker entities, not WorkerSummaryDto objects.
        var workerEntities = await _unitOfWork.Workers.GetByAssociationIdAsync(associationId, cancellationToken);

        var workersList = workerEntities
            .Select(w => new WorkerSummaryDto(
                w.Id,
                w.FirstName,
                w.LastName,
                w.CivilId,
                w.WorkerServices
               .Select(ws => $"{ws.Service.NameEn} - {ws.Service.NameAr}")
               .ToList(),
                w.YearsOfExperience,
                w.PhoneNumber
            ))
            .ToList();


        var data = MapToDetailsDto(association, workersCount, users, workersList);

        return ApiResponse<AssociationDetailsDto>.SuccessResponse(data, "تم جلب تفاصيل الجمعية بنجاح", "Association details retrieved successfully");
    }

    public async Task<ApiResponse<List<AssociationDto>>> SearchByNameAsync(
        int currentUserId,
        string name,
        CancellationToken cancellationToken = default)
    {
        var (_, authError) = await AuthorizeAsync<List<AssociationDto>>(currentUserId, ReadScope, cancellationToken);
        if (authError is not null)
        {
            return authError;
        }

        var results = await _unitOfWork.Associations.QueryAsync(isDeleted: false, searchTerm: name, cancellationToken: cancellationToken);
        var data = results.Select(r => MapToDto(r.Association, r.WorkersCount)).ToList();

        return ApiResponse<List<AssociationDto>>.SuccessResponse(data, "تم البحث عن الجمعيات بنجاح", "Associations searched successfully");
    }

    public async Task<ApiResponse<List<AssociationDto>>> GetOrderedByWorkersCountAsync(
        int currentUserId,
        bool ascending,
        CancellationToken cancellationToken = default)
    {
        var (_, authError) = await AuthorizeAsync<List<AssociationDto>>(currentUserId, ReadScope, cancellationToken);
        if (authError is not null)
        {
            return authError;
        }

        var results = await _unitOfWork.Associations.QueryAsync(isDeleted: false, orderByWorkersCountAscending: ascending, cancellationToken: cancellationToken);
        var data = results.Select(r => MapToDto(r.Association, r.WorkersCount)).ToList();

        return ApiResponse<List<AssociationDto>>.SuccessResponse(data, "تم جلب الجمعيات المرتبة بنجاح", "Ordered associations retrieved successfully");
    }

    public async Task<ApiResponse<List<AssociationDto>>> GetDeletedAsync(
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        var (_, authError) = await AuthorizeAsync<List<AssociationDto>>(currentUserId, ReadScope, cancellationToken);
        if (authError is not null)
        {
            return authError;
        }

        var results = await _unitOfWork.Associations.QueryAsync(isDeleted: true, cancellationToken: cancellationToken);
        var data = results.Select(r => MapToDto(r.Association, r.WorkersCount)).ToList();

        return ApiResponse<List<AssociationDto>>.SuccessResponse(data, "تم جلب الجمعيات المحذوفة بنجاح", "Deleted associations retrieved successfully");
    }

    public async Task<ApiResponse<List<AssociationDto>>> SearchDeletedByNameAsync(
        int currentUserId,
        string name,
        CancellationToken cancellationToken = default)
    {
        var (_, authError) = await AuthorizeAsync<List<AssociationDto>>(currentUserId, ReadScope, cancellationToken);
        if (authError is not null)
        {
            return authError;
        }

        var results = await _unitOfWork.Associations.QueryAsync(isDeleted: true, searchTerm: name, cancellationToken: cancellationToken);
        var data = results.Select(r => MapToDto(r.Association, r.WorkersCount)).ToList();

        return ApiResponse<List<AssociationDto>>.SuccessResponse(data, "تم البحث في الجمعيات المحذوفة بنجاح", "Deleted associations searched successfully");
    }

    public async Task<ApiResponse<AssociationDto>> CreateAsync(
        int currentUserId,
        CreateAssociationDto dto,
        CancellationToken cancellationToken = default)
    {
        var (currentUser, authError) = await AuthorizeAsync<AssociationDto>(currentUserId, CreateScope, cancellationToken);
        if (authError is not null)
        {
            return authError;
        }

        var nameEnError = ValidateName<AssociationDto>(dto.EnglishName, "English association name", "اسم الجمعية باللغة الإنجليزية");
        if (nameEnError is not null) return nameEnError;

        var nameArError = ValidateName<AssociationDto>(dto.ArabicName, "Arabic association name", "اسم الجمعية باللغة العربية");
        if (nameArError is not null) return nameArError;

        if (await _unitOfWork.Cities.GetByIdAsync(dto.CityId, cancellationToken) is null)
        {
            return ApiResponse<AssociationDto>.FailureResponse("المدينة غير موجودة.", "City not found.");
        }

        var existingAssociations = await _unitOfWork.Associations.QueryAsync(isDeleted: false, cancellationToken: cancellationToken);

        var isDuplicateName = existingAssociations.Any(r =>
            r.Association.ArabicName.Trim().Equals(dto.ArabicName.Trim(), StringComparison.OrdinalIgnoreCase) &&
            r.Association.EnglishName.Trim().Equals(dto.EnglishName.Trim(), StringComparison.OrdinalIgnoreCase));

        if (isDuplicateName)
        {
            return ApiResponse<AssociationDto>.FailureResponse("توجد جمعية مسجلة بنفس الاسم العربي والإنجليزي معاً", "An association with the same Arabic and English name already exists");
        }

        var (parsedLat, latError) = ParseCoordinate<AssociationDto>(dto.Latitude, "Latitude", "خط العرض");
        if (latError is not null) return latError;

        var (parsedLng, lngError) = ParseCoordinate<AssociationDto>(dto.Longitude, "Longitude", "خط الطول");
        if (lngError is not null) return lngError;

        var isDuplicateLocation = existingAssociations.Any(r =>
            r.Association.Latitude == parsedLat &&
            r.Association.Longitude == parsedLng);

        if (isDuplicateLocation)
        {
            return ApiResponse<AssociationDto>.FailureResponse("توجد جمعية مسجلة بنفس الإحداثيات الجغرافية", "Another association is already registered with the same geographic coordinates");
        }

        var adminPayload = dto.Admin;
        Role? adminRole = null;
        if (adminPayload is not null)
        {
            try
            {
                AssociationAdminUserFactory.ValidatePayload(adminPayload);
            }
            catch (Exception ex)
            {
                return ApiResponse<AssociationDto>.FailureResponse("بيانات مسؤول الجمعية غير صالحة.", ex.Message);
            }

            if (await _unitOfWork.Users.EmailExistsAsync(adminPayload.Email.Trim(), cancellationToken))
            {
                return ApiResponse<AssociationDto>.FailureResponse("البريد الإلكتروني للمسؤول مستخدم بالفعل", "A user with this email already exists");
            }

            if (await _unitOfWork.Users.PhoneExistsAsync(adminPayload.PhoneNumber.Trim(), cancellationToken))
            {
                return ApiResponse<AssociationDto>.FailureResponse("رقم الهاتف للمسؤول مستخدم بالفعل", "A user with this phone number already exists");
            }

            adminRole = await _unitOfWork.Roles.GetWithPermissionsAsync(AssociationAdminUserFactory.AssociationRoleId, cancellationToken);
            if (adminRole is null)
            {
                return ApiResponse<AssociationDto>.FailureResponse("دور مسؤول الجمعية غير موجود.", "Association role was not found.");
            }
        }

        var association = new Association
        {
            EnglishName = dto.EnglishName.Trim(),
            ArabicName = dto.ArabicName.Trim(),
            CityId = dto.CityId,
            LocationOnGoogleMaps = string.IsNullOrWhiteSpace(dto.LocationOnGoogleMaps) ? null : dto.LocationOnGoogleMaps.Trim(),
            Latitude = parsedLat,
            Longitude = parsedLng,
            ContactEmail = dto.ContactEmail.Trim(),
            ContactPhone = dto.ContactPhone.Trim(),
            CreatedBy = currentUser!.UserId,
            IsActive = true
        };

        try
        {
            if (adminRole is null)
            {
                await _unitOfWork.Associations.AddAsync(association, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            else
            {
                await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    await _unitOfWork.Associations.AddAsync(association, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    var adminUser = AssociationAdminUserFactory.Build(adminRole, association.Id, adminPayload!, _passwordHasher);
                    await _unitOfWork.Users.AddAsync(adminUser, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }, cancellationToken);
            }
        }
        catch (Exception ex) when (ex.GetType().FullName == "Microsoft.EntityFrameworkCore.DbUpdateException")
        {
            return HandleDbUpdateException(ex);
        }

        var data = await MapExistingAsync(association.Id, cancellationToken);
        return ApiResponse<AssociationDto>.SuccessResponse(data, "تم إنشاء الجمعية بنجاح", "Association created successfully", 201);
    }

    public async Task<ApiResponse<AssociationDto>> UpdateAsync(
        int currentUserId,
        UpdateAssociationDto dto,
        CancellationToken cancellationToken = default)
    {
        var (_, authError) = await AuthorizeAsync<AssociationDto>(currentUserId, EditScope, cancellationToken);
        if (authError is not null)
        {
            return authError;
        }

        var nameEnError = ValidateName<AssociationDto>(dto.EnglishName, "English association name", "اسم الجمعية باللغة الإنجليزية");
        if (nameEnError is not null) return nameEnError;

        var nameArError = ValidateName<AssociationDto>(dto.ArabicName, "Arabic association name", "اسم الجمعية باللغة العربية");
        if (nameArError is not null) return nameArError;

        var association = await _unitOfWork.Associations.GetByIdAsync(dto.Id, cancellationToken);
        if (association is null)
        {
            return ApiResponse<AssociationDto>.FailureResponse("لم يتم العثور على الجمعية.", "Association not found.");
        }

        if (await _unitOfWork.Cities.GetByIdAsync(dto.CityId, cancellationToken) is null)
        {
            return ApiResponse<AssociationDto>.FailureResponse("المدينة غير موجودة.", "City not found.");
        }

        var existingAssociations = await _unitOfWork.Associations.QueryAsync(isDeleted: false, cancellationToken: cancellationToken);

        var isDuplicateName = existingAssociations.Any(r =>
            r.Association.Id != dto.Id &&
            r.Association.ArabicName.Trim().Equals(dto.ArabicName.Trim(), StringComparison.OrdinalIgnoreCase) &&
            r.Association.EnglishName.Trim().Equals(dto.EnglishName.Trim(), StringComparison.OrdinalIgnoreCase));

        if (isDuplicateName)
        {
            return ApiResponse<AssociationDto>.FailureResponse("توجد جمعية أخرى مسجلة بنفس الاسم العربي والإنجليزي معاً", "Another association with the same Arabic and English name already exists");
        }

        var (parsedLat, latError) = ParseCoordinate<AssociationDto>(dto.Latitude, "Latitude", "خط العرض");
        if (latError is not null) return latError;

        var (parsedLng, lngError) = ParseCoordinate<AssociationDto>(dto.Longitude, "Longitude", "خط الطول");
        if (lngError is not null) return lngError;

        var isDuplicateLocation = existingAssociations.Any(r =>
            r.Association.Id != dto.Id &&
            r.Association.Latitude == parsedLat &&
            r.Association.Longitude == parsedLng);

        if (isDuplicateLocation)
        {
            return ApiResponse<AssociationDto>.FailureResponse("توجد جمعية أخرى مسجلة بنفس الإحداثيات الجغرافية", "Another association is already registered with the same geographic coordinates");
        }

        association.EnglishName = dto.EnglishName.Trim();
        association.ArabicName = dto.ArabicName.Trim();
        association.CityId = dto.CityId;
        association.LocationOnGoogleMaps = string.IsNullOrWhiteSpace(dto.LocationOnGoogleMaps) ? null : dto.LocationOnGoogleMaps.Trim();
        association.Latitude = parsedLat;
        association.Longitude = parsedLng;
        association.ContactEmail = dto.ContactEmail.Trim();
        association.ContactPhone = dto.ContactPhone.Trim();

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (ex.GetType().FullName == "Microsoft.EntityFrameworkCore.DbUpdateException")
        {
            return HandleDbUpdateException(ex);
        }

        var data = await MapExistingAsync(association.Id, cancellationToken);
        return ApiResponse<AssociationDto>.SuccessResponse(data, "تم تحديث الجمعية بنجاح", "Association updated successfully");
    }

    public async Task<ApiResponse<bool>> DeleteAsync(
        int currentUserId,
        int associationId,
        CancellationToken cancellationToken = default)
    {
        var (_, authError) = await AuthorizeAsync<bool>(currentUserId, DeleteScope, cancellationToken);
        if (authError is not null)
        {
            return authError;
        }

        var association = await _unitOfWork.Associations.GetByIdWithWorkersAsync(associationId, cancellationToken);
        if (association is null)
        {
            return ApiResponse<bool>.FailureResponse("لم يتم العثور على الجمعية.", "Association not found.");
        }

        var users = await _unitOfWork.Users.GetByEntityAsync(EntityType.Association, association.Id, search: null, cancellationToken);
        var workers = association.Workers.ToList();

        foreach (var user in users)
        {
            _unitOfWork.Users.Remove(user);
        }

        foreach (var worker in workers)
        {
            await _unitOfWork.Workers.Remove(worker);
        }

        _unitOfWork.Associations.Remove(association);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.SuccessResponse(true, "تم حذف الجمعية بنجاح", "Association deleted successfully");
    }

    public async Task<ApiResponse<AssociationDto>> RestoreAsync(
        int currentUserId,
        int associationId,
        CancellationToken cancellationToken = default)
    {
        var (_, authError) = await AuthorizeAsync<AssociationDto>(currentUserId, DeleteScope, cancellationToken);
        if (authError is not null)
        {
            return authError;
        }

        var association = await _unitOfWork.Associations.GetByIdIncludingDeletedAsync(associationId, cancellationToken);
        if (association is null)
        {
            return ApiResponse<AssociationDto>.FailureResponse("لم يتم العثور على الجمعية.", "Association not found.");
        }

        if (!association.IsDeleted)
        {
            return ApiResponse<AssociationDto>.FailureResponse("الجمعية غير محذوفة أصلاً.", "Association is not deleted.");
        }

        var cascadeDeletedAt = association.DeletedAt;
        var deletedUsers = await _unitOfWork.Users.GetDeletedByEntityAsync(EntityType.Association, association.Id, cancellationToken);
        var workersResponse = await _unitOfWork.Workers.GetDeletedByAssociationIdAsync(association.Id, cancellationToken);
        var deletedWorkers = workersResponse ?? new List<Worker>();

        association.IsDeleted = false;
        association.DeletedAt = null;

        foreach (var user in deletedUsers.Where(u => u.DeletedAt == cascadeDeletedAt))
        {
            user.IsDeleted = false;
            user.DeletedAt = null;
        }

        foreach (var worker in deletedWorkers.Where(w => w.DeletedAt == cascadeDeletedAt))
        {
            worker.IsDeleted = false;
            worker.DeletedAt = null;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var data = await MapExistingAsync(association.Id, cancellationToken);
        return ApiResponse<AssociationDto>.SuccessResponse(data, "تم استعادة الجمعية بنجاح", "Association restored successfully");
    }

    #region Helpers

    private async Task<(User? User, ApiResponse<T>? Error)> AuthorizeAsync<T>(
        int currentUserId,
        int[] anyOfPermissionIds,
        CancellationToken cancellationToken)
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
            return (null, ApiResponse<T>.FailureResponse("لا تملك الصلاحية المطلوبة لإدارة الجمعيات.", "You do not hold the required Associations permission."));
        }

        return (currentUser, null);
    }

    private static ApiResponse<AssociationDto> HandleDbUpdateException(Exception ex)
    {
        var innerMsg = ex.InnerException?.Message ?? ex.Message;

        if (innerMsg.Contains("IX_Associations_ArabicName_EnglishName") || innerMsg.Contains("ArabicName"))
        {
            return ApiResponse<AssociationDto>.FailureResponse(
                "توجد جمعية مسجلة بنفس الاسم العربي والإنجليزي معاً",
                "An association with the same Arabic and English name already exists");
        }

        if (innerMsg.Contains("IX_Associations_Latitude_Longitude") || innerMsg.Contains("Latitude"))
        {
            return ApiResponse<AssociationDto>.FailureResponse(
                "توجد جمعية مسجلة بنفس الإحداثيات الجغرافية",
                "An association with the same geographic coordinates already exists");
        }

        if (innerMsg.Contains("IX_Associations_ContactPhone") || innerMsg.Contains("ContactPhone"))
        {
            return ApiResponse<AssociationDto>.FailureResponse(
                "رقم الهاتف مستخدم بالفعل لجمعية أخرى",
                "The contact phone number is already used by another association");
        }

        return ApiResponse<AssociationDto>.FailureResponse(
            "حدث خطأ أثناء حفظ البيانات بقاعدة البيانات",
            "An error occurred while saving data to the database");
    }

    private async Task<AssociationDto> MapExistingAsync(int associationId, CancellationToken cancellationToken)
    {
        var result = await _unitOfWork.Associations.GetByIdWithWorkersCountAsync(associationId, cancellationToken);
        if (result is null || !result.HasValue)
        {
            throw new KeyNotFoundException("Association could not be loaded after the change.");
        }

        var (association, workersCount) = result.Value;
        return MapToDto(association, workersCount);
    }

    private static ApiResponse<T>? ValidateName<T>(string value, string fieldLabelEn, string fieldLabelAr)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return ApiResponse<T>.FailureResponse($"{fieldLabelAr} مطلوب.", $"{fieldLabelEn} is required.");
        }

        if (value.Trim().Length > MaxNameLength)
        {
            return ApiResponse<T>.FailureResponse($"{fieldLabelAr} لا يمكن أن يتجاوز {MaxNameLength} حرف.", $"{fieldLabelEn} cannot exceed {MaxNameLength} characters.");
        }

        return null;
    }

    private static (decimal Result, ApiResponse<T>? Error) ParseCoordinate<T>(string? value, string fieldLabelEn, string fieldLabelAr)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return (0, ApiResponse<T>.FailureResponse($"{fieldLabelAr} مطلوب.", $"{fieldLabelEn} is required."));
        }

        if (!decimal.TryParse(value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
        {
            return (0, ApiResponse<T>.FailureResponse($"{fieldLabelAr} ليس رقماً صالحاً.", $"{fieldLabelEn} is not a valid number."));
        }

        return (parsed, null);
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

    private static AssociationDto MapToDto(Association association, int workersCount) => new()
    {
        Id = association.Id,
        EnglishName = association.EnglishName,
        ArabicName = association.ArabicName,
        LocationOnGoogleMaps = association.LocationOnGoogleMaps ?? string.Empty,
        Latitude = FormatCoordinate(association.Latitude),
        Longitude = FormatCoordinate(association.Longitude),
        CountryId = association.City.CountryId,
        CountryEnglishName = association.City.Country.EnglishName,
        CountryArabicName = association.City.Country.ArabicName,
        ContactEmail = association.ContactEmail ?? string.Empty,
        ContactPhone = association.ContactPhone ?? string.Empty,
        CityId = association.CityId,
        CityEnglishName = association.City.EnglishName,
        CityArabicName = association.City.ArabicName,
        IsDeleted = association.IsDeleted,
        WorkersCount = workersCount
    };

    private static string FormatCoordinate(decimal? value) =>
        value.HasValue ? value.Value.ToString("0.######", CultureInfo.InvariantCulture) : string.Empty;

    private static AssociationDetailsDto MapToDetailsDto(Association association, int workersCount, List<User> users, List<WorkerSummaryDto> workers)
    {
        var dto = MapToDto(association, workersCount);

        return new AssociationDetailsDto
        {
            Id = dto.Id,
            EnglishName = dto.EnglishName,
            ArabicName = dto.ArabicName,
            LocationOnGoogleMaps = dto.LocationOnGoogleMaps,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            CountryId = dto.CountryId,
            CountryEnglishName = dto.CountryEnglishName,
            CountryArabicName = dto.CountryArabicName,
            CityId = dto.CityId,
            CityEnglishName = dto.CityEnglishName,
            CityArabicName = dto.CityArabicName,
            ContactEmail = dto.ContactEmail,
            ContactPhone = dto.ContactPhone,
            IsDeleted = dto.IsDeleted,
            WorkersCount = dto.WorkersCount,
            Users = users.Select(UserManagementService.MapUserSummary).ToList(),
            Workers = workers
        };
    }

    #endregion
}