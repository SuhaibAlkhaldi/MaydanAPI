using Maydan.Application.DTOs.Common;
using Maydan.Application.DTOs.Projects;
using Maydan.Application.DTOs.UserManagement;
using Maydan.Application.Interfaces;
using Maydan.Domain.Entities;
using Maydan.Domain.Enums;

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

        if (currentUser!.EntityType != EntityType.ProductionCompany)
        {
            return new ApiResponse<ProjectDto>(false, "يمكن لمستخدمي شركات الإنتاج فقط إنشاء المشاريع.", "Only Production Company users can create projects.", default, 403);
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

        var (producer, producerError) = await GetSameCompanyUserAsync<ProjectDto>(request.ProducerUserId, currentUser, "المنتج", "Producer", cancellationToken);
        if (producerError is not null)
        {
            return producerError;
        }

        var (locationManager, lmError) = await GetSameCompanyUserAsync<ProjectDto>(request.LocationManagerUserId, currentUser, "مدير الموقع", "Location Manager", cancellationToken);
        if (lmError is not null)
        {
            return lmError;
        }

        var hasLocationManagerOverlap =
            await _unitOfWork.Projects.HasLocationManagerOverlapAsync(
                locationManager!.UserId,
                request.StartDate,
                request.EndDate,
                excludedProjectId: null,
                cancellationToken: cancellationToken);

        if (hasLocationManagerOverlap)
        {
            return ApiResponse<ProjectDto>.FailureResponse(
                "مدير الموقع مسند إلى مشروع آخر خلال هذه الفترة.",
                "Location Manager is already assigned to another Project during these dates.");
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
            ProductionCompanyId = currentUser.EntityId,
            CreatedBy = currentUser.UserId,
            IsActive = true,
            CityId = request.CityId,
            CityLocationId = request.CityLocationId
        };

        await _unitOfWork.Projects.AddAsync(project, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var createdProject = await _unitOfWork.Projects.GetByIdAsync(project.Id, cancellationToken);
        if (createdProject is null)
        {
            return new ApiResponse<ProjectDto>(false, "تعذر تحميل المشروع بعد الإنشاء.", "Project could not be loaded after creation.", default, 500);
        }

        return ApiResponse<ProjectDto>.SuccessResponse(MapToDto(createdProject), "Project created successfully.", "تم إنشاء المشروع بنجاح.", 201);
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
            cancellationToken);

        var filteredProjects = projects
            .Where(project => CanViewProject(currentUser!, project))
            .Select(MapToDto)
            .ToList();

        return ApiResponse<List<ProjectDto>>.SuccessResponse(filteredProjects, "Projects retrieved successfully.", "تم جلب المشاريع بنجاح.");
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
            return new ApiResponse<ProjectDto>(false, "المشروع غير موجود.", "Project was not found.", default, 404);
        }

        if (!CanViewProject(currentUser!, project))
        {
            return new ApiResponse<ProjectDto>(false, "لا تملك صلاحية الوصول لهذا المشروع.", "Cannot view a project outside the caller's authorized scope.", default, 403);
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
            return new ApiResponse<ProjectDto>(false, "المشروع غير موجود.", "Project was not found.", default, 404);
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

        var (producer, producerError) = await GetSameCompanyUserAsync<ProjectDto>(request.ProducerUserId, currentUser!, "المنتج", "Producer", cancellationToken);
        if (producerError is not null)
        {
            return producerError;
        }

        var (locationManager, lmError) = await GetSameCompanyUserAsync<ProjectDto>(request.LocationManagerUserId, currentUser!, "مدير الموقع", "Location Manager", cancellationToken);
        if (lmError is not null)
        {
            return lmError;
        }

        var hasLocationManagerOverlap =
            await _unitOfWork.Projects.HasLocationManagerOverlapAsync(
                locationManager!.UserId,
                request.StartDate,
                request.EndDate,
                excludedProjectId: project.Id,
                cancellationToken: cancellationToken);

        if (hasLocationManagerOverlap)
        {
            return ApiResponse<ProjectDto>.FailureResponse(
                "مدير الموقع مسند إلى مشروع آخر خلال هذه الفترة.",
                "Location Manager is already assigned to another Project during these dates.");
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

        if (!string.IsNullOrWhiteSpace(request.WorkPermitImagePath))
        {
            project.WorkPermitImagePath = request.WorkPermitImagePath;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var updatedProject = await _unitOfWork.Projects.GetByIdAsync(project.Id, cancellationToken);
        if (updatedProject is null)
        {
            return new ApiResponse<ProjectDto>(false, "تعذر تحميل المشروع بعد التحديث.", "Project could not be loaded after update.", default, 500);
        }

        return ApiResponse<ProjectDto>.SuccessResponse(MapToDto(updatedProject), "Project updated successfully.", "تم تحديث بيانات المشروع بنجاح.");
    }

    public async Task<ApiResponse<object?>> DeleteAsync(
        int id,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        var (currentUser, authError) = await AuthorizeAsync<object?>(currentUserId, new[] { DeleteProjectsPermissionId, ManageProjectsPermissionId }, cancellationToken);
        if (authError is not null)
        {
            return authError;
        }

        var project = await _unitOfWork.Projects.GetByIdAsync(id, cancellationToken);
        if (project is null)
        {
            return new ApiResponse<object?>(false, "المشروع غير موجود.", "Project was not found.", default, 404);
        }

        var companyError = EnsureSameProductionCompany<object?>(project, currentUser!);
        if (companyError is not null)
        {
            return companyError;
        }

        _unitOfWork.Projects.Remove(project);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<object?>.SuccessResponse(null, "Project deleted successfully.", "تم حذف المشروع بنجاح.");
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
            return new ApiResponse<ProjectDto>(false, "المشروع غير موجود.", "Project was not found.", default, 404);
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

        if (currentUser!.EntityType != EntityType.ProductionCompany)
        {
            return ApiResponse<List<UserSummaryDto>>.FailureResponse(
                "Only Production Company users can query available users.",
                "يمكن لمستخدمي شركات الإنتاج فقط الاستعلام عن المستخدمين المتاحين.");
        }

        if (endDate <= startDate)
        {
            return ApiResponse<List<UserSummaryDto>>.FailureResponse(
                "End date must be greater than start date.",
                "تاريخ الانتهاء يجب أن يكون بعد تاريخ البداية.");
        }


        var busyUserIds = await _unitOfWork.Projects.GetBusyUserIdsAsync(
            currentUser.EntityId, startDate, endDate, currentProjectId, cancellationToken);


        var allCompanyUsers = await _unitOfWork.Users.GetByEntityAsync(
            EntityType.ProductionCompany, currentUser.EntityId, search: null, cancellationToken);

   
        var availableUsers = allCompanyUsers
           // 5==producer && 6== Location Manager
              .Where(u => u.RoleId == 5 || u.RoleId == 6)
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
            return (null, new ApiResponse<T>(false, "المستخدم الحالي غير موجود.", "Current user was not found.", default, 401));
        }

        if (!currentUser.IsActive)
        {
            return (null, new ApiResponse<T>(false, "المستخدم الحالي غير فعّال.", "Current user is inactive.", default, 401));
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
            return (null, new ApiResponse<T>(false, "لا تملك الصلاحية المطلوبة لإدارة المشاريع.", "You do not hold the required Projects permission.", default, 403));
        }

        return (currentUser, null);
    }

    private async Task<(User? User, ApiResponse<T>? Error)> GetSameCompanyUserAsync<T>(
        int userId,
        User currentUser,
        string roleLabelAr,
        string roleLabelEn,
        CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return (null, new ApiResponse<T>(false, $"المستخدم المحدد لـ ({roleLabelAr}) غير موجود.", $"{roleLabelEn} not found.", default, 404));
        }

        if (user.EntityType != EntityType.ProductionCompany || user.EntityId != currentUser.EntityId)
        {
            return (null, ApiResponse<T>.FailureResponse($"{roleLabelEn} must belong to the same Production Company.", $"يجب أن ينتمي ({roleLabelAr}) إلى نفس شركة الإنتاج."));
        }

        return (user, null);
    }

    private static ApiResponse<T>? EnsureSameProductionCompany<T>(Project project, User currentUser)
    {
        if (project.ProductionCompanyId != currentUser.EntityId)
        {
            return new ApiResponse<T>(false, "لا يمكنك إدارة مشروع خارج شركة الإنتاج الخاصة بك.", "Cannot manage a project outside the current Production Company.", default, 403);
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
            return ApiResponse<T>.FailureResponse("Resource or schedule overlap detected.", overlapErrorMsg);
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
            ProjectTypeNameEn = project.ProjectType.NameEn,
            ProjectTypeNameAr = project.ProjectType.NameAr,
            ProducerUserId = project.ProducerUserId,
            ProducerNameEn = $"{project.Producer.FirstNameEn} {project.Producer.LastNameEn}",
            ProducerNameAr = $"{project.Producer.FirstNameAr} {project.Producer.LastNameAr}",
            LocationManagerUserId = project.LocationManagerUserId,
            LocationManagerNameEn = $"{project.LocationManager.FirstNameEn} {project.LocationManager.LastNameEn}",
            LocationManagerNameAr = $"{project.LocationManager.FirstNameAr} {project.LocationManager.LastNameAr}",
            WorkPermitImagePath = project.WorkPermitImagePath,
            ProductionCompanyId = project.ProductionCompanyId,
            IsDeleted = project.IsDeleted,
            CityId = project.CityId,
            CityName = project.City?.EnglishName ?? string.Empty,
            CityNameAr = project.City?.ArabicName ?? string.Empty,
            CityLocationId = project.CityLocationId,
            CityLocationName = project.CityLocation?.EnglishName ?? string.Empty
        };
    }

    #endregion
}