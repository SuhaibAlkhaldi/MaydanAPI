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

    public async Task<ProjectDto> CreateAsync(
        CreateProjectDto request,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserWithPermissionsAsync(currentUserId, cancellationToken);
        EnsureHasAnyProjectPermission(currentUser, CreateProjectsPermissionId, ManageProjectsPermissionId);

        if (currentUser.EntityType != EntityType.ProductionCompany)
        {
            throw new InvalidOperationException("Only Production Company users can create projects.");
        }

        ValidateProjectPayload(
            request.ProjectNameEn,
            request.ProjectNameAr,
            request.StartDate,
            request.EndDate,
            request.ProducerUserId,
            request.LocationManagerUserId);

        if (!await _unitOfWork.ProjectTypes.ExistsAsync(request.ProjectTypeId, cancellationToken))
        {
            throw new KeyNotFoundException("Invalid Project Type.");
        }

        var producer = await GetSameCompanyUserAsync(request.ProducerUserId, currentUser, "Producer", cancellationToken);
        var locationManager = await GetSameCompanyUserAsync(request.LocationManagerUserId, currentUser, "Location Manager", cancellationToken);

        var project = new Project
        {
            ProjectNameEn = request.ProjectNameEn.Trim(),
            ProjectNameAr = request.ProjectNameAr.Trim(),

            StartDate = request.StartDate,
            EndDate = request.EndDate,

            ProjectTypeId = request.ProjectTypeId,

            ProducerUserId = producer.UserId,
            LocationManagerUserId = locationManager.UserId,

            WorkPermitImagePath = request.WorkPermitImagePath,

            ProductionCompanyId = currentUser.EntityId,

            CreatedBy = currentUser.UserId,
            IsActive = true
        };

        await _unitOfWork.Projects.AddAsync(project, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var createdProject = await _unitOfWork.Projects.GetByIdAsync(project.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Project could not be loaded after creation.");

        return MapToDto(createdProject);
    }

    public async Task<List<ProjectDto>> GetAllAsync(

        int currentUserId,
        ProjectQueryDto query,
        CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserWithPermissionsAsync(currentUserId, cancellationToken);


        var currentUser = await GetCurrentUserAsync(currentUserId, cancellationToken);

        if (currentUser.EntityType != EntityType.ProductionCompany)
        {
            throw new InvalidOperationException("Only Production Company users can create projects.");
        }

        ValidateProjectPayload(
            request.ProjectNameEn,
            request.ProjectNameAr,
            request.StartDate,
            request.EndDate,
            request.ProducerUserId,
            request.LocationManagerUserId);

        if (!await _unitOfWork.ProjectTypes.ExistsAsync(request.ProjectTypeId, cancellationToken))
        {
            throw new KeyNotFoundException("Invalid Project Type.");
        }

        var producer = await GetSameCompanyUserAsync(request.ProducerUserId, currentUser, "Producer", cancellationToken);
        var locationManager = await GetSameCompanyUserAsync(request.LocationManagerUserId, currentUser, "Location Manager", cancellationToken);

        var project = new Project
        {
            ProjectNameEn = request.ProjectNameEn.Trim(),
            ProjectNameAr = request.ProjectNameAr.Trim(),

            StartDate = request.StartDate,
            EndDate = request.EndDate,

            ProjectTypeId = request.ProjectTypeId,

            ProducerUserId = producer.UserId,
            LocationManagerUserId = locationManager.UserId,

            WorkPermitImagePath = request.WorkPermitImagePath,

            ProductionCompanyId = currentUser.EntityId,

            CreatedBy = currentUser.UserId,
            IsActive = true
        };

        await _unitOfWork.Projects.AddAsync(project, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var createdProject = await _unitOfWork.Projects.GetByIdAsync(project.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Project could not be loaded after creation.");

        return MapToDto(createdProject);
    }

    public async Task<List<ProjectDto>> GetAllAsync(
        ProjectQueryDto query,
        CancellationToken cancellationToken = default)
    {
        var projects = await _unitOfWork.Projects.GetAllAsync(
            query.IsDeleted,
            query.SearchTerm,
            query.StartDate,
            query.EndDate,
            query.SearchByProductionCompanyName,
            query.SearchByProductionCompanyId,
            cancellationToken);

        return projects
            .Where(project => CanViewProject(currentUser, project))
            .Select(MapToDto)
            .ToList();
    }

    public async Task<ProjectDto> GetByIdAsync(
        int currentUserId,
        int id,
        CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserWithPermissionsAsync(currentUserId, cancellationToken);

        var project = await _unitOfWork.Projects.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Project was not found.");

        if (!CanViewProject(currentUser, project))
        {
            throw new UnauthorizedAccessException("Cannot view a project outside the caller's authorized scope.");
        }


        int id,
        CancellationToken cancellationToken = default)
    {
        var project = await _unitOfWork.Projects.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Project was not found.");

        return MapToDto(project);
    }

    public async Task<ProjectDto> UpdateAsync(
        int id,
        UpdateProjectDto request,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserWithPermissionsAsync(currentUserId, cancellationToken);
        EnsureHasAnyProjectPermission(currentUser, EditProjectsPermissionId, ManageProjectsPermissionId);
        var currentUser = await GetCurrentUserAsync(currentUserId, cancellationToken);

        var project = await _unitOfWork.Projects.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Project was not found.");

        EnsureSameProductionCompany(project, currentUser);

        ValidateProjectPayload(
            request.ProjectNameEn,
            request.ProjectNameAr,
            request.StartDate,
            request.EndDate,
            request.ProducerUserId,
            request.LocationManagerUserId);

        if (!await _unitOfWork.ProjectTypes.ExistsAsync(request.ProjectTypeId, cancellationToken))
        {
            throw new KeyNotFoundException("Invalid Project Type.");
        }

        var producer = await GetSameCompanyUserAsync(request.ProducerUserId, currentUser, "Producer", cancellationToken);
        var locationManager = await GetSameCompanyUserAsync(request.LocationManagerUserId, currentUser, "Location Manager", cancellationToken);

        project.ProjectNameEn = request.ProjectNameEn.Trim();
        project.ProjectNameAr = request.ProjectNameAr.Trim();
        project.StartDate = request.StartDate;
        project.EndDate = request.EndDate;
        project.ProjectTypeId = request.ProjectTypeId;
        project.ProducerUserId = producer.UserId;
        project.LocationManagerUserId = locationManager.UserId;

        if (!string.IsNullOrWhiteSpace(request.WorkPermitImagePath))
        {
            project.WorkPermitImagePath = request.WorkPermitImagePath;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var updatedProject = await _unitOfWork.Projects.GetByIdAsync(project.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Project could not be loaded after update.");

        return MapToDto(updatedProject);
    }

    public async Task DeleteAsync(
        int id,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserWithPermissionsAsync(currentUserId, cancellationToken);
        EnsureHasAnyProjectPermission(currentUser, DeleteProjectsPermissionId, ManageProjectsPermissionId);

        var project = await _unitOfWork.Projects.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Project was not found.");

        EnsureSameProductionCompany(project, currentUser);


        _unitOfWork.Projects.Remove(project);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<ProjectDto> RestoreAsync(
        int id,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserWithPermissionsAsync(currentUserId, cancellationToken);
        EnsureHasAnyProjectPermission(currentUser, DeleteProjectsPermissionId, ManageProjectsPermissionId);


        var project = await _unitOfWork.Projects.GetByIdIncludingDeletedAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Project was not found.");

        EnsureSameProductionCompany(project, currentUser);

        if (!project.IsDeleted)
        {
            throw new InvalidOperationException("Project is not deleted.");
        }

        project.IsDeleted = false;
        project.DeletedAt = null;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(project);
    }

    public async Task<List<ProjectTypeDto>> GetProjectTypesAsync(
        CancellationToken cancellationToken = default)
    {
        var projectTypes = await _unitOfWork.ProjectTypes.GetAllAsync(cancellationToken);

        return projectTypes
            .Select(projectType => new ProjectTypeDto(projectType.Id, projectType.NameEn, projectType.NameAr))
            .ToList();
    }

    private async Task<User> GetCurrentUserAsync(int currentUserId, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(currentUserId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Current user was not found.");

        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("Current user is inactive.");
        }

        return user;
    }

    private async Task<User> GetCurrentUserWithPermissionsAsync(int currentUserId, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetWithPermissionsAsync(currentUserId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Current user was not found.");

        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("Current user is inactive.");
        }

        return user;
    }

    private static void EnsureHasAnyProjectPermission(User user, params int[] permissionIds)
    {
        if (!GetEffectivePermissionIds(user).Overlaps(permissionIds))
        {
            throw new UnauthorizedAccessException("Caller does not hold the required Projects permission.");
        }
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

    private static void ValidateProjectPayload(
        string projectNameEn,
        string projectNameAr,
        DateTime startDate,
        DateTime endDate,
        int producerUserId,
        int locationManagerUserId)
    {
        if (string.IsNullOrWhiteSpace(projectNameEn))
        {
            throw new InvalidOperationException("English project name is required.");
        }

        if (string.IsNullOrWhiteSpace(projectNameAr))
        {
            throw new InvalidOperationException("Arabic project name is required.");
        }

        if (projectNameEn.Trim().Length > 200)
        {
            throw new InvalidOperationException("English project name cannot exceed 200 characters.");
        }

        if (projectNameAr.Trim().Length > 200)
        {
            throw new InvalidOperationException("Arabic project name cannot exceed 200 characters.");
        }

        if (endDate <= startDate)
        {
            throw new InvalidOperationException("End date must be greater than start date.");
        }

        if (producerUserId == locationManagerUserId)
        {
            throw new InvalidOperationException("Producer and Location Manager must be different users.");
        }
    }

    private async Task<User> GetSameCompanyUserAsync(
        int userId,
        User currentUser,
        string roleLabel,
        CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken)
            ?? throw new KeyNotFoundException($"{roleLabel} not found.");

        if (user.EntityType != EntityType.ProductionCompany || user.EntityId != currentUser.EntityId)
        {
            throw new InvalidOperationException($"{roleLabel} must belong to the same Production Company.");


        var updatedProject = await _unitOfWork.Projects.GetByIdAsync(project.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Project could not be loaded after update.");

        return MapToDto(updatedProject);
    }

    public async Task DeleteAsync(
        int id,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(currentUserId, cancellationToken);

        var project = await _unitOfWork.Projects.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Project was not found.");

        EnsureSameProductionCompany(project, currentUser);

        _unitOfWork.Projects.Remove(project);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<ProjectDto> RestoreAsync(
        int id,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(currentUserId, cancellationToken);

        var project = await _unitOfWork.Projects.GetByIdIncludingDeletedAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Project was not found.");

        EnsureSameProductionCompany(project, currentUser);

        if (!project.IsDeleted)
        {
            throw new InvalidOperationException("Project is not deleted.");
        }

        project.IsDeleted = false;
        project.DeletedAt = null;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(project);
    }

    public async Task<List<ProjectTypeDto>> GetProjectTypesAsync(
        CancellationToken cancellationToken = default)
    {
        var projectTypes = await _unitOfWork.ProjectTypes.GetAllAsync(cancellationToken);

        return projectTypes
            .Select(projectType => new ProjectTypeDto(projectType.Id, projectType.NameEn, projectType.NameAr))
            .ToList();
    }

    private async Task<User> GetCurrentUserAsync(int currentUserId, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(currentUserId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Current user was not found.");

        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("Current user is inactive.");
        }

        return user;
    }

    private static void ValidateProjectPayload(
        string projectNameEn,
        string projectNameAr,
        DateTime startDate,
        DateTime endDate,
        int producerUserId,
        int locationManagerUserId)
    {
        if (string.IsNullOrWhiteSpace(projectNameEn))
        {
            throw new InvalidOperationException("English project name is required.");
        }

        if (string.IsNullOrWhiteSpace(projectNameAr))
        {
            throw new InvalidOperationException("Arabic project name is required.");
        }

        if (projectNameEn.Trim().Length > 200)
        {
            throw new InvalidOperationException("English project name cannot exceed 200 characters.");
        }

        if (projectNameAr.Trim().Length > 200)
        {
            throw new InvalidOperationException("Arabic project name cannot exceed 200 characters.");
        }

        if (endDate <= startDate)
        {
            throw new InvalidOperationException("End date must be greater than start date.");
        }

        if (producerUserId == locationManagerUserId)
        {
            throw new InvalidOperationException("Producer and Location Manager must be different users.");
        }
    }



    private async Task<User> GetSameCompanyUserAsync(
        int userId,
        User currentUser,
        string roleLabel,
        CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken)
            ?? throw new KeyNotFoundException($"{roleLabel} not found.");

        if (user.EntityType != EntityType.ProductionCompany || user.EntityId != currentUser.EntityId)
        {
            throw new InvalidOperationException($"{roleLabel} must belong to the same Production Company.");
        }

        return user;
    }


    private static void EnsureSameProductionCompany(Project project, User currentUser)
    {
        if (project.ProductionCompanyId != currentUser.EntityId)
        {
            throw new UnauthorizedAccessException("Cannot manage a project outside the current Production Company.");
        }
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
            ProducerNameEn =
                $"{project.Producer.FirstNameEn} {project.Producer.LastNameEn}",
            ProducerNameAr =
                $"{project.Producer.FirstNameAr} {project.Producer.LastNameAr}",

            LocationManagerUserId = project.LocationManagerUserId,
            LocationManagerNameEn =
                $"{project.LocationManager.FirstNameEn} {project.LocationManager.LastNameEn}",
            LocationManagerNameAr =
                $"{project.LocationManager.FirstNameAr} {project.LocationManager.LastNameAr}",

            WorkPermitImagePath = project.WorkPermitImagePath,

            ProductionCompanyId = project.ProductionCompanyId,

            IsDeleted = project.IsDeleted
        };
    }
}
