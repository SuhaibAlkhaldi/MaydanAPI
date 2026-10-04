using System.Globalization;
using Maydan.Application.DTOs.Associations;
using Maydan.Application.DTOs.Common;
using Maydan.Application.DTOs.UserManagement;
using Maydan.Application.Interfaces;
using Maydan.Domain.Entities;
using Maydan.Domain.Enums;
using Microsoft.EntityFrameworkCore;

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
        await GetAuthorizedUserAsync(currentUserId, ViewAssociationsPermissionId, cancellationToken);

        var results = await _unitOfWork.Associations.QueryAsync(isDeleted: false, cancellationToken: cancellationToken);
        var data = results.Select(r => MapToDto(r.Association, r.WorkersCount)).ToList();

        return new ApiResponse<List<AssociationDto>>(true, "تم جلب الجمعيات بنجاح", "Associations retrieved successfully", data);
    }

    public async Task<ApiResponse<AssociationDto>> GetByIdAsync(int currentUserId, int associationId, CancellationToken cancellationToken = default)
    {
        await GetAuthorizedUserAsync(currentUserId, ViewAssociationsPermissionId, cancellationToken);

        var result = await _unitOfWork.Associations.GetByIdWithWorkersCountAsync(associationId, cancellationToken)
            ?? throw new KeyNotFoundException("Association was not found.");

        var data = MapToDto(result.Association, result.WorkersCount);

        return new ApiResponse<AssociationDto>(true, "تم جلب بيانات الجمعية بنجاح", "Association retrieved successfully", data);
    }

    public async Task<ApiResponse<AssociationDetailsDto>> GetDetailsAsync(int currentUserId, int associationId, CancellationToken cancellationToken = default)
    {
        await GetAuthorizedUserAsync(currentUserId, ViewAssociationUsersPermissionId, cancellationToken);

        var result = await _unitOfWork.Associations.GetByIdWithWorkersCountAsync(associationId, cancellationToken)
            ?? throw new KeyNotFoundException("Association was not found.");

        var users = await _unitOfWork.Users.GetByEntityAsync(EntityType.Association, associationId, search: null, cancellationToken);
        var data = MapToDetailsDto(result.Association, result.WorkersCount, users);

        return new ApiResponse<AssociationDetailsDto>(true, "تم جلب تفاصيل الجمعية بنجاح", "Association details retrieved successfully", data);
    }

    public async Task<ApiResponse<List<AssociationDto>>> SearchByNameAsync(int currentUserId, string name, CancellationToken cancellationToken = default)
    {
        await GetAuthorizedUserAsync(currentUserId, ViewAssociationsPermissionId, cancellationToken);

        var results = await _unitOfWork.Associations.QueryAsync(isDeleted: false, searchTerm: name, cancellationToken: cancellationToken);
        var data = results.Select(r => MapToDto(r.Association, r.WorkersCount)).ToList();

        return new ApiResponse<List<AssociationDto>>(true, "تم البحث عن الجمعيات بنجاح", "Associations searched successfully", data);
    }

    public async Task<ApiResponse<List<AssociationDto>>> GetOrderedByWorkersCountAsync(int currentUserId, bool ascending, CancellationToken cancellationToken = default)
    {
        await GetAuthorizedUserAsync(currentUserId, ViewAssociationsPermissionId, cancellationToken);

        var results = await _unitOfWork.Associations.QueryAsync(isDeleted: false, orderByWorkersCountAscending: ascending, cancellationToken: cancellationToken);
        var data = results.Select(r => MapToDto(r.Association, r.WorkersCount)).ToList();

        return new ApiResponse<List<AssociationDto>>(true, "تم جلب الجمعيات المرتبة بنجاح", "Ordered associations retrieved successfully", data);
    }

    public async Task<ApiResponse<List<AssociationDto>>> GetDeletedAsync(int currentUserId, CancellationToken cancellationToken = default)
    {
        await GetAuthorizedUserAsync(currentUserId, ViewAssociationsPermissionId, cancellationToken);

        var results = await _unitOfWork.Associations.QueryAsync(isDeleted: true, cancellationToken: cancellationToken);
        var data = results.Select(r => MapToDto(r.Association, r.WorkersCount)).ToList();

        return new ApiResponse<List<AssociationDto>>(true, "تم جلب الجمعيات المحذوفة بنجاح", "Deleted associations retrieved successfully", data);
    }

    public async Task<ApiResponse<List<AssociationDto>>> SearchDeletedByNameAsync(int currentUserId, string name, CancellationToken cancellationToken = default)
    {
        await GetAuthorizedUserAsync(currentUserId, ViewAssociationsPermissionId, cancellationToken);

        var results = await _unitOfWork.Associations.QueryAsync(isDeleted: true, searchTerm: name, cancellationToken: cancellationToken);
        var data = results.Select(r => MapToDto(r.Association, r.WorkersCount)).ToList();

        return new ApiResponse<List<AssociationDto>>(true, "تم البحث في الجمعيات المحذوفة بنجاح", "Deleted associations searched successfully", data);
    }

    public async Task<ApiResponse<AssociationDto>> CreateAsync(int currentUserId, CreateAssociationDto dto, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetAuthorizedUserAsync(currentUserId, CreateAssociationsPermissionId, cancellationToken);

        ValidateName(dto.EnglishName, "English association name");
        ValidateName(dto.ArabicName, "Arabic association name");
        await EnsureCityExistsAsync(dto.CityId, cancellationToken);

        var existingAssociations = await _unitOfWork.Associations.QueryAsync(isDeleted: false, cancellationToken: cancellationToken);

        var isDuplicateName = existingAssociations.Any(r =>
            r.Association.ArabicName.Trim().Equals(dto.ArabicName.Trim(), StringComparison.OrdinalIgnoreCase) &&
            r.Association.EnglishName.Trim().Equals(dto.EnglishName.Trim(), StringComparison.OrdinalIgnoreCase));

        if (isDuplicateName)
        {
            return new ApiResponse<AssociationDto>(
                false,
                "توجد جمعية مسجلة بنفس الاسم العربي والإنجليزي معاً",
                "An association with the same Arabic and English name already exists",
                null!);
        }

        var parsedLat = ParseCoordinate(dto.Latitude, "Latitude");
        var parsedLng = ParseCoordinate(dto.Longitude, "Longitude");

        var isDuplicateLocation = existingAssociations.Any(r =>
            r.Association.Latitude == parsedLat &&
            r.Association.Longitude == parsedLng);

        if (isDuplicateLocation)
        {
            return new ApiResponse<AssociationDto>(
                false,
                "توجد جمعية مسجلة بنفس الإحداثيات الجغرافية",
                "Another association is already registered with the same geographic coordinates",
                null!);
        }

        var adminPayload = dto.Admin;
        Role? adminRole = null;
        if (adminPayload is not null)
        {
            AssociationAdminUserFactory.ValidatePayload(adminPayload);

            if (await _unitOfWork.Users.EmailExistsAsync(adminPayload.Email.Trim(), cancellationToken))
            {
                return new ApiResponse<AssociationDto>(
                    false,
                    "البريد الإلكتروني للمسؤول مستخدم بالفعل",
                    "A user with this email already exists",
                    null!);
            }

            adminRole = await _unitOfWork.Roles.GetWithPermissionsAsync(AssociationAdminUserFactory.AssociationRoleId, cancellationToken)
                ?? throw new KeyNotFoundException("Association role was not found.");
        }

        var association = new Association
        {
            EnglishName = dto.EnglishName.Trim(),
            ArabicName = dto.ArabicName.Trim(),
            CityId = dto.CityId,
            LocationOnGoogleMaps = string.IsNullOrWhiteSpace(dto.LocationOnGoogleMaps) ? null : dto.LocationOnGoogleMaps.Trim(),
            Latitude = parsedLat,
            Longitude = parsedLng,
            CreatedBy = currentUser.UserId,
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
        catch (DbUpdateException ex)
        {
            return HandleDbUpdateException(ex);
        }

        var data = await MapExistingAsync(association.Id, cancellationToken);
        return new ApiResponse<AssociationDto>(true, "تم إنشاء الجمعية بنجاح", "Association created successfully", data);
    }

    public async Task<ApiResponse<AssociationDto>> UpdateAsync(int currentUserId, UpdateAssociationDto dto, CancellationToken cancellationToken = default)
    {
        await GetAuthorizedUserAsync(currentUserId, EditAssociationsPermissionId, cancellationToken);

        ValidateName(dto.EnglishName, "English association name");
        ValidateName(dto.ArabicName, "Arabic association name");

        var association = await _unitOfWork.Associations.GetByIdAsync(dto.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Association was not found.");

        await EnsureCityExistsAsync(dto.CityId, cancellationToken);

        var existingAssociations = await _unitOfWork.Associations.QueryAsync(isDeleted: false, cancellationToken: cancellationToken);

        var isDuplicateName = existingAssociations.Any(r =>
            r.Association.Id != dto.Id &&
            r.Association.ArabicName.Trim().Equals(dto.ArabicName.Trim(), StringComparison.OrdinalIgnoreCase) &&
            r.Association.EnglishName.Trim().Equals(dto.EnglishName.Trim(), StringComparison.OrdinalIgnoreCase));

        if (isDuplicateName)
        {
            return new ApiResponse<AssociationDto>(
                false,
                "توجد جمعية أخرى مسجلة بنفس الاسم العربي والإنجليزي معاً",
                "Another association with the same Arabic and English name already exists",
                null!);
        }

        var parsedLat = ParseCoordinate(dto.Latitude, "Latitude");
        var parsedLng = ParseCoordinate(dto.Longitude, "Longitude");

        var isDuplicateLocation = existingAssociations.Any(r =>
            r.Association.Id != dto.Id &&
            r.Association.Latitude == parsedLat &&
            r.Association.Longitude == parsedLng);

        if (isDuplicateLocation)
        {
            return new ApiResponse<AssociationDto>(
                false,
                "توجد جمعية أخرى مسجلة بنفس الإحداثيات الجغرافية",
                "Another association is already registered with the same geographic coordinates",
                null!);
        }

        association.EnglishName = dto.EnglishName.Trim();
        association.ArabicName = dto.ArabicName.Trim();
        association.CityId = dto.CityId;
        association.LocationOnGoogleMaps = string.IsNullOrWhiteSpace(dto.LocationOnGoogleMaps) ? null : dto.LocationOnGoogleMaps.Trim();
        association.Latitude = parsedLat;
        association.Longitude = parsedLng;

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            return HandleDbUpdateException(ex);
        }

        var data = await MapExistingAsync(association.Id, cancellationToken);
        return new ApiResponse<AssociationDto>(true, "تم تحديث الجمعية بنجاح", "Association updated successfully", data);
    }

    public async Task<ApiResponse<bool>> DeleteAsync(int currentUserId, int associationId, CancellationToken cancellationToken = default)
    {
        await GetAuthorizedUserAsync(currentUserId, DeleteAssociationsPermissionId, cancellationToken);

        var association = await _unitOfWork.Associations.GetByIdWithWorkersAsync(associationId, cancellationToken)
            ?? throw new KeyNotFoundException("Association was not found.");

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

        return new ApiResponse<bool>(true, "تم حذف الجمعية بنجاح", "Association deleted successfully", true);
    }

    public async Task<ApiResponse<AssociationDto>> RestoreAsync(int currentUserId, int associationId, CancellationToken cancellationToken = default)
    {
        await GetAuthorizedUserAsync(currentUserId, DeleteAssociationsPermissionId, cancellationToken);

        var association = await _unitOfWork.Associations.GetByIdIncludingDeletedAsync(associationId, cancellationToken)
            ?? throw new KeyNotFoundException("Association was not found.");

        if (!association.IsDeleted)
        {
            throw new InvalidOperationException("Association is not deleted.");
        }

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

        var data = await MapExistingAsync(association.Id, cancellationToken);
        return new ApiResponse<AssociationDto>(true, "تم استعادة الجمعية بنجاح", "Association restored successfully", data);
    }

    private static ApiResponse<AssociationDto> HandleDbUpdateException(DbUpdateException ex)
    {
        var innerMsg = ex.InnerException?.Message ?? ex.Message;

        if (innerMsg.Contains("IX_Associations_ArabicName_EnglishName") || innerMsg.Contains("ArabicName"))
        {
            return new ApiResponse<AssociationDto>(
                false,
                "توجد جمعية مسجلة بنفس الاسم العربي والإنجليزي معاً",
                "An association with the same Arabic and English name already exists",
                null!);
        }

        if (innerMsg.Contains("IX_Associations_Latitude_Longitude") || innerMsg.Contains("Latitude"))
        {
            return new ApiResponse<AssociationDto>(
                false,
                "توجد جمعية مسجلة بنفس الإحداثيات الجغرافية",
                "An association with the same geographic coordinates already exists",
                null!);
        }

        if (innerMsg.Contains("IX_Associations_ContactPhone") || innerMsg.Contains("ContactPhone"))
        {
            return new ApiResponse<AssociationDto>(
                false,
                "رقم الهاتف مستخدم بالفعل لجمعية أخرى",
                "The contact phone number is already used by another association",
                null!);
        }

        return new ApiResponse<AssociationDto>(
            false,
            "حدث خطأ أثناء حفظ البيانات بقاعدة البيانات",
            "An error occurred while saving data to the database",
            null!);
    }

    private async Task<AssociationDto> MapExistingAsync(int associationId, CancellationToken cancellationToken)
    {
        var result = await _unitOfWork.Associations.GetByIdWithWorkersCountAsync(associationId, cancellationToken)
            ?? throw new KeyNotFoundException("Association could not be loaded after the change.");

        return MapToDto(result.Association, result.WorkersCount);
    }

    private async Task<User> GetAuthorizedUserAsync(int currentUserId, int requiredPermissionId, CancellationToken cancellationToken)
    {
        var currentUser = await _unitOfWork.Users.GetWithPermissionsAsync(currentUserId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Current user was not found.");

        if (!currentUser.IsActive)
        {
            throw new UnauthorizedAccessException("Current user is inactive.");
        }

        if (!GetEffectivePermissionIds(currentUser).Overlaps(new[] { requiredPermissionId, ManageAssociationsPermissionId }))
        {
            throw new UnauthorizedAccessException("Caller does not hold the required Associations permission.");
        }

        return currentUser;
    }

    private async Task EnsureCityExistsAsync(int cityId, CancellationToken cancellationToken)
    {
        if (await _unitOfWork.Cities.GetByIdAsync(cityId, cancellationToken) is null)
        {
            throw new KeyNotFoundException("City was not found.");
        }
    }

    private static void ValidateName(string value, string fieldLabel)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{fieldLabel} is required.");
        }

        if (value.Trim().Length > MaxNameLength)
        {
            throw new InvalidOperationException($"{fieldLabel} cannot exceed {MaxNameLength} characters.");
        }
    }

    private static decimal ParseCoordinate(string? value, string fieldLabel)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{fieldLabel} is required.");
        }

        if (!decimal.TryParse(value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
        {
            throw new InvalidOperationException($"{fieldLabel} is not a valid number.");
        }

        return parsed;
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

    private static string FormatCoordinate(decimal value) =>
        value.ToString("0.######", CultureInfo.InvariantCulture);

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
}