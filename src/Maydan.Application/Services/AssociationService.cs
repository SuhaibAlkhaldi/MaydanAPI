using System.Globalization;
using Maydan.Application.DTOs.Associations;
using Maydan.Application.DTOs.Common;
using Maydan.Application.DTOs.UserManagement;
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

    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;

    public AssociationService(IUnitOfWork unitOfWork, IPasswordHasher passwordHasher)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
    }

    public async Task<ApiResponse<List<AssociationDto>>> GetAllAsync(int currentUserId, CancellationToken cancellationToken = default)
    {
        var (currentUser, authError) = await GetAuthorizedUserAsync<List<AssociationDto>>(currentUserId, ViewAssociationsPermissionId, cancellationToken);
        if (authError is not null) return authError;

        var results = await _unitOfWork.Associations.QueryAsync(isDeleted: false, cancellationToken: cancellationToken);
        var dtos = results.Select(r => MapToDto(r.Association, r.WorkersCount)).ToList();

        return ApiResponse<List<AssociationDto>>.SuccessResponse(dtos, "Associations retrieved successfully.", "تم جلب الجمعيات بنجاح.");
    }

    public async Task<ApiResponse<AssociationDto>> GetByIdAsync(int currentUserId, int associationId, CancellationToken cancellationToken = default)
    {
        var (currentUser, authError) = await GetAuthorizedUserAsync<AssociationDto>(currentUserId, ViewAssociationsPermissionId, cancellationToken);
        if (authError is not null) return authError;

        var tenantError = EnsureSameAssociation<AssociationDto>(associationId, currentUser!);
        if (tenantError != null) return tenantError;

        var result = await _unitOfWork.Associations.GetByIdWithWorkersCountAsync(associationId, cancellationToken);
        if (result == null)
            return new ApiResponse<AssociationDto>(false, "الجمعية غير موجودة.", "Association was not found.", default, 404);

        return ApiResponse<AssociationDto>.SuccessResponse(MapToDto(result.Value.Association, result.Value.WorkersCount), "Association retrieved successfully.", "تم جلب بيانات الجمعية بنجاح.");
    }

    public async Task<ApiResponse<AssociationDetailsDto>> GetDetailsAsync(int currentUserId, int associationId, CancellationToken cancellationToken = default)
    {
        var (currentUser, authError) = await GetAuthorizedUserAsync<AssociationDetailsDto>(currentUserId, ViewAssociationUsersPermissionId, cancellationToken);
        if (authError is not null) return authError;

        var tenantError = EnsureSameAssociation<AssociationDetailsDto>(associationId, currentUser!);
        if (tenantError != null) return tenantError;

        var result = await _unitOfWork.Associations.GetByIdWithWorkersCountAsync(associationId, cancellationToken);
        if (result == null)
            return new ApiResponse<AssociationDetailsDto>(false, "الجمعية غير موجودة.", "Association was not found.", default, 404);

        var users = await _unitOfWork.Users.GetByEntityAsync(EntityType.Association, associationId, search: null, cancellationToken);

        return ApiResponse<AssociationDetailsDto>.SuccessResponse(MapToDetailsDto(result.Value.Association, result.Value.WorkersCount, users), "Details retrieved successfully.", "تم جلب التفاصيل بنجاح.");
    }

    public async Task<ApiResponse<List<AssociationDto>>> SearchByNameAsync(int currentUserId, string name, CancellationToken cancellationToken = default)
    {
        var (currentUser, authError) = await GetAuthorizedUserAsync<List<AssociationDto>>(currentUserId, ViewAssociationsPermissionId, cancellationToken);
        if (authError is not null) return authError;

        var results = await _unitOfWork.Associations.QueryAsync(isDeleted: false, searchTerm: name, cancellationToken: cancellationToken);
        var dtos = results.Select(r => MapToDto(r.Association, r.WorkersCount)).ToList();

        return ApiResponse<List<AssociationDto>>.SuccessResponse(dtos, "Search completed successfully.", "تم البحث بنجاح.");
    }

    public async Task<ApiResponse<List<AssociationDto>>> GetOrderedByWorkersCountAsync(int currentUserId, bool ascending, CancellationToken cancellationToken = default)
    {
        var (currentUser, authError) = await GetAuthorizedUserAsync<List<AssociationDto>>(currentUserId, ViewAssociationsPermissionId, cancellationToken);
        if (authError is not null) return authError;

        var results = await _unitOfWork.Associations.QueryAsync(isDeleted: false, orderByWorkersCountAscending: ascending, cancellationToken: cancellationToken);
        var dtos = results.Select(r => MapToDto(r.Association, r.WorkersCount)).ToList();

        return ApiResponse<List<AssociationDto>>.SuccessResponse(dtos, "Ordered successfully.", "تم الفرز بنجاح.");
    }

    public async Task<ApiResponse<List<AssociationDto>>> GetDeletedAsync(int currentUserId, CancellationToken cancellationToken = default)
    {
        var (currentUser, authError) = await GetAuthorizedUserAsync<List<AssociationDto>>(currentUserId, ViewAssociationsPermissionId, cancellationToken);
        if (authError is not null) return authError;

        var results = await _unitOfWork.Associations.QueryAsync(isDeleted: true, cancellationToken: cancellationToken);
        var dtos = results.Select(r => MapToDto(r.Association, r.WorkersCount)).ToList();

        return ApiResponse<List<AssociationDto>>.SuccessResponse(dtos, "Deleted associations retrieved successfully.", "تم جلب الجمعيات المحذوفة بنجاح.");
    }

    public async Task<ApiResponse<List<AssociationDto>>> SearchDeletedByNameAsync(int currentUserId, string name, CancellationToken cancellationToken = default)
    {
        var (currentUser, authError) = await GetAuthorizedUserAsync<List<AssociationDto>>(currentUserId, ViewAssociationsPermissionId, cancellationToken);
        if (authError is not null) return authError;

        var results = await _unitOfWork.Associations.QueryAsync(isDeleted: true, searchTerm: name, cancellationToken: cancellationToken);
        var dtos = results.Select(r => MapToDto(r.Association, r.WorkersCount)).ToList();

        return ApiResponse<List<AssociationDto>>.SuccessResponse(dtos, "Deleted search completed successfully.", "تم البحث بالجمعيات المحذوفة بنجاح.");
    }

    public async Task<ApiResponse<AssociationDto>> CreateAsync(int currentUserId, CreateAssociationDto dto, CancellationToken cancellationToken = default)
    {
        var (currentUser, authError) = await GetAuthorizedUserAsync<AssociationDto>(currentUserId, CreateAssociationsPermissionId, cancellationToken);
        if (authError is not null) return authError;

        var nameEnError = ValidateName<AssociationDto>(dto.EnglishName, "الاسم الإنجليزي", "English association name");
        if (nameEnError != null) return nameEnError;

        var nameArError = ValidateName<AssociationDto>(dto.ArabicName, "الاسم العربي", "Arabic association name");
        if (nameArError != null) return nameArError;

        if (await _unitOfWork.Cities.GetByIdAsync(dto.CityId, cancellationToken) is null)
            return new ApiResponse<AssociationDto>(false, "المدينة غير موجودة.", "City was not found.", default, 404);

        var (lat, latError) = ParseCoordinate<AssociationDto>(dto.Latitude, "خط العرض (Latitude)", "Latitude");
        if (latError != null) return latError;

        var (lon, lonError) = ParseCoordinate<AssociationDto>(dto.Longitude, "خط الطول (Longitude)", "Longitude");
        if (lonError != null) return lonError;

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
                return ApiResponse<AssociationDto>.FailureResponse(ex.Message, ex.Message);
            }

            if (await _unitOfWork.Users.EmailExistsAsync(adminPayload.Email.Trim(), cancellationToken))
                return ApiResponse<AssociationDto>.FailureResponse("A user with this email already exists.", "يوجد مستخدم مسجل بهذا البريد الإلكتروني مسبقاً.");

            adminRole = await _unitOfWork.Roles.GetWithPermissionsAsync(AssociationAdminUserFactory.AssociationRoleId, cancellationToken);
            if (adminRole == null)
                return new ApiResponse<AssociationDto>(false, "دور الجمعية غير موجود.", "Association role was not found.", default, 404);
        }

        var association = new Association
        {
            EnglishName = dto.EnglishName.Trim(),
            ArabicName = dto.ArabicName.Trim(),
            CityId = dto.CityId,
            LocationOnGoogleMaps = string.IsNullOrWhiteSpace(dto.LocationOnGoogleMaps) ? null : dto.LocationOnGoogleMaps.Trim(),
            Latitude = lat,
            Longitude = lon,
            CreatedBy = currentUser!.UserId,
            IsActive = true
        };

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

        return await MapExistingAsync(association.Id, "تم إنشاء الجمعية بنجاح.", "Association created successfully.", 201, cancellationToken);
    }

    public async Task<ApiResponse<AssociationDto>> UpdateAsync(int currentUserId, UpdateAssociationDto dto, CancellationToken cancellationToken = default)
    {
        var (currentUser, authError) = await GetAuthorizedUserAsync<AssociationDto>(currentUserId, EditAssociationsPermissionId, cancellationToken);
        if (authError is not null) return authError;

        var tenantError = EnsureSameAssociation<AssociationDto>(dto.Id, currentUser!);
        if (tenantError != null) return tenantError;

        var nameEnError = ValidateName<AssociationDto>(dto.EnglishName, "الاسم الإنجليزي", "English association name");
        if (nameEnError != null) return nameEnError;

        var nameArError = ValidateName<AssociationDto>(dto.ArabicName, "الاسم العربي", "Arabic association name");
        if (nameArError != null) return nameArError;

        var association = await _unitOfWork.Associations.GetByIdAsync(dto.Id, cancellationToken);
        if (association == null)
            return new ApiResponse<AssociationDto>(false, "الجمعية غير موجودة.", "Association was not found.", default, 404);

        if (await _unitOfWork.Cities.GetByIdAsync(dto.CityId, cancellationToken) is null)
            return new ApiResponse<AssociationDto>(false, "المدينة غير موجودة.", "City was not found.", default, 404);

        var (lat, latError) = ParseCoordinate<AssociationDto>(dto.Latitude, "خط العرض", "Latitude");
        if (latError != null) return latError;

        var (lon, lonError) = ParseCoordinate<AssociationDto>(dto.Longitude, "خط الطول", "Longitude");
        if (lonError != null) return lonError;

        association.EnglishName = dto.EnglishName.Trim();
        association.ArabicName = dto.ArabicName.Trim();
        association.CityId = dto.CityId;
        association.LocationOnGoogleMaps = string.IsNullOrWhiteSpace(dto.LocationOnGoogleMaps) ? null : dto.LocationOnGoogleMaps.Trim();
        association.Latitude = lat;
        association.Longitude = lon;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await MapExistingAsync(association.Id, "تم تحديث الجمعية بنجاح.", "Association updated successfully.", 200, cancellationToken);
    }

    public async Task<ApiResponse<object?>> DeleteAsync(int currentUserId, int associationId, CancellationToken cancellationToken = default)
    {
        var (currentUser, authError) = await GetAuthorizedUserAsync<object?>(currentUserId, DeleteAssociationsPermissionId, cancellationToken);
        if (authError is not null) return authError;

        var tenantError = EnsureSameAssociation<object?>(associationId, currentUser!);
        if (tenantError != null) return tenantError;

        try
        {
            var association = await _unitOfWork.Associations.GetByIdWithWorkersAsync(associationId, cancellationToken);
            if (association == null)
                return new ApiResponse<object?>(false, "الجمعية غير موجودة.", "Association was not found.", default, 404);

            var users = await _unitOfWork.Users.GetByEntityAsync(EntityType.Association, association.Id, search: null, cancellationToken);
            var workers = association.Workers?.ToList() ?? new List<Worker>();

            foreach (var user in users) _unitOfWork.Users.Remove(user);
            foreach (var worker in workers) _unitOfWork.Workers.Remove(worker);

            _unitOfWork.Associations.Remove(association);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return ApiResponse<object?>.SuccessResponse(null, "Association deleted successfully.", "تم حذف الجمعية بنجاح.");
        }
        catch (Exception ex)
        {
            string realError = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
            return new ApiResponse<object?>(false, $"خطأ في السيرفر: {realError}", $"Server Error: {realError}", default, 500);
        }
    }

    public async Task<ApiResponse<AssociationDto>> RestoreAsync(int currentUserId, int associationId, CancellationToken cancellationToken = default)
    {
        var (currentUser, authError) = await GetAuthorizedUserAsync<AssociationDto>(currentUserId, DeleteAssociationsPermissionId, cancellationToken);
        if (authError is not null) return authError;

        var tenantError = EnsureSameAssociation<AssociationDto>(associationId, currentUser!);
        if (tenantError != null) return tenantError;

        var association = await _unitOfWork.Associations.GetByIdIncludingDeletedAsync(associationId, cancellationToken);
        if (association == null)
            return new ApiResponse<AssociationDto>(false, "الجمعية غير موجودة.", "Association was not found.", default, 404);

        if (!association.IsDeleted)
            return ApiResponse<AssociationDto>.FailureResponse("Association is not deleted.", "الجمعية غير محذوفة أصلاً.");

        var cascadeDeletedAt = association.DeletedAt;

        var deletedUsers = await _unitOfWork.Users.GetDeletedByEntityAsync(EntityType.Association, association.Id, cancellationToken);
        var deletedWorkers = await _unitOfWork.Workers.GetDeletedByAssociationIdAsync(association.Id, cancellationToken);

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

        return await MapExistingAsync(association.Id, "تم استرجاع الجمعية بنجاح.", "Association restored successfully.", 200, cancellationToken);
    }

    #region Helpers

    private async Task<ApiResponse<AssociationDto>> MapExistingAsync(int associationId, string msgAr, string msgEn, int statusCode, CancellationToken cancellationToken)
    {
        var result = await _unitOfWork.Associations.GetByIdWithWorkersCountAsync(associationId, cancellationToken);
        if (result == null)
            return new ApiResponse<AssociationDto>(false, "تعذر تحميل الجمعية.", "Association could not be loaded.", default, 404);

        return ApiResponse<AssociationDto>.SuccessResponse(MapToDto(result.Value.Association, result.Value.WorkersCount), msgEn, msgAr, statusCode);
    }

    private async Task<(User? User, ApiResponse<T>? Error)> GetAuthorizedUserAsync<T>(int currentUserId, int requiredPermissionId, CancellationToken cancellationToken)
    {
        var currentUser = await _unitOfWork.Users.GetWithPermissionsAsync(currentUserId, cancellationToken);

        if (currentUser is null)
            return (null, new ApiResponse<T>(false, "المستخدم الحالي غير موجود.", "Current user was not found.", default, 401));

        if (!currentUser.IsActive)
            return (null, new ApiResponse<T>(false, "المستخدم الحالي غير فعّال.", "Current user is inactive.", default, 401));

        if (!GetEffectivePermissionIds(currentUser).Overlaps(new[] { requiredPermissionId, ManageAssociationsPermissionId }))
        {
            return (null, new ApiResponse<T>(false, "لا تملك الصلاحية المطلوبة لإدارة الجمعيات.", "Caller does not hold the required Associations permission.", default, 403));
        }

        return (currentUser, null);
    }

    private static ApiResponse<T>? EnsureSameAssociation<T>(int associationId, User currentUser)
    {
        if (currentUser.EntityType == EntityType.Association && currentUser.EntityId != associationId)
        {
            return new ApiResponse<T>(false, "لا تملك صلاحية الوصول لبيانات جمعية أخرى.", "Cannot access data outside your own association.", default, 403);
        }
        return null;
    }

    private static ApiResponse<T>? ValidateName<T>(string value, string fieldLabelAr, string fieldLabelEn)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return ApiResponse<T>.FailureResponse($"{fieldLabelEn} is required.", $"{fieldLabelAr} مطلوب.");
        }

        if (value.Trim().Length > MaxNameLength)
        {
            return ApiResponse<T>.FailureResponse($"{fieldLabelEn} cannot exceed {MaxNameLength} characters.", $"{fieldLabelAr} يجب ألا يتجاوز {MaxNameLength} حرف.");
        }

        return null;
    }

    private static (decimal? Value, ApiResponse<T>? Error) ParseCoordinate<T>(string? value, string fieldLabelAr, string fieldLabelEn)
    {
        if (string.IsNullOrWhiteSpace(value)) return (null, null);

        if (!decimal.TryParse(value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
        {
            return (null, ApiResponse<T>.FailureResponse($"{fieldLabelEn} is not a valid number.", $"{fieldLabelAr} ليس رقماً صالحاً."));
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
        CityId = association.CityId,
        CityEnglishName = association.City.EnglishName,
        CityArabicName = association.City.ArabicName,
        IsDeleted = association.IsDeleted,
        WorkersCount = workersCount
    };

    private static string FormatCoordinate(decimal? value) =>
        value?.ToString("0.######", CultureInfo.InvariantCulture) ?? string.Empty;

    private static AssociationDetailsDto MapToDetailsDto(Association association, int workersCount, List<User> users)
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
            IsDeleted = dto.IsDeleted,
            WorkersCount = dto.WorkersCount,
            Users = users.Select(UserManagementService.MapUserSummary).ToList()
        };
    }
    #endregion
}