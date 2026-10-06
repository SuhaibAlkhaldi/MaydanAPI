using Maydan.Application.DTOs.Common;
using Maydan.Application.DTOs.Projects;
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
            return Fail<ProjectDto>("يمكن لمستخدمي شركات الإنتاج فقط إنشاء المشاريع.", "Only Production Company users can create projects.");
        }

        var validationError = ValidateProjectPayload<ProjectDto>(
            request.ProjectNameEn,
            request.ProjectNameAr,
            request.StartDate,
            request.EndDate,
            request.ProducerUserId,
            request.LocationManagerUserId);

        if (validationError is not null)
        {
            return validationError;
        }

        if (!await _unitOfWork.ProjectTypes.ExistsAsync(request.ProjectTypeId, cancellationToken))
        {
            return Fail<ProjectDto>("نوع المشروع غير صالح.", "Invalid Project Type.");
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
            IsActive = true
        };

        await _unitOfWork.Projects.AddAsync(project, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var createdProject = await _unitOfWork.Projects.GetByIdAsync(project.Id, cancellationToken);
        if (createdProject is null)
        {
            return Fail<ProjectDto>("تعذر تحميل المشروع بعد الإنشاء.", "Project could not be loaded after creation.");
        }

        return Ok(MapToDto(createdProject), "تم إنشاء المشروع بنجاح.", "Project created successfully.", statusCode: 201);
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
            cancellationToken);

        var filteredProjects = projects
            .Where(project => CanViewProject(currentUser!, project))
            .Select(MapToDto)
            .ToList();

        return Ok(filteredProjects, "تم جلب المشاريع بنجاح.", "Projects retrieved successfully.");
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
            return Fail<ProjectDto>("المشروع غير موجود.", "Project was not found.");
        }

        if (!CanViewProject(currentUser!, project))
        {
            return Fail<ProjectDto>("لا تملك صلاحية الوصول لهذا المشروع.", "Cannot view a project outside the caller's authorized scope.", statusCode: 403);
        }

        return Ok(MapToDto(project), "تم جلب بيانات المشروع بنجاح.", "Project retrieved successfully.");
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
            return Fail<ProjectDto>("المشروع غير موجود.", "Project was not found.");
        }

        var companyError = EnsureSameProductionCompany<ProjectDto>(project, currentUser!);
        if (companyError is not null)
        {
            return companyError;
        }

        var validationError = ValidateProjectPayload<ProjectDto>(
            request.ProjectNameEn,
            request.ProjectNameAr,
            request.StartDate,
            request.EndDate,
            request.ProducerUserId,
            request.LocationManagerUserId);

        if (validationError is not null)
        {
            return validationError;
        }

        if (!await _unitOfWork.ProjectTypes.ExistsAsync(request.ProjectTypeId, cancellationToken))
        {
            return Fail<ProjectDto>("نوع المشروع غير صالح.", "Invalid Project Type.");
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

        project.ProjectNameEn = request.ProjectNameEn.Trim();
        project.ProjectNameAr = request.ProjectNameAr.Trim();
        project.StartDate = request.StartDate;
        project.EndDate = request.EndDate;
        project.ProjectTypeId = request.ProjectTypeId;
        project.ProducerUserId = producer!.UserId;
        project.LocationManagerUserId = locationManager!.UserId;

        if (!string.IsNullOrWhiteSpace(request.WorkPermitImagePath))
        {
            project.WorkPermitImagePath = request.WorkPermitImagePath;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var updatedProject = await _unitOfWork.Projects.GetByIdAsync(project.Id, cancellationToken);
        if (updatedProject is null)
        {
            return Fail<ProjectDto>("تعذر تحميل المشروع بعد التحديث.", "Project could not be loaded after update.");
        }

        return Ok(MapToDto(updatedProject), "تم تحديث بيانات المشروع بنجاح.", "Project updated successfully.");
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
            return Fail<object?>("المشروع غير موجود.", "Project was not found.");
        }

        var companyError = EnsureSameProductionCompany<object?>(project, currentUser!);
        if (companyError is not null)
        {
            return companyError;
        }

        _unitOfWork.Projects.Remove(project);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok<object?>(null, "تم حذف المشروع بنجاح.", "Project deleted successfully.");
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
            return Fail<ProjectDto>("المشروع غير موجود.", "Project was not found.");
        }

        var companyError = EnsureSameProductionCompany<ProjectDto>(project, currentUser!);
        if (companyError is not null)
        {
            return companyError;
        }

        if (!project.IsDeleted)
        {
            return Fail<ProjectDto>("المشروع غير محذوف أصلًا.", "Project is not deleted.");
        }

        project.IsDeleted = false;
        project.DeletedAt = null;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var restoredProject = await _unitOfWork.Projects.GetByIdAsync(project.Id, cancellationToken);
        return Ok(MapToDto(restoredProject!), "تم استرجاع المشروع بنجاح.", "Project restored successfully.");
    }

    public async Task<ApiResponse<List<ProjectTypeDto>>> GetProjectTypesAsync(
        CancellationToken cancellationToken = default)
    {
        var projectTypes = await _unitOfWork.ProjectTypes.GetAllAsync(cancellationToken);

        var dtos = projectTypes
            .Select(pt => new ProjectTypeDto(pt.Id, pt.NameEn, pt.NameAr))
            .ToList();

        return Ok(dtos, "تم جلب أنواع المشاريع بنجاح.", "Project types retrieved successfully.");
    }

    #region Helpers

    private async Task<(User? User, ApiResponse<T>? Error)> AuthorizeUserAsync<T>(int currentUserId, CancellationToken cancellationToken)
    {
        var currentUser = await _unitOfWork.Users.GetWithPermissionsAsync(currentUserId, cancellationToken);
        if (currentUser is null)
        {
            return (null, Fail<T>("المستخدم الحالي غير موجود.", "Current user was not found.", 401));
        }

        if (!currentUser.IsActive)
        {
            return (null, Fail<T>("المستخدم الحالي غير فعّال.", "Current user is inactive.", 401));
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
            return (null, Fail<T>("لا تملك الصلاحية المطلوبة لإدارة المشاريع.", "You do not hold the required Projects permission.", 403));
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
            return (null, Fail<T>($"المستخدم المحدد لـ ({roleLabelAr}) غير موجود.", $"{roleLabelEn} not found."));
        }

        if (user.EntityType != EntityType.ProductionCompany || user.EntityId != currentUser.EntityId)
        {
            return (null, Fail<T>($"يجب أن ينتمي ({roleLabelAr}) إلى نفس شركة الإنتاج.", $"{roleLabelEn} must belong to the same Production Company."));
        }

        return (user, null);
    }

    private static ApiResponse<T>? EnsureSameProductionCompany<T>(Project project, User currentUser)
    {
        if (project.ProductionCompanyId != currentUser.EntityId)
        {
            return Fail<T>("لا يمكنك إدارة مشروع خارج شركة الإنتاج الخاصة بك.", "Cannot manage a project outside the current Production Company.", 403);
        }

        return null;
    }

    private static ApiResponse<T>? ValidateProjectPayload<T>(
        string projectNameEn,
        string projectNameAr,
        DateTime startDate,
        DateTime endDate,
        int producerUserId,
        int locationManagerUserId)
    {
        if (string.IsNullOrWhiteSpace(projectNameEn))
        {
            return Fail<T>("اسم المشروع باللغة الإنجليزية مطلوب.", "English project name is required.");
        }

        if (string.IsNullOrWhiteSpace(projectNameAr))
        {
            return Fail<T>("اسم المشروع باللغة العربية مطلوب.", "Arabic project name is required.");
        }

        if (projectNameEn.Trim().Length > 200)
        {
            return Fail<T>("اسم المشروع باللغة الإنجليزية يجب ألا يتجاوز 200 حرف.", "English project name cannot exceed 200 characters.");
        }

        if (projectNameAr.Trim().Length > 200)
        {
            return Fail<T>("اسم المشروع باللغة العربية يجب ألا يتجاوز 200 حرف.", "Arabic project name cannot exceed 200 characters.");
        }

        if (endDate <= startDate)
        {
            return Fail<T>("تاريخ الانتهاء يجب أن يكون بعد تاريخ البداية.", "End date must be greater than start date.");
        }

        if (producerUserId == locationManagerUserId)
        {
            return Fail<T>("يجب أن يكون المنتج ومدير الموقع شخصين مختلفين.", "Producer and Location Manager must be different users.");
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
            IsDeleted = project.IsDeleted
        };
    }

    private static ApiResponse<T> Fail<T>(string messageAr, string messageEn, int statusCode = 400) =>
        new(false, messageAr, messageEn, default, statusCode);

    private static ApiResponse<T> Ok<T>(T data, string messageAr, string messageEn, int statusCode = 200) =>
        new(true, messageAr, messageEn, data, statusCode);

    #endregion
}