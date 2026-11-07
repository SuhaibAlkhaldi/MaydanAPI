using Maydan.Application.DTOs.Common;
using Maydan.Application.DTOs.Projects;
using Maydan.Application.DTOs.UserManagement;
using Maydan.Application.Interfaces;
using Maydan.Domain.Entities;
using Maydan.Domain.Enums;
using Maydan.Domain.Constants;
using NetTopologySuite.Geometries;

namespace Maydan.Application.Services;

public class ProjectService : IProjectService
{
    private const int ViewProjectsPermissionId = 25;
    private const int CreateProjectsPermissionId = 26;
    private const int EditProjectsPermissionId = 27;
    private const int DeleteProjectsPermissionId = 28;
    private const int ReviewProjectsPermissionId = 29;
    private const int ManageProjectsPermissionId = 30;

    private readonly IUnitOfWork _unitOfWork;

    public ProjectService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<ProjectDto>> CreateAsync(
        CreateProjectDto request,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        var (currentUser, authError) = await AuthorizeAsync<ProjectDto>(currentUserId, new[] { CreateProjectsPermissionId, ManageProjectsPermissionId }, cancellationToken);
        if (authError is not null)
        {
            return authError;
        }

        // 1. تحديد ما إذا كان المنشئ من بيت الأردن أو شركة إنتاج
        bool isBaytAlUrdon = currentUser!.EntityType == EntityType.BaytAlUrdon;
        bool isProductionCompany = currentUser.EntityType == EntityType.ProductionCompany;

        if (!isBaytAlUrdon && !isProductionCompany)
        {
            return ApiResponse<ProjectDto>.FailureResponse("Only Production Company or Bayt Al Urdon users can create projects.", "يمكن لمستخدمي شركات الإنتاج أو بيت الأردن فقط إنشاء المشاريع.");
        }

        // 2. تحديد ProductionCompanyId بناءً على نوع المستخدم
        int targetProductionCompanyId;
        if (isProductionCompany)
        {
            targetProductionCompanyId = currentUser.EntityId;
        }
        else
        {
            // إذا كان المستخدم بيت الأردن، يجب أن يوفر ProductionCompanyId
            if (!request.ProductionCompanyId.HasValue || request.ProductionCompanyId.Value <= 0)
            {
                return ApiResponse<ProjectDto>.FailureResponse("Production Company ID is required when Bayt Al Urdon creates a project.", "معرف شركة الإنتاج مطلوب عندما يقوم بيت الأردن بإنشاء مشروع.");
            }
            targetProductionCompanyId = request.ProductionCompanyId.Value;
        }

        var validationError = await ValidateProjectPayloadAsync<ProjectDto>(
            request.ProjectNameEn,
            request.ProjectNameAr,
            request.StartDate,
            request.EndDate,
            request.ProducerUserId,
            request.LocationManagerUserId,
            request.CityId,
            request.CityLocationId,
            null,
            cancellationToken);

        if (validationError is not null)
        {
            return validationError;
        }

        if (!await _unitOfWork.ProjectTypes.ExistsAsync(request.ProjectTypeId, cancellationToken))
        {
            return ApiResponse<ProjectDto>.FailureResponse("Invalid Project Type.", "نوع المشروع غير صالح.");
        }

        var (producer, producerError) = await GetTargetCompanyUserAsync<ProjectDto>(request.ProducerUserId, targetProductionCompanyId, "المنتج", "Producer", cancellationToken);
        if (producerError is not null)
        {
            return producerError;
        }

        var (locationManager, lmError) = await GetTargetCompanyUserAsync<ProjectDto>(request.LocationManagerUserId, targetProductionCompanyId, "مدير الموقع", "Location Manager", cancellationToken);
        if (lmError is not null)
        {
            return lmError;
        }

        Point? projectLocation = null;
        bool hasGeoWarning = false;
        if (request.Latitude.HasValue && request.Longitude.HasValue)
        {
            projectLocation = new Point(request.Longitude.Value, request.Latitude.Value) { SRID = 4326 };
            var matchedAssociation = await _unitOfWork.Associations.FindByGeofenceContainingAsync(projectLocation, cancellationToken);
            if (matchedAssociation is null)
            {
                hasGeoWarning = true;
            }
        }

        var project = new Project
        {
            ProjectNameEn = request.ProjectNameEn.Trim(),
            ProjectNameAr = request.ProjectNameAr.Trim(),
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            ProjectTypeId = request.ProjectTypeId,
            ProducerUserId = producer!.UserId,
            LocationManagerUserId = locationManager!.UserId,
            WorkPermitImagePath = request.WorkPermitImagePath,
            ProductionCompanyId = targetProductionCompanyId,
            CreatedBy = currentUser.UserId,
            // 3. التفعيل التلقائي إذا كان المنشئ من بيت الأردن
            IsActive = isBaytAlUrdon,
            CityId = request.CityId,
            CityLocationId = request.CityLocationId,
            Location = projectLocation
        };

        await _unitOfWork.Projects.AddAsync(project, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var createdProject = await _unitOfWork.Projects.GetByIdAsync(project.Id, cancellationToken);
        if (createdProject is null)
        {
            return ApiResponse<ProjectDto>.FailureResponse("Project could not be loaded after creation.", "تعذر تحميل المشروع بعد الإنشاء.");
        }

        string msgEn = isBaytAlUrdon ? "Project created and approved successfully." : "Project created successfully and is pending approval.";
        string msgAr = isBaytAlUrdon ? "تم إنشاء المشروع وتفعيله بنجاح." : "تم إنشاء المشروع بنجاح وهو بانتظار موافقة جهة الميدان (بيت الأردن).";

        return ApiResponse<ProjectDto>.SuccessResponse(MapToDto(createdProject), msgEn, msgAr, 201);
    }

    public async Task<ApiResponse<List<ProjectDto>>> GetAllAsync(
        int currentUserId,
        ProjectQueryDto query,
        CancellationToken cancellationToken = default)
    {
        var (currentUser, authError) = await AuthorizeUserAsync<List<ProjectDto>>(currentUserId, cancellationToken);
        if (authError is not null)
        {
            return authError;
        }

        var projects = await _unitOfWork.Projects.GetAllAsync(
            query.IsDeleted,
            query.SearchTerm,
            query.StartDate,
            query.EndDate,
            query.SearchByProductionCompanyName,
            query.SearchByProductionCompanyId,
            query.CityId,
            query.CityLocationId,
            query.IsActive,
            cancellationToken);

        var filteredProjects = projects
            .Where(project => CanViewProject(currentUser!, project))
            .Select(MapToDto)
            .ToList();

        return ApiResponse<List<ProjectDto>>.SuccessResponse(filteredProjects, "Projects retrieved successfully.", "تم جلب المشاريع بنجاح.");
    }

    public async Task<ApiResponse<ProjectDto>> ApproveAsync(int id, int currentUserId, CancellationToken cancellationToken = default)
    {
        var (currentUser, authError) = await AuthorizeUserAsync<ProjectDto>(currentUserId, cancellationToken);
        if (authError is not null) return authError;

        if (currentUser!.EntityType != EntityType.BaytAlUrdon)
        {
            return ApiResponse<ProjectDto>.FailureResponse("Only Bayt Al Urdon can approve projects.", "غير مصرح لك باعتماد المشاريع.");
        }

        var project = await _unitOfWork.Projects.GetByIdAsync(id, cancellationToken);
        if (project is null)
        {
            return ApiResponse<ProjectDto>.FailureResponse("Project was not found.", "المشروع غير موجود.");
        }

        if (project.IsActive)
        {
            return ApiResponse<ProjectDto>.FailureResponse("Project is already active.", "المشروع مفعل مسبقاً.");
        }

        var overlapError = await _unitOfWork.Projects
    .CheckOverlapConflictsAsync(
        project.StartDate,
        project.EndDate,
        project.ProducerUserId,
        project.LocationManagerUserId,
        project.CityLocationId,
        project.Id,
        cancellationToken);

        if (overlapError is not null)
        {
            return ApiResponse<ProjectDto>.FailureResponse(
                overlapError,
                "Cannot approve project due to scheduling conflicts.");
        }
        project.IsActive = true;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<ProjectDto>.SuccessResponse(MapToDto(project), "Project approved successfully.", "تم اعتماد وتفعيل المشروع بنجاح.");
    }

    public async Task<ApiResponse<ProjectDto>> GetByIdAsync(
        int currentUserId,
        int id,
        CancellationToken cancellationToken = default)
    {
        var (currentUser, authError) = await AuthorizeUserAsync<ProjectDto>(currentUserId, cancellationToken);
        if (authError is not null)
        {
            return authError;
        }

        var project = await _unitOfWork.Projects.GetByIdAsync(id, cancellationToken);
        if (project is null)
        {
            return ApiResponse<ProjectDto>.FailureResponse("Project was not found.", "المشروع غير موجود.");
        }

        if (!CanViewProject(currentUser!, project))
        {
            return ApiResponse<ProjectDto>.FailureResponse("Cannot view a project outside the caller's authorized scope.", "لا تملك صلاحية الوصول لهذا المشروع.");
        }

        return ApiResponse<ProjectDto>.SuccessResponse(MapToDto(project), "Project retrieved successfully.", "تم جلب بيانات المشروع بنجاح.");
    }

    public async Task<ApiResponse<ProjectDto>> UpdateAsync(
        int id,
        UpdateProjectDto request,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        var (currentUser, authError) = await AuthorizeAsync<ProjectDto>(currentUserId, new[] { EditProjectsPermissionId, ManageProjectsPermissionId }, cancellationToken);
        if (authError is not null)
        {
            return authError;
        }

        var project = await _unitOfWork.Projects.GetByIdAsync(id, cancellationToken);
        if (project is null)
        {
            return ApiResponse<ProjectDto>.FailureResponse("Project was not found.", "المشروع غير موجود.");
        }

        var companyError = EnsureSameProductionCompany<ProjectDto>(project, currentUser!);
        if (companyError is not null)
        {
            return companyError;
        }

        var validationError = await ValidateProjectPayloadAsync<ProjectDto>(
            request.ProjectNameEn,
            request.ProjectNameAr,
            request.StartDate,
            request.EndDate,
            request.ProducerUserId,
            request.LocationManagerUserId,
            request.CityId,
            request.CityLocationId,
            project.Id,
            cancellationToken);

        if (validationError is not null)
        {
            return validationError;
        }

        if (!await _unitOfWork.ProjectTypes.ExistsAsync(request.ProjectTypeId, cancellationToken))
        {
            return ApiResponse<ProjectDto>.FailureResponse("Invalid Project Type.", "نوع المشروع غير صالح.");
        }

        // Use GetTargetCompanyUserAsync to validate against the project's company, not necessarily the current user's
        var (producer, producerError) = await GetTargetCompanyUserAsync<ProjectDto>(request.ProducerUserId, project.ProductionCompanyId, "المنتج", "Producer", cancellationToken);
        if (producerError is not null)
        {
            return producerError;
        }

        var (locationManager, lmError) = await GetTargetCompanyUserAsync<ProjectDto>(request.LocationManagerUserId, project.ProductionCompanyId, "مدير الموقع", "Location Manager", cancellationToken);
        if (lmError is not null)
        {
            return lmError;
        }

        project.ProjectNameEn = request.ProjectNameEn.Trim();
        project.ProjectNameAr = request.ProjectNameAr.Trim();
        project.StartDate = request.StartDate;
        project.EndDate = request.EndDate;
        project.ProjectTypeId = request.ProjectTypeId;
        project.ProducerUserId = producer!.UserId;
        project.LocationManagerUserId = locationManager!.UserId;
        project.CityId = request.CityId;
        project.CityLocationId = request.CityLocationId;
        if (request.Latitude.HasValue && request.Longitude.HasValue)
        {
            project.Location = new Point(request.Longitude.Value, request.Latitude.Value) { SRID = 4326 };
        }

        if (!string.IsNullOrWhiteSpace(request.WorkPermitImagePath))
        {
            project.WorkPermitImagePath = request.WorkPermitImagePath;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var updatedProject = await _unitOfWork.Projects.GetByIdAsync(project.Id, cancellationToken);
        if (updatedProject is null)
        {
            return ApiResponse<ProjectDto>.FailureResponse("Project could not be loaded after update.", "تعذر تحميل المشروع بعد التحديث.");
        }

        return ApiResponse<ProjectDto>.SuccessResponse(MapToDto(updatedProject), "Project updated successfully.", "تم تحديث بيانات المشروع بنجاح.");
    }

    public async Task<ApiResponse<bool>> DeleteAsync(
        int id,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        var (currentUser, authError) = await AuthorizeAsync<bool>(currentUserId, new[] { DeleteProjectsPermissionId, ManageProjectsPermissionId }, cancellationToken);
        if (authError is not null)
        {
            return authError;
        }

        var project = await _unitOfWork.Projects.GetByIdAsync(id, cancellationToken);
        if (project is null)
        {
            return ApiResponse<bool>.FailureResponse("Project was not found.", "المشروع غير موجود.");
        }

        var companyError = EnsureSameProductionCompany<bool>(project, currentUser!);
        if (companyError is not null)
        {
            return companyError;
        }

        _unitOfWork.Projects.Remove(project);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.SuccessResponse(true, "Project deleted successfully.", "تم حذف المشروع بنجاح.");
    }

    public async Task<ApiResponse<ProjectDto>> RestoreAsync(
        int id,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        var (currentUser, authError) = await AuthorizeAsync<ProjectDto>(currentUserId, new[] { DeleteProjectsPermissionId, ManageProjectsPermissionId }, cancellationToken);
        if (authError is not null)
        {
            return authError;
        }

        var project = await _unitOfWork.Projects.GetByIdIncludingDeletedAsync(id, cancellationToken);
        if (project is null)
        {
            return ApiResponse<ProjectDto>.FailureResponse("Project was not found.", "المشروع غير موجود.");
        }

        var companyError = EnsureSameProductionCompany<ProjectDto>(project, currentUser!);
        if (companyError is not null)
        {
            return companyError;
        }

        if (!project.IsDeleted)
        {
            return ApiResponse<ProjectDto>.FailureResponse("Project is not deleted.", "المشروع غير محذوف أصلًا.");
        }

        project.IsDeleted = false;
        project.DeletedAt = null;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var restoredProject = await _unitOfWork.Projects.GetByIdAsync(project.Id, cancellationToken);
        return ApiResponse<ProjectDto>.SuccessResponse(MapToDto(restoredProject!), "Project restored successfully.", "تم استرجاع المشروع بنجاح.");
    }

    public async Task<ApiResponse<List<ProjectTypeDto>>> GetProjectTypesAsync(
        CancellationToken cancellationToken = default)
    {
        var projectTypes = await _unitOfWork.ProjectTypes.GetAllAsync(cancellationToken);

        var dtos = projectTypes
            .Select(pt => new ProjectTypeDto(pt.Id, pt.NameEn, pt.NameAr))
            .ToList();

        return ApiResponse<List<ProjectTypeDto>>.SuccessResponse(dtos, "Project types retrieved successfully.", "تم جلب أنواع المشاريع بنجاح.");
    }

    public async Task<ApiResponse<List<UserSummaryDto>>> GetAvailableUsersAsync(
          int currentUserId,
          DateTime startDate,
          DateTime endDate,
          int? currentProjectId = null,
          CancellationToken cancellationToken = default)
    {
        var (currentUser, authError) = await AuthorizeUserAsync<List<UserSummaryDto>>(currentUserId, cancellationToken);
        if (authError is not null) return authError;

        // Allow both Production Company and BaytAlUrdon (if BaytAlUrdon needs to assign users when creating)
        if (currentUser!.EntityType != EntityType.ProductionCompany && currentUser.EntityType != EntityType.BaytAlUrdon)
        {
            return ApiResponse<List<UserSummaryDto>>.FailureResponse(
                "Only Production Company or Bayt Al Urdon users can query available users.",
                "يمكن لمستخدمي شركات الإنتاج أو بيت الأردن فقط الاستعلام عن المستخدمين المتاحين.");
        }

        if (endDate <= startDate)
        {
            return ApiResponse<List<UserSummaryDto>>.FailureResponse(
                "End date must be greater than start date.",
                "تاريخ الانتهاء يجب أن يكون بعد تاريخ البداية.");
        }

        // If BaytAlUrdon, they'd need to supply the ProductionCompanyId to query its users. 
        // For now, defaulting to querying based on currentUser.EntityId if it's a ProductionCompany.
        // *NOTE:* If BaytAlUrdon uses this endpoint, you might need to update the signature to accept a productionCompanyId.
        int targetCompanyId = currentUser.EntityType == EntityType.ProductionCompany ? currentUser.EntityId : 0;

        if (targetCompanyId == 0)
        {
            return ApiResponse<List<UserSummaryDto>>.FailureResponse(
               "Production Company ID is required for Bayt Al Urdon users to query available users.",
               "يجب توفير معرف شركة الإنتاج لمستخدمي بيت الأردن للاستعلام عن المستخدمين.");
        }

        var busyUserIds = await _unitOfWork.Projects.GetBusyUserIdsAsync(
            targetCompanyId, startDate, endDate, currentProjectId, cancellationToken);


        var allCompanyUsers = await _unitOfWork.Users.GetByEntityAsync(
            EntityType.ProductionCompany, targetCompanyId, search: null, cancellationToken);

        var availableUsers = allCompanyUsers
           // 5==producer && 6== Location Manager
              .Where(u => u.RoleId == SystemRoles.Producer || u.RoleId == SystemRoles.LocationManager)
                     .Where(u => !busyUserIds.Contains(u.UserId))
                         .Select(UserManagementService.MapUserSummary)
                             .ToList();

        return ApiResponse<List<UserSummaryDto>>.SuccessResponse(
            availableUsers,
            "Available users retrieved successfully.",
            "تم جلب المستخدمين المتاحين بنجاح.");
    }

    #region Helpers

    private async Task<(User? User, ApiResponse<T>? Error)> AuthorizeUserAsync<T>(int currentUserId, CancellationToken cancellationToken)
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

        return (currentUser, null);
    }

    private async Task<(User? User, ApiResponse<T>? Error)> AuthorizeAsync<T>(int currentUserId, int[] anyOfPermissionIds, CancellationToken cancellationToken)
    {
        var (currentUser, userError) = await AuthorizeUserAsync<T>(currentUserId, cancellationToken);
        if (userError is not null)
        {
            return (null, userError);
        }

        if (!GetEffectivePermissionIds(currentUser!).Overlaps(anyOfPermissionIds))
        {
            return (null, ApiResponse<T>.FailureResponse("You do not hold the required Projects permission.", "لا تملك الصلاحية المطلوبة لإدارة المشاريع."));
        }

        return (currentUser, null);
    }

    // Modified to check against a target Production Company ID instead of currentUser.EntityId
    private async Task<(User? User, ApiResponse<T>? Error)> GetTargetCompanyUserAsync<T>(
        int userId,
        int targetProductionCompanyId,
        string roleLabelAr,
        string roleLabelEn,
        CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return (null, ApiResponse<T>.FailureResponse($"{roleLabelEn} not found.", $"المستخدم المحدد لـ ({roleLabelAr}) غير موجود."));
        }

        if (user.EntityType != EntityType.ProductionCompany || user.EntityId != targetProductionCompanyId)
        {
            return (null, ApiResponse<T>.FailureResponse($"{roleLabelEn} must belong to the selected Production Company.", $"يجب أن ينتمي ({roleLabelAr}) إلى شركة الإنتاج المحددة للمشروع."));
        }

        return (user, null);
    }

    private static ApiResponse<T>? EnsureSameProductionCompany<T>(Project project, User currentUser)
    {
        // Bayt Al Urdon bypasses this check as they can manage across companies
        if (currentUser.EntityType == EntityType.BaytAlUrdon)
        {
            return null;
        }

        if (project.ProductionCompanyId != currentUser.EntityId)
        {
            return ApiResponse<T>.FailureResponse("Cannot manage a project outside the current Production Company.", "لا يمكنك إدارة مشروع خارج شركة الإنتاج الخاصة بك.");
        }

        return null;
    }


    private async Task<ApiResponse<T>?> ValidateProjectPayloadAsync<T>(
        string projectNameEn,
        string projectNameAr,
        DateTime startDate,
        DateTime endDate,
        int producerUserId,
        int locationManagerUserId,
        int cityId,
        int cityLocationId,
        int? excludeProjectId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(projectNameEn))
        {
            return ApiResponse<T>.FailureResponse("English project name is required.", "اسم المشروع باللغة الإنجليزية مطلوب.");
        }

        if (string.IsNullOrWhiteSpace(projectNameAr))
        {
            return ApiResponse<T>.FailureResponse("Arabic project name is required.", "اسم المشروع باللغة العربية مطلوب.");
        }

        if (projectNameEn.Trim().Length > 200)
        {
            return ApiResponse<T>.FailureResponse("English project name cannot exceed 200 characters.", "اسم المشروع باللغة الإنجليزية يجب ألا يتجاوز 200 حرف.");
        }

        if (projectNameAr.Trim().Length > 200)
        {
            return ApiResponse<T>.FailureResponse("Arabic project name cannot exceed 200 characters.", "اسم المشروع باللغة العربية يجب ألا يتجاوز 200 حرف.");
        }

        if (endDate <= startDate)
        {
            return ApiResponse<T>.FailureResponse("End date must be greater than start date.", "تاريخ الانتهاء يجب أن يكون بعد تاريخ البداية.");
        }

        if (producerUserId == locationManagerUserId)
        {
            return ApiResponse<T>.FailureResponse("Producer and Location Manager must be different users.", "يجب أن يكون المنتج ومدير الموقع شخصين مختلفين.");
        }

        var overlapErrorMsg = await _unitOfWork.Projects.CheckOverlapConflictsAsync(
             startDate,
             endDate,
             producerUserId,
             locationManagerUserId,
             cityLocationId,
             excludeProjectId,
             cancellationToken);

        if (overlapErrorMsg != null)
        {
            return ApiResponse<T>.FailureResponse(overlapErrorMsg, "تم اكتشاف تداخل في الموارد أو المواعيد.");
        }

        if (cityId <= 0)
        {
            return ApiResponse<T>.FailureResponse("City is required to register the project.", "المدينة مطلوبة لتسجيل المشروع.");
        }

        if (cityLocationId <= 0)
        {
            return ApiResponse<T>.FailureResponse("City location is required.", "موقع المدينة التفصيلي مطلوب.");
        }

        return null;
    }

    private static bool CanViewProject(User currentUser, Project project)
    {
        var permissionIds = GetEffectivePermissionIds(currentUser);
        var hasAllProjectsPermission = permissionIds.Overlaps(new[]
        {
            ViewProjectsPermissionId,
            ReviewProjectsPermissionId,
            ManageProjectsPermissionId
        });

        if (currentUser.EntityType is EntityType.BaytAlUrdon or EntityType.Aseza)
        {
            return hasAllProjectsPermission;
        }

        if (currentUser.EntityType != EntityType.ProductionCompany)
        {
            return false;
        }

        if (project.ProductionCompanyId != currentUser.EntityId)
        {
            return false;
        }

        return hasAllProjectsPermission ||
            project.ProducerUserId == currentUser.UserId ||
            project.LocationManagerUserId == currentUser.UserId;
    }

    private static HashSet<int> GetEffectivePermissionIds(User user)
    {
        var rolePermissionIds = user.Role?.RolePermissions?
         .Where(rp => rp.IsActive && rp.Permission?.IsActive == true)
         .Select(rp => rp.PermissionId) ?? Enumerable.Empty<int>();

        var directPermissionIds = user.UserPermissions?
            .Where(up => up.IsActive && up.Permission?.IsActive == true)
            .Select(up => up.PermissionId) ?? Enumerable.Empty<int>();

        var groupPermissionIds = user.UserGroups?
            .Where(ug => ug.Group != null && ug.Group.IsActive)
            .SelectMany(ug => ug.Group.GroupPermissions ?? new List<GroupPermission>())
            .Where(gp => gp.IsActive && gp.Permission?.IsActive == true)
            .Select(gp => gp.PermissionId) ?? Enumerable.Empty<int>();

        return rolePermissionIds.Concat(directPermissionIds).Concat(groupPermissionIds).ToHashSet();
    }

    private static ProjectDto MapToDto(Project project)
    {
        return new ProjectDto
        {
            Id = project.Id,
            ProjectNameEn = project.ProjectNameEn,
            ProjectNameAr = project.ProjectNameAr,
            StartDate = project.StartDate,
            EndDate = project.EndDate,
            ProjectTypeId = project.ProjectTypeId,
            ProjectTypeNameEn = project.ProjectType?.NameEn ?? string.Empty,
            ProjectTypeNameAr = project.ProjectType?.NameAr ?? string.Empty,
            ProducerUserId = project.ProducerUserId,
            ProducerNameEn = project.Producer != null ? $"{project.Producer.FirstNameEn} {project.Producer.LastNameEn}" : string.Empty,
            ProducerNameAr = project.Producer != null ? $"{project.Producer.FirstNameAr} {project.Producer.LastNameAr}" : string.Empty,
            LocationManagerUserId = project.LocationManagerUserId,
            LocationManagerNameEn = project.LocationManager != null ? $"{project.LocationManager.FirstNameEn} {project.LocationManager.LastNameEn}" : string.Empty,
            LocationManagerNameAr = project.LocationManager != null ? $"{project.LocationManager.FirstNameAr} {project.LocationManager.LastNameAr}" : string.Empty,
            WorkPermitImagePath = project.WorkPermitImagePath,
            ProductionCompanyId = project.ProductionCompanyId,
            IsDeleted = project.IsDeleted,
            IsActive = project.IsActive,
            CityId = project.CityId,
            CityName = project.City?.EnglishName ?? string.Empty,
            CityNameAr = project.City?.ArabicName ?? string.Empty,
            CityLocationId = project.CityLocationId,
            CityLocationName = project.CityLocation?.EnglishName ?? string.Empty,
            Latitude = project.Location?.Y,
            Longitude = project.Location?.X
        };
    }

    #endregion
}