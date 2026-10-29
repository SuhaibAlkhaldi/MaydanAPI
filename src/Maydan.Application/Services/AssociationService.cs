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

        return ApiResponse<List<AssociationDto>>.SuccessResponse(data, messageEn: "Associations retrieved successfully", messageAr: "تم جلب الجمعيات بنجاح");
    }

    public async Task<ApiResponse<AssociationDto>> GetByIdAsync(
        int currentUserId,
        int associationId,
        CancellationToken cancellationToken = default)
    {
        var (currentUser, authError) = await AuthorizeAsync<AssociationDto>(currentUserId, ReadScope, cancellationToken);
        if (authError is not null)
        {
            return authError;
        }

        var tenantError = EnsureSameAssociation<AssociationDto>(associationId, currentUser!);
        if (tenantError is not null)
        {
            return tenantError;
        }

        var result = await _unitOfWork.Associations.GetByIdWithWorkersCountAsync(associationId, cancellationToken);
        if (result is null || !result.HasValue)
        {
            return new ApiResponse<AssociationDto>(false, "لم يتم العثور على الجمعية.", "Association not found.", default, 404);
        }

        var data = MapToDto(result.Value.Association, result.Value.WorkersCount);

        return ApiResponse<AssociationDto>.SuccessResponse(data, messageEn: "Association retrieved successfully", messageAr: "تم جلب بيانات الجمعية بنجاح");
    }

    public async Task<ApiResponse<AssociationDetailsDto>> GetDetailsAsync(
         int currentUserId,
         int associationId,
         CancellationToken cancellationToken = default)
    {
        var (currentUser, authError) = await AuthorizeAsync<AssociationDetailsDto>(currentUserId, ViewUsersScope, cancellationToken);
        if (authError is not null)
        {
            return authError;
        }

        var tenantError = EnsureSameAssociation<AssociationDetailsDto>(associationId, currentUser!);
        if (tenantError is not null)
        {
            return tenantError;
        }

        var result = await _unitOfWork.Associations.GetByIdWithWorkersCountAsync(associationId, cancellationToken);
        if (result is null || !result.HasValue)
        {
            return new ApiResponse<AssociationDetailsDto>(false, "لم يتم العثور على الجمعية.", "Association not found.", default, 404);
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

        return ApiResponse<AssociationDetailsDto>.SuccessResponse(data, messageEn: "Association details retrieved successfully", messageAr: "تم جلب تفاصيل الجمعية بنجاح");
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

        return ApiResponse<List<AssociationDto>>.SuccessResponse(data, messageEn: "Associations searched successfully", messageAr: "تم البحث عن الجمعيات بنجاح");
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

        return ApiResponse<List<AssociationDto>>.SuccessResponse(data, messageEn: "Ordered associations retrieved successfully", messageAr: "تم جلب الجمعيات المرتبة بنجاح");
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

        return ApiResponse<List<AssociationDto>>.SuccessResponse(data, messageEn: "Deleted associations retrieved successfully", messageAr: "تم جلب الجمعيات المحذوفة بنجاح");
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

        return ApiResponse<List<AssociationDto>>.SuccessResponse(data, messageEn: "Deleted associations searched successfully", messageAr: "تم البحث في الجمعيات المحذوفة بنجاح");
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
            return new ApiResponse<AssociationDto>(false, "المدينة غير موجودة.", "City not found.", default, 404);
        }

        var existingAssociations = await _unitOfWork.Associations.QueryAsync(isDeleted: false, cancellationToken: cancellationToken);

        var isDuplicateName = existingAssociations.Any(r =>
            r.Association.ArabicName.Trim().Equals(dto.ArabicName.Trim(), StringComparison.OrdinalIgnoreCase) &&
            r.Association.EnglishName.Trim().Equals(dto.EnglishName.Trim(), StringComparison.OrdinalIgnoreCase));

        if (isDuplicateName)
        {
            return ApiResponse<AssociationDto>.FailureResponse(messageEn: "An association with the same Arabic and English name already exists", messageAr: "توجد جمعية مسجلة بنفس الاسم العربي والإنجليزي معاً");
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
            return ApiResponse<AssociationDto>.FailureResponse(messageEn: "Another association is already registered with the same geographic coordinates", messageAr: "توجد جمعية مسجلة بنفس الإحداثيات الجغرافية");
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
                return ApiResponse<AssociationDto>.FailureResponse(messageEn: ex.Message, messageAr: "بيانات مسؤول الجمعية غير صالحة.");
            }

            if (await _unitOfWork.Users.EmailExistsAsync(adminPayload.Email.Trim(), cancellationToken))
            {
                return ApiResponse<AssociationDto>.FailureResponse(messageEn: "A user with this email already exists", messageAr: "البريد الإلكتروني للمسؤول مستخدم بالفعل");
            }

            if (await _unitOfWork.Users.PhoneExistsAsync(adminPayload.PhoneNumber.Trim(), cancellationToken))
            {
                return ApiResponse<AssociationDto>.FailureResponse(messageEn: "A user with this phone number already exists", messageAr: "رقم الهاتف للمسؤول مستخدم بالفعل");
            }

            adminRole = await _unitOfWork.Roles.GetWithPermissionsAsync(AssociationAdminUserFactory.AssociationRoleId, cancellationToken);
            if (adminRole is null)
            {
                return ApiResponse<AssociationDto>.FailureResponse(messageEn: "Association role was not found.", messageAr: "دور مسؤول الجمعية غير موجود.");
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
        return ApiResponse<AssociationDto>.SuccessResponse(data, messageEn: "Association created successfully", messageAr: "تم إنشاء الجمعية بنجاح", statusCode: 201);
    }

    public async Task<ApiResponse<AssociationUpdateResponseDto>> UpdateAsync(
        int currentUserId,
        UpdateAssociationDto dto,
        CancellationToken cancellationToken = default)
    {
        var (currentUser, authError) = await AuthorizeAsync<AssociationUpdateResponseDto>(
            currentUserId, EditScope, cancellationToken);
        if (authError is not null) return authError;

        if (dto.Id <= 0)
            return ApiResponse<AssociationUpdateResponseDto>.FailureResponse(
                messageEn: "Invalid association ID.", messageAr: "رقم الجمعية غير صالح.");

        var tenantError = EnsureSameAssociation<AssociationUpdateResponseDto>(dto.Id, currentUser!);
        if (tenantError is not null) return tenantError;

        // This repository method must return an EF-tracked entity.
        var association = await _unitOfWork.Associations.GetByIdForUpdateAsync(dto.Id, cancellationToken);
        if (association is null)
            return new ApiResponse<AssociationUpdateResponseDto>(
                false, "لم يتم العثور على الجمعية.", "Association not found.", default, 404);

        // Effective values: a missing field means retain its persisted value.
        var englishName = dto.EnglishName is null ? association.EnglishName : dto.EnglishName.Trim();
        var arabicName = dto.ArabicName is null ? association.ArabicName : dto.ArabicName.Trim();
        var cityId = dto.CityId ?? association.CityId;
        var latitude = association.Latitude;
        var longitude = association.Longitude;

        var nameEnError = ValidateName<AssociationUpdateResponseDto>(
            englishName, "English association name", "اسم الجمعية باللغة الإنجليزية");
        if (nameEnError is not null) return nameEnError;

        var nameArError = ValidateName<AssociationUpdateResponseDto>(
            arabicName, "Arabic association name", "اسم الجمعية باللغة العربية");
        if (nameArError is not null) return nameArError;

        if (dto.CityId.HasValue)
        {
            if (cityId <= 0 || await _unitOfWork.Cities.GetByIdAsync(cityId, cancellationToken) is null)
                return new ApiResponse<AssociationUpdateResponseDto>(
                    false, "المدينة غير موجودة.", "City not found.", default, 404);
        }

        if (dto.Latitude is not null)
        {
            var (parsed, error) = ParseCoordinate<AssociationUpdateResponseDto>(
                dto.Latitude, "Latitude", "خط العرض");
            if (error is not null) return error;
            latitude = parsed;
        }

        if (dto.Longitude is not null)
        {
            var (parsed, error) = ParseCoordinate<AssociationUpdateResponseDto>(
                dto.Longitude, "Longitude", "خط الطول");
            if (error is not null) return error;
            longitude = parsed;
        }

        // Validate uniqueness using the final combination, not the partial request.
        var existingAssociations = await _unitOfWork.Associations.QueryAsync(
            isDeleted: false, cancellationToken: cancellationToken);

        if (existingAssociations.Any(r =>
            r.Association.Id != dto.Id &&
            string.Equals(r.Association.EnglishName.Trim(), englishName.Trim(), StringComparison.OrdinalIgnoreCase) &&
            string.Equals(r.Association.ArabicName.Trim(), arabicName.Trim(), StringComparison.OrdinalIgnoreCase)))
            return ApiResponse<AssociationUpdateResponseDto>.FailureResponse(
                messageEn: "Another association with the same Arabic and English names already exists.",
                messageAr: "توجد جمعية أخرى مسجلة بنفس الاسم العربي والإنجليزي معاً.");

        if (existingAssociations.Any(r =>
            r.Association.Id != dto.Id &&
            r.Association.Latitude == latitude && r.Association.Longitude == longitude))
            return ApiResponse<AssociationUpdateResponseDto>.FailureResponse(
                messageEn: "Another association is already registered with the same geographic coordinates.",
                messageAr: "توجد جمعية أخرى مسجلة بنفس الإحداثيات الجغرافية.");

        User? admin = null;
        if (dto.Admin is not null)
        {
            admin = await _unitOfWork.Users.GetAssociationAdminForUpdateAsync(dto.Id, cancellationToken);
            if (admin is null)
                return new ApiResponse<AssociationUpdateResponseDto>(
                    false, "لم يتم العثور على مسؤول الجمعية.", "Association administrator not found.", default, 404);

            var adminFields = new (string? Value, string EnglishLabel, string ArabicLabel)[]
            {
                (dto.Admin.FirstNameEn, "English first name", "الاسم الأول بالإنجليزية"),
                (dto.Admin.LastNameEn, "English last name", "اسم العائلة بالإنجليزية"),
                (dto.Admin.FirstNameAr, "Arabic first name", "الاسم الأول بالعربية"),
                (dto.Admin.LastNameAr, "Arabic last name", "اسم العائلة بالعربية"),
                (dto.Admin.Email, "Email", "البريد الإلكتروني"),
                (dto.Admin.PhoneNumber, "Phone number", "رقم الهاتف")
            };

            foreach (var field in adminFields)
                if (field.Value is not null && string.IsNullOrWhiteSpace(field.Value))
                    return ApiResponse<AssociationUpdateResponseDto>.FailureResponse(
                        messageEn: $"{field.EnglishLabel} cannot be empty.",
                        messageAr: $"{field.ArabicLabel} لا يمكن أن يكون فارغًا.");

            if (dto.Admin.Email is not null)
            {
                var newEmail = dto.Admin.Email.Trim();
                if (!string.Equals(newEmail, admin.Email, StringComparison.OrdinalIgnoreCase) &&
                    await _unitOfWork.Users.EmailExistsForAnotherUserAsync(newEmail,admin.UserId,cancellationToken))
                    return ApiResponse<AssociationUpdateResponseDto>.FailureResponse(
                        messageEn: "A user with this email already exists.",
                        messageAr: "البريد الإلكتروني للمسؤول مستخدم بالفعل.");
            }

            if (dto.Admin.PhoneNumber is not null)
            {
                var newPhone = dto.Admin.PhoneNumber.Trim();
                if (!string.Equals(newPhone, admin.PhoneNumber, StringComparison.OrdinalIgnoreCase) &&
                    await _unitOfWork.Users.PhoneExistsForAnotherUserAsync(newPhone,admin.UserId,cancellationToken))
                    return ApiResponse<AssociationUpdateResponseDto>.FailureResponse(
                        messageEn: "A user with this phone number already exists.",
                        messageAr: "رقم الهاتف للمسؤول مستخدم بالفعل.");
            }
        }

        // All validations passed; modify only explicitly provided fields.
        if (dto.EnglishName is not null) association.EnglishName = englishName;
        if (dto.ArabicName is not null) association.ArabicName = arabicName;
        if (dto.CityId.HasValue) association.CityId = cityId;
        if (dto.LocationOnGoogleMaps is not null)
            association.LocationOnGoogleMaps = string.IsNullOrWhiteSpace(dto.LocationOnGoogleMaps)
                ? null : dto.LocationOnGoogleMaps.Trim();
        if (dto.Latitude is not null) association.Latitude = latitude;
        if (dto.Longitude is not null) association.Longitude = longitude;
        if (dto.ContactEmail is not null) association.ContactEmail = dto.ContactEmail.Trim();
        if (dto.ContactPhone is not null) association.ContactPhone = dto.ContactPhone.Trim();

        if (admin is not null && dto.Admin is not null)
        {
            if (dto.Admin.FirstNameEn is not null) admin.FirstNameEn = dto.Admin.FirstNameEn.Trim();
            if (dto.Admin.LastNameEn is not null) admin.LastNameEn = dto.Admin.LastNameEn.Trim();
            if (dto.Admin.FirstNameAr is not null) admin.FirstNameAr = dto.Admin.FirstNameAr.Trim();
            if (dto.Admin.LastNameAr is not null) admin.LastNameAr = dto.Admin.LastNameAr.Trim();
            if (dto.Admin.Email is not null) admin.Email = dto.Admin.Email.Trim();
            if (dto.Admin.PhoneNumber is not null) admin.PhoneNumber = dto.Admin.PhoneNumber.Trim();
        }

        try
        {
            // One tracked DbContext save persists both entities atomically.
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (ex.GetType().FullName == "Microsoft.EntityFrameworkCore.DbUpdateException")
        {
            return new ApiResponse<AssociationUpdateResponseDto>(
                false,
                "تعذر حفظ التعديلات بسبب تعارض أو خطأ في البيانات.",
                "Unable to save changes due to a database conflict or constraint.",
                default, 409);
        }

        var updated = await MapExistingAsync(association.Id, cancellationToken);
        // Include admin in the response even when only association fields were changed.
        admin ??= await _unitOfWork.Users.GetAssociationAdminForUpdateAsync(
            association.Id, cancellationToken);

        var data = new AssociationUpdateResponseDto
        {
            Id = updated.Id,
            EnglishName = updated.EnglishName,
            ArabicName = updated.ArabicName,
            LocationOnGoogleMaps = updated.LocationOnGoogleMaps,
            Latitude = updated.Latitude,
            Longitude = updated.Longitude,
            ContactPhone = updated.ContactPhone,
            ContactEmail = updated.ContactEmail,
            CountryId = updated.CountryId,
            CountryEnglishName = updated.CountryEnglishName,
            CountryArabicName = updated.CountryArabicName,
            CityId = updated.CityId,
            CityEnglishName = updated.CityEnglishName,
            CityArabicName = updated.CityArabicName,
            IsDeleted = updated.IsDeleted,
            WorkersCount = updated.WorkersCount,
            Admin = admin is null ? null : new AssociationAdminResponseDto
            {
                UserId = admin.UserId,
                FirstNameEn = admin.FirstNameEn,
                LastNameEn = admin.LastNameEn,
                FirstNameAr = admin.FirstNameAr,
                LastNameAr = admin.LastNameAr,
                Email = admin.Email,
                PhoneNumber = admin.PhoneNumber
            }
        };

        return ApiResponse<AssociationUpdateResponseDto>.SuccessResponse(
            data,
            messageEn: "Association updated successfully",
            messageAr: "تم تحديث الجمعية بنجاح");
    }


    public async Task<ApiResponse<bool>> DeleteAsync(
        int currentUserId,
        int associationId,
        CancellationToken cancellationToken = default)
    {
        var (currentUser, authError) =
            await AuthorizeAsync<bool>(
                currentUserId,
                DeleteScope,
                cancellationToken);

        if (authError is not null)
            return authError;

        var tenantError =
            EnsureSameAssociation<bool>(associationId, currentUser!);

        if (tenantError is not null)
            return tenantError;

        // Load the real tracked Association.
        var association = await _unitOfWork.Associations
            .GetByIdForUpdateAsync(
                associationId,
                cancellationToken);

        if (association is null)
        {
            return new ApiResponse<bool>(
                false,
                "لم يتم العثور على الجمعية.",
                "Association not found.",
                default,
                404);
        }

        // Load tracked users and workers.
        var users = await _unitOfWork.Users
            .GetAssociationUsersForUpdateAsync(
                associationId,
                cancellationToken);

        var workers = await _unitOfWork.Workers
            .GetByAssociationIdForUpdateAsync(
                associationId,
                cancellationToken);

        // One timestamp for the entire delete operation.
        var deletedAt = DateTime.UtcNow;

        // Soft delete the association.
        association.IsDeleted = true;
        association.DeletedAt = deletedAt;

        // Soft delete only currently active users.
        foreach (var user in users)
        {
            if (user.IsDeleted)
                continue;

            user.IsDeleted = true;
            user.DeletedAt = deletedAt;
        }

        // Soft delete only currently active workers.
        foreach (var worker in workers)
        {
            if (worker.IsDeleted)
                continue;

            worker.IsDeleted = true;
            worker.DeletedAt = deletedAt;
        }

        // One SaveChanges persists the tracked updates atomically.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.SuccessResponse(
            true,
            messageEn: "Association deleted successfully",
            messageAr: "تم حذف الجمعية بنجاح");
    }



    public async Task<ApiResponse<AssociationDto>> RestoreAsync(
        int currentUserId,
        int associationId,
        CancellationToken cancellationToken = default)
    {
        var (currentUser, authError) =
            await AuthorizeAsync<AssociationDto>(
                currentUserId,
                DeleteScope,
                cancellationToken);

        if (authError is not null)
            return authError;

        var tenantError =
            EnsureSameAssociation<AssociationDto>(
                associationId,
                currentUser!);

        if (tenantError is not null)
            return tenantError;

        // Load the deleted Association with tracking.
        var association = await _unitOfWork.Associations
            .GetByIdIncludingDeletedAsync(
                associationId,
                cancellationToken);

        if (association is null)
        {
            return new ApiResponse<AssociationDto>(
                false,
                "لم يتم العثور على الجمعية.",
                "Association not found.",
                default,
                404);
        }

        if (!association.IsDeleted)
        {
            return ApiResponse<AssociationDto>.FailureResponse(
                messageEn: "Association is not deleted.",
                messageAr: "الجمعية غير محذوفة أصلاً.");
        }

        // Identify records deleted with this association.
        var cascadeDeletedAt = association.DeletedAt;

        var deletedUsers = await _unitOfWork.Users
            .GetDeletedAssociationUsersForUpdateAsync(
                associationId,
                cancellationToken);

        var deletedWorkers = await _unitOfWork.Workers
            .GetDeletedByAssociationIdAsync(
                associationId,
                cancellationToken);

        // Restore the association.
        association.IsDeleted = false;
        association.DeletedAt = null;

        if (cascadeDeletedAt.HasValue)
        {
            // Restore only users deleted with the association.
            foreach (var user in deletedUsers)
            {
                if (user.DeletedAt != cascadeDeletedAt)
                    continue;

                user.IsDeleted = false;
                user.DeletedAt = null;
            }

            // Restore only workers deleted with the association.
            foreach (var worker in deletedWorkers)
            {
                if (worker.DeletedAt != cascadeDeletedAt)
                    continue;

                worker.IsDeleted = false;
                worker.DeletedAt = null;
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var data = await MapExistingAsync(
            association.Id,
            cancellationToken);

        return ApiResponse<AssociationDto>.SuccessResponse(
            data,
            messageEn: "Association restored successfully",
            messageAr: "تم استعادة الجمعية بنجاح");
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
            return (null, new ApiResponse<T>(false, "المستخدم الحالي غير موجود.", "Current user was not found.", default, 401));
        }

        if (!currentUser.IsActive)
        {
            return (null, new ApiResponse<T>(false, "المستخدم الحالي غير فعّال.", "Current user is inactive.", default, 401));
        }

        if (!GetEffectivePermissionIds(currentUser).Overlaps(anyOfPermissionIds))
        {
            return (null, new ApiResponse<T>(false, "لا تملك الصلاحية المطلوبة لإدارة الجمعيات.", "You do not hold the required Associations permission.", default, 403));
        }

        return (currentUser, null);
    }


    // Association-scoped users cannot access another association by supplying its ID.
    private static ApiResponse<T>? EnsureSameAssociation<T>(int associationId, User currentUser)
    {
        if (currentUser.EntityType == EntityType.Association && currentUser.EntityId != associationId)
        {
            return new ApiResponse<T>(
                false,
                "لا تملك صلاحية الوصول لبيانات جمعية أخرى.",
                "Cannot access data outside your own association.",
                default,
                403);
        }

        return null;
    }

    private static ApiResponse<AssociationDto> HandleDbUpdateException(Exception ex)
    {
        var innerMsg = ex.InnerException?.Message ?? ex.Message;

        if (innerMsg.Contains("IX_Associations_ArabicName_EnglishName") || innerMsg.Contains("ArabicName"))
        {
            return ApiResponse<AssociationDto>.FailureResponse(messageEn: "An association with the same Arabic and English name already exists", messageAr: "توجد جمعية مسجلة بنفس الاسم العربي والإنجليزي معاً");
        }

        if (innerMsg.Contains("IX_Associations_Latitude_Longitude") || innerMsg.Contains("Latitude"))
        {
            return ApiResponse<AssociationDto>.FailureResponse(messageEn: "An association with the same geographic coordinates already exists", messageAr: "توجد جمعية مسجلة بنفس الإحداثيات الجغرافية");
        }

        if (innerMsg.Contains("IX_Associations_ContactPhone") || innerMsg.Contains("ContactPhone"))
        {
            return ApiResponse<AssociationDto>.FailureResponse(messageEn: "The contact phone number is already used by another association", messageAr: "رقم الهاتف مستخدم بالفعل لجمعية أخرى");
        }

        return ApiResponse<AssociationDto>.FailureResponse(messageEn: "An error occurred while saving data to the database", messageAr: "حدث خطأ أثناء حفظ البيانات بقاعدة البيانات");
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
            return ApiResponse<T>.FailureResponse(messageEn: $"{fieldLabelEn} is required.", messageAr: $"{fieldLabelAr} مطلوب.");
        }

        if (value.Trim().Length > MaxNameLength)
        {
            return ApiResponse<T>.FailureResponse(messageEn: $"{fieldLabelEn} cannot exceed {MaxNameLength} characters.", messageAr: $"{fieldLabelAr} لا يمكن أن يتجاوز {MaxNameLength} حرف.");
        }

        return null;
    }

    private static (decimal Result, ApiResponse<T>? Error) ParseCoordinate<T>(string? value, string fieldLabelEn, string fieldLabelAr)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return (0, ApiResponse<T>.FailureResponse(messageEn: $"{fieldLabelEn} is required.", messageAr: $"{fieldLabelAr} مطلوب."));
        }

        if (!decimal.TryParse(value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
        {
            return (0, ApiResponse<T>.FailureResponse(messageEn: $"{fieldLabelEn} is not a valid number.", messageAr: $"{fieldLabelAr} ليس رقماً صالحاً."));
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