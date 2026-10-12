using System.Globalization;
using Maydan.Application.DTOs.Associations;
using Maydan.Application.DTOs.Common;
using Maydan.Application.DTOs.UserManagement;
using Maydan.Application.DTOs.Workers;
using Maydan.Application.Interfaces;
using Maydan.Domain.Entities;
using Maydan.Domain.Enums;

namespace Maydan.Application.Services;

public class AssociationServiceV2 : IAssociationServiceV2
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

    public AssociationServiceV2(IUnitOfWork unitOfWork, IPasswordHasher passwordHasher)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
    }

    public async Task<ApiResponse<List<AssociationDtoV2>>> GetAllAsync(
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        var (_, authError) = await AuthorizeAsync<List<AssociationDtoV2>>(currentUserId, ReadScope, cancellationToken);
        if (authError is not null)
        {
            return authError;
        }

        var results = await _unitOfWork.AssociationsV2.QueryAsync(isDeleted: false, cancellationToken: cancellationToken);
        var data = results.Select(r => MapToDto(r.AssociationV2, r.WorkersCount)).ToList();

        return ApiResponse<List<AssociationDtoV2>>.SuccessResponse(
            data,
            "Associations retrieved successfully.",
            "تم جلب الجمعيات بنجاح.");
    }

    public async Task<ApiResponse<AssociationDtoV2>> GetByIdAsync(
        int currentUserId,
        int associationIdV2,
        CancellationToken cancellationToken = default)
    {
        var (_, authError) = await AuthorizeAsync<AssociationDtoV2>(currentUserId, ReadScope, cancellationToken);
        if (authError is not null)
        {
            return authError;
        }

        var result = await _unitOfWork.AssociationsV2.GetByIdWithWorkersCountAsync(associationIdV2, cancellationToken);
        if (result is null || !result.HasValue)
        {
            return ApiResponse<AssociationDtoV2>.FailureResponse("Association not found.", "لم يتم العثور على الجمعية.");
        }

        var (associationV2, workersCount) = result.Value;
        var data = MapToDto(associationV2, workersCount);
        return ApiResponse<AssociationDtoV2>.SuccessResponse(
            data,
            "Association retrieved successfully.",
            "تم جلب بيانات الجمعية بنجاح.");
    }

    public async Task<ApiResponse<AssociationDetailsDtoV2>> GetDetailsAsync(
         int currentUserId,
         int associationIdV2,
         CancellationToken cancellationToken = default)
    {
        var (_, authError) = await AuthorizeAsync<AssociationDetailsDtoV2>(currentUserId, ViewUsersScope, cancellationToken);
        if (authError is not null)
        {
            return authError;
        }

        var result = await _unitOfWork.AssociationsV2.GetByIdWithWorkersCountAsync(associationIdV2, cancellationToken);
        if (result is null || !result.HasValue)
        {
            return ApiResponse<AssociationDetailsDtoV2>.FailureResponse("Association not found.", "لم يتم العثور على الجمعية.");
        }

        var (associationV2, workersCount) = result.Value;
        var users = await _unitOfWork.Users.GetByEntityAsync(EntityType.Association, associationIdV2, search: null, cancellationToken);
        var workersResponse = await _unitOfWork.Workers.GetByAssociationIdAsync(associationIdV2, cancellationToken);
        var workers = workersResponse.Data ?? new List<WorkerSummaryDto>();
        var data = MapToDetailsDto(associationV2, workersCount, users, workers);

        return ApiResponse<AssociationDetailsDtoV2>.SuccessResponse(
            data,
            "Association details retrieved successfully.",
            "تم جلب تفاصيل الجمعية بنجاح.");
    }

    public async Task<ApiResponse<List<AssociationDtoV2>>> SearchByNameAsync(
        int currentUserId,
        string name,
        CancellationToken cancellationToken = default)
    {
        var (_, authError) = await AuthorizeAsync<List<AssociationDtoV2>>(currentUserId, ReadScope, cancellationToken);
        if (authError is not null)
        {
            return authError;
        }

        var results = await _unitOfWork.AssociationsV2.QueryAsync(isDeleted: false, searchTerm: name, cancellationToken: cancellationToken);
        var data = results.Select(r => MapToDto(r.AssociationV2, r.WorkersCount)).ToList();

        return ApiResponse<List<AssociationDtoV2>>.SuccessResponse(
            data,
            "Associations searched successfully.",
            "تم البحث عن الجمعيات بنجاح.");
    }

    public async Task<ApiResponse<List<AssociationDtoV2>>> GetOrderedByWorkersCountAsync(
        int currentUserId,
        bool ascending,
        CancellationToken cancellationToken = default)
    {
        var (_, authError) = await AuthorizeAsync<List<AssociationDtoV2>>(currentUserId, ReadScope, cancellationToken);
        if (authError is not null)
        {
            return authError;
        }

        var results = await _unitOfWork.AssociationsV2.QueryAsync(isDeleted: false, orderByWorkersCountAscending: ascending, cancellationToken: cancellationToken);
        var data = results.Select(r => MapToDto(r.AssociationV2, r.WorkersCount)).ToList();

        return ApiResponse<List<AssociationDtoV2>>.SuccessResponse(
            data,
            "Ordered associations retrieved successfully.",
            "تم جلب الجمعيات المرتبة بنجاح.");
    }

    public async Task<ApiResponse<List<AssociationDtoV2>>> GetDeletedAsync(
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        var (_, authError) = await AuthorizeAsync<List<AssociationDtoV2>>(currentUserId, ReadScope, cancellationToken);
        if (authError is not null)
        {
            return authError;
        }

        var results = await _unitOfWork.AssociationsV2.QueryAsync(isDeleted: true, cancellationToken: cancellationToken);
        var data = results.Select(r => MapToDto(r.AssociationV2, r.WorkersCount)).ToList();

        return ApiResponse<List<AssociationDtoV2>>.SuccessResponse(
            data,
            "Deleted associations retrieved successfully.",
            "تم جلب الجمعيات المحذوفة بنجاح.");
    }

    public async Task<ApiResponse<List<AssociationDtoV2>>> SearchDeletedByNameAsync(
        int currentUserId,
        string name,
        CancellationToken cancellationToken = default)
    {
        var (_, authError) = await AuthorizeAsync<List<AssociationDtoV2>>(currentUserId, ReadScope, cancellationToken);
        if (authError is not null)
        {
            return authError;
        }

        var results = await _unitOfWork.AssociationsV2.QueryAsync(isDeleted: true, searchTerm: name, cancellationToken: cancellationToken);
        var data = results.Select(r => MapToDto(r.AssociationV2, r.WorkersCount)).ToList();

        return ApiResponse<List<AssociationDtoV2>>.SuccessResponse(
            data,
            "Deleted associations searched successfully.",
            "تم البحث في الجمعيات المحذوفة بنجاح.");
    }

    public async Task<ApiResponse<AssociationDtoV2>> CreateAsync(
        int currentUserId,
        CreateAssociationDtoV2 dto,
        CancellationToken cancellationToken = default)
    {
        var (currentUser, authError) = await AuthorizeAsync<AssociationDtoV2>(currentUserId, CreateScope, cancellationToken);
        if (authError is not null)
        {
            return authError;
        }

        var nameEnError = ValidateName<AssociationDtoV2>(dto.EnglishName, "English association name", "اسم الجمعية باللغة الإنجليزية");
        if (nameEnError is not null) return nameEnError;

        var nameArError = ValidateName<AssociationDtoV2>(dto.ArabicName, "Arabic association name", "اسم الجمعية باللغة العربية");
        if (nameArError is not null) return nameArError;

        if (await _unitOfWork.Cities.GetByIdAsync(dto.CityId, cancellationToken) is null)
        {
            return ApiResponse<AssociationDtoV2>.FailureResponse("City not found.", "المدينة غير موجودة.");
        }

        var existingAssociations = await _unitOfWork.AssociationsV2.QueryAsync(isDeleted: false, cancellationToken: cancellationToken);

        var isDuplicateName = existingAssociations.Any(r =>
            r.AssociationV2.ArabicName.Trim().Equals(dto.ArabicName.Trim(), StringComparison.OrdinalIgnoreCase) &&
            r.AssociationV2.EnglishName.Trim().Equals(dto.EnglishName.Trim(), StringComparison.OrdinalIgnoreCase));

        if (isDuplicateName)
        {
            return ApiResponse<AssociationDtoV2>.FailureResponse(
                "An association with the same Arabic and English name already exists.",
                "توجد جمعية مسجلة بنفس الاسم العربي والإنجليزي معاً.");
        }

        var (parsedLat, latError) = ParseCoordinate<AssociationDtoV2>(dto.Latitude, "Latitude", "خط العرض");
        if (latError is not null) return latError;

        var (parsedLng, lngError) = ParseCoordinate<AssociationDtoV2>(dto.Longitude, "Longitude", "خط الطول");
        if (lngError is not null) return lngError;

        var isDuplicateLocation = existingAssociations.Any(r =>
            r.AssociationV2.Latitude == parsedLat &&
            r.AssociationV2.Longitude == parsedLng);

        if (isDuplicateLocation)
        {
            return ApiResponse<AssociationDtoV2>.FailureResponse(
                "Another association is already registered with the same geographic coordinates.",
                "توجد جمعية مسجلة بنفس الإحداثيات الجغرافية.");
        }
        var phoneError = ValidateContactField<AssociationDtoV2>(dto.ContactPhone, "Contact phone", "رقم الهاتف");
        if (phoneError is not null) return phoneError;

        var emailError = ValidateContactField<AssociationDtoV2>(dto.ContactEmail, "Contact email", "البريد الإلكتروني");
        if (emailError is not null) return emailError;

        var isDuplicatePhone = existingAssociations.Any(r =>
            r.AssociationV2.ContactPhone.Trim().Equals(dto.ContactPhone.Trim(), StringComparison.OrdinalIgnoreCase));

        if (isDuplicatePhone)
        {
            return ApiResponse<AssociationDtoV2>.FailureResponse(
                "The contact phone number is already used by another association.",
                "رقم الهاتف مستخدم بالفعل لجمعية أخرى.");
        }

        var isDuplicateEmail = existingAssociations.Any(r =>
            r.AssociationV2.ContactEmail.Trim().Equals(dto.ContactEmail.Trim(), StringComparison.OrdinalIgnoreCase));

        if (isDuplicateEmail)
        {
            return ApiResponse<AssociationDtoV2>.FailureResponse(
                "The contact email is already used by another association.",
                "البريد الإلكتروني مستخدم بالفعل لجمعية أخرى.");
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
                return ApiResponse<AssociationDtoV2>.FailureResponse(ex.Message, "بيانات مسؤول الجمعية غير صالحة.");
            }

            if (await _unitOfWork.Users.EmailExistsAsync(adminPayload.Email.Trim(), cancellationToken))
            {
                return ApiResponse<AssociationDtoV2>.FailureResponse("A user with this email already exists.", "البريد الإلكتروني للمسؤول مستخدم بالفعل.");
            }

            adminRole = await _unitOfWork.Roles.GetWithPermissionsAsync(AssociationAdminUserFactory.AssociationRoleId, cancellationToken);
            if (adminRole is null)
            {
                return ApiResponse<AssociationDtoV2>.FailureResponse("Association role was not found.", "دور مسؤول الجمعية غير موجود.");
            }
        }

        var association = new AssociationV2
        {
            EnglishName = dto.EnglishName.Trim(),
            ArabicName = dto.ArabicName.Trim(),
            CityId = dto.CityId,
            LocationOnGoogleMaps = string.IsNullOrWhiteSpace(dto.LocationOnGoogleMaps) ? null : dto.LocationOnGoogleMaps.Trim(),
            Latitude = parsedLat,
            Longitude = parsedLng,
            ContactPhone = dto.ContactPhone?.Trim() ?? string.Empty,
            ContactEmail = dto.ContactEmail?.Trim() ?? string.Empty,
            CreatedBy = currentUser!.UserId,
            IsActive = true
        };

        try
        {
            if (adminRole is null)
            {
                await _unitOfWork.AssociationsV2.AddAsync(association, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            else
            {
                await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    await _unitOfWork.AssociationsV2.AddAsync(association, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    var adminUser = AssociationAdminUserFactory.Build(adminRole, association.Id, adminPayload!, _passwordHasher);
                    await _unitOfWork.Users.AddAsync(adminUser, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            return HandleDbException<AssociationDtoV2>(ex);
        }

        var data = await MapExistingAsync(association.Id, cancellationToken);
        return ApiResponse<AssociationDtoV2>.SuccessResponse(
            data,
            "Association created successfully.",
            "تم إنشاء الجمعية بنجاح.",
            201);
    }

    public async Task<ApiResponse<AssociationDtoV2>> UpdateAsync(
        int currentUserId,
        UpdateAssociationDtoV2 dto,
        CancellationToken cancellationToken = default)
    {
        var (_, authError) = await AuthorizeAsync<AssociationDtoV2>(currentUserId, EditScope, cancellationToken);
        if (authError is not null)
        {
            return authError;
        }

        var nameEnError = ValidateName<AssociationDtoV2>(dto.EnglishName, "English association name", "اسم الجمعية باللغة الإنجليزية");
        if (nameEnError is not null) return nameEnError;

        var nameArError = ValidateName<AssociationDtoV2>(dto.ArabicName, "Arabic association name", "اسم الجمعية باللغة العربية");
        if (nameArError is not null) return nameArError;

        var association = await _unitOfWork.AssociationsV2.GetByIdAsync(dto.Id, cancellationToken);
        if (association is null)
        {
            return ApiResponse<AssociationDtoV2>.FailureResponse("Association not found.", "لم يتم العثور على الجمعية.");
        }

        if (await _unitOfWork.Cities.GetByIdAsync(dto.CityId, cancellationToken) is null)
        {
            return ApiResponse<AssociationDtoV2>.FailureResponse("City not found.", "المدينة غير موجودة.");
        }

        var existingAssociations = await _unitOfWork.AssociationsV2.QueryAsync(isDeleted: false, cancellationToken: cancellationToken);

        var isDuplicateName = existingAssociations.Any(r =>
            r.AssociationV2.Id != dto.Id &&
            r.AssociationV2.ArabicName.Trim().Equals(dto.ArabicName.Trim(), StringComparison.OrdinalIgnoreCase) &&
            r.AssociationV2.EnglishName.Trim().Equals(dto.EnglishName.Trim(), StringComparison.OrdinalIgnoreCase));

        if (isDuplicateName)
        {
            return ApiResponse<AssociationDtoV2>.FailureResponse(
                "Another association with the same Arabic and English name already exists.",
                "توجد جمعية أخرى مسجلة بنفس الاسم العربي والإنجليزي معاً.");
        }

        var (parsedLat, latError) = ParseCoordinate<AssociationDtoV2>(dto.Latitude, "Latitude", "خط العرض");
        if (latError is not null) return latError;

        var (parsedLng, lngError) = ParseCoordinate<AssociationDtoV2>(dto.Longitude, "Longitude", "خط الطول");
        if (lngError is not null) return lngError;

        var isDuplicateLocation = existingAssociations.Any(r =>
            r.AssociationV2.Id != dto.Id &&
            r.AssociationV2.Latitude == parsedLat &&
            r.AssociationV2.Longitude == parsedLng);

        if (isDuplicateLocation)
        {
            return ApiResponse<AssociationDtoV2>.FailureResponse(
                "Another association is already registered with the same geographic coordinates.",
                "توجد جمعية أخرى مسجلة بنفس الإحداثيات الجغرافية.");
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
        catch (Exception ex)
        {
            return HandleDbException<AssociationDtoV2>(ex);
        }

        var data = await MapExistingAsync(association.Id, cancellationToken);
        return ApiResponse<AssociationDtoV2>.SuccessResponse(
            data,
            "Association updated successfully.",
            "تم تحديث بيانات الجمعية بنجاح.");
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

        var association = await _unitOfWork.AssociationsV2.GetByIdWithWorkersAsync(associationId, cancellationToken);
        if (association is null)
        {
            return ApiResponse<bool>.FailureResponse("Association not found.", "لم يتم العثور على الجمعية.");
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

        _unitOfWork.AssociationsV2.Remove(association);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.SuccessResponse(
            true,
            "Association deleted successfully.",
            "تم حذف الجمعية بنجاح.");
    }

    public async Task<ApiResponse<AssociationDtoV2>> RestoreAsync(
        int currentUserId,
        int associationId,
        CancellationToken cancellationToken = default)
    {
        var (_, authError) = await AuthorizeAsync<AssociationDtoV2>(currentUserId, DeleteScope, cancellationToken);
        if (authError is not null)
        {
            return authError;
        }

        var association = await _unitOfWork.AssociationsV2.GetByIdIncludingDeletedAsync(associationId, cancellationToken);
        if (association is null)
        {
            return ApiResponse<AssociationDtoV2>.FailureResponse("Association not found.", "لم يتم العثور على الجمعية.");
        }

        if (!association.IsDeleted)
        {
            return ApiResponse<AssociationDtoV2>.FailureResponse("Association is not deleted.", "الجمعية غير محذوفة أصلاً.");
        }

        var cascadeDeletedAt = association.DeletedAt;
        var deletedUsers = await _unitOfWork.Users.GetDeletedByEntityAsync(EntityType.Association, association.Id, cancellationToken);
        var workersResponse = await _unitOfWork.Workers.GetDeletedByAssociationIdAsync(association.Id, cancellationToken);
        var deletedWorkers = workersResponse.Data ?? new List<Worker>();

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
        return ApiResponse<AssociationDtoV2>.SuccessResponse(
            data,
            "Association restored successfully.",
            "تم استعادة الجمعية بنجاح.");
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
            return (null, ApiResponse<T>.FailureResponse("Current user was not found.", "المستخدم الحالي غير موجود."));
        }

        if (!currentUser.IsActive)
        {
            return (null, ApiResponse<T>.FailureResponse("Current user is inactive.", "المستخدم الحالي غير فعّال."));
        }

        if (!GetEffectivePermissionIds(currentUser).Overlaps(anyOfPermissionIds))
        {
            return (null, ApiResponse<T>.FailureResponse("You do not hold the required Associations permission.", "لا تملك الصلاحية المطلوبة لإدارة الجمعيات."));
        }

        return (currentUser, null);
    }

    private static ApiResponse<T>? ValidateContactField<T>(string value, string fieldLabelEn, string fieldLabelAr)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return ApiResponse<T>.FailureResponse($"{fieldLabelEn} is required.", $"{fieldLabelAr} مطلوب.");
        }

        return null;
    }

    private static ApiResponse<T> HandleDbException<T>(Exception ex)
    {
        var msg = ex.InnerException?.Message ?? ex.Message;

        if (msg.Contains("IX_Associations_ArabicName_EnglishName") || msg.Contains("ArabicName"))
        {
            return ApiResponse<T>.FailureResponse(
                "An association with the same Arabic and English name already exists.",
                "توجد جمعية مسجلة بنفس الاسم العربي والإنجليزي معاً.");
        }

        if (msg.Contains("IX_Associations_Latitude_Longitude") || msg.Contains("Latitude"))
        {
            return ApiResponse<T>.FailureResponse(
                "An association with the same geographic coordinates already exists.",
                "توجد جمعية مسجلة بنفس الإحداثيات الجغرافية.");
        }

        if (msg.Contains("IX_Associations_ContactPhone") || msg.Contains("ContactPhone"))
        {
            return ApiResponse<T>.FailureResponse(
                "The contact phone number is already used by another association.",
                "رقم الهاتف مستخدم بالفعل لجمعية أخرى.");
        }

        return ApiResponse<T>.FailureResponse(
            "An error occurred while saving data to the database.",
            "حدث خطأ أثناء حفظ البيانات بقاعدة البيانات.");
    }

    private async Task<AssociationDtoV2> MapExistingAsync(int associationId, CancellationToken cancellationToken)
    {
        var result = await _unitOfWork.AssociationsV2.GetByIdWithWorkersCountAsync(associationId, cancellationToken);
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
            return ApiResponse<T>.FailureResponse($"{fieldLabelEn} is required.", $"{fieldLabelAr} مطلوب.");
        }

        if (value.Trim().Length > MaxNameLength)
        {
            return ApiResponse<T>.FailureResponse($"{fieldLabelEn} cannot exceed {MaxNameLength} characters.", $"{fieldLabelAr} لا يمكن أن يتجاوز {MaxNameLength} حرف.");
        }

        return null;
    }

    private static (decimal Result, ApiResponse<T>? Error) ParseCoordinate<T>(string? value, string fieldLabelEn, string fieldLabelAr)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return (0, ApiResponse<T>.FailureResponse($"{fieldLabelEn} is required.", $"{fieldLabelAr} مطلوب."));
        }

        if (!decimal.TryParse(value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
        {
            return (0, ApiResponse<T>.FailureResponse($"{fieldLabelEn} is not a valid number.", $"{fieldLabelAr} ليس رقماً صالحاً."));
        }

        return (parsed, null);
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

    private static AssociationDtoV2 MapToDto(AssociationV2 association, int workersCount) => new()
    {
        Id = association.Id,
        EnglishName = association.EnglishName,
        ArabicName = association.ArabicName,
        LocationOnGoogleMaps = association.LocationOnGoogleMaps ?? string.Empty,
        Latitude = FormatCoordinate(association.Latitude),
        Longitude = FormatCoordinate(association.Longitude),
        ContactPhone = association.ContactPhone,
        ContactEmail = association.ContactEmail,
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
        value.HasValue ? value.Value.ToString("0.######", CultureInfo.InvariantCulture) : string.Empty;

    private static AssociationDetailsDtoV2 MapToDetailsDto(AssociationV2 association, int workersCount, List<User> users, List<WorkerSummaryDto> workers)
    {
        var dto = MapToDto(association, workersCount);

        return new AssociationDetailsDtoV2
        {
            Id = dto.Id,
            EnglishName = dto.EnglishName,
            ArabicName = dto.ArabicName,
            LocationOnGoogleMaps = dto.LocationOnGoogleMaps,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            ContactPhone = dto.ContactPhone,
            ContactEmail = dto.ContactEmail,
            CountryId = dto.CountryId,
            CountryEnglishName = dto.CountryEnglishName,
            CountryArabicName = dto.CountryArabicName,
            CityId = dto.CityId,
            CityEnglishName = dto.CityEnglishName,
            CityArabicName = dto.CityArabicName,
            IsDeleted = dto.IsDeleted,
            WorkersCount = dto.WorkersCount,
            Users = users.Select(UserManagementService.MapUserSummary).ToList(),
            Workers = workers
        };
    }

    #endregion
}