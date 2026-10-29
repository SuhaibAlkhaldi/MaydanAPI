using Maydan.Application.DTOs.Projects;
using Maydan.Application.Interfaces;
using Maydan.Application.Services;
using Maydan.Domain.Entities;
using Maydan.Domain.Enums;

namespace Maydan.Application.Tests.Services;

// ProjectService returns ApiResponse<T> for expected validation and authorization failures.
// These tests cover CRUD and LocationManager overlap using lightweight in-memory repositories.
// SQL Server translation, query filters, and concurrency require separate integration tests.
public class ProjectServiceTests
{
    private static User NewUser(int userId, EntityType entityType, int entityId, bool isActive = true) => new()
    {
        UserId = userId,
        FirstNameEn = $"First{userId}",
        LastNameEn = $"Last{userId}",
        FirstNameAr = $"اول{userId}",
        LastNameAr = $"اخير{userId}",
        RoleId = 1,
        EntityType = entityType,
        EntityId = entityId,
        IsActive = isActive
    };

    private static ProjectType NewProjectType(int id) => new()
    {
        Id = id,
        NameEn = "Feature Film",
        NameAr = "فيلم طويل",
        IsActive = true
    };

    private static Role NewEmptyRole(int roleId) => new()
    {
        RoleId = roleId,
        RoleNameEn = $"Role {roleId}",
        RoleNameAr = $"Role {roleId}",
        IsActive = true
    };

    private static CreateProjectDto ValidCreateDto(int producerUserId, int locationManagerUserId, int projectTypeId) => new()
    {
        ProjectNameEn = "New Documentary",
        ProjectNameAr = "وثائقي جديد",
        StartDate = new DateTime(2026, 1, 1),
        EndDate = new DateTime(2026, 6, 1),
        ProjectTypeId = projectTypeId,
        ProducerUserId = producerUserId,
        LocationManagerUserId = locationManagerUserId,
        WorkPermitImagePath = "/uploads/work-permits/abc123.pdf"
    };

    private static UpdateProjectDto ValidUpdateDto(int producerUserId, int locationManagerUserId, int projectTypeId) => new()
    {
        ProjectNameEn = "Updated Documentary",
        ProjectNameAr = "وثائقي محدث",
        StartDate = new DateTime(2026, 2, 1),
        EndDate = new DateTime(2026, 7, 1),
        ProjectTypeId = projectTypeId,
        ProducerUserId = producerUserId,
        LocationManagerUserId = locationManagerUserId,
        WorkPermitImagePath = null
    };

    // ---------------------------------------------------------------------
    // CreateAsync — ApiResponse behavior and LocationManager overlap
    // ---------------------------------------------------------------------

    [Fact]
    public async Task CreateAsync_OverlappingProjectForSameManager_Fails()
    {
        var currentUser = NewUser(1, EntityType.ProductionCompany, 5);
        var producer = NewUser(2, EntityType.ProductionCompany, 5);
        var locationManager = NewUser(3, EntityType.ProductionCompany, 5);
        var projectType = NewProjectType(7);

        var existingProject = NewProject(
            20,
            producer.UserId,
            locationManager.UserId,
            projectType.Id,
            productionCompanyId: 5);

        existingProject.StartDate = new DateTime(2026, 4, 1);
        existingProject.EndDate = new DateTime(2026, 8, 31);

        var (service, projectRepository) = BuildService(
            users: [currentUser, producer, locationManager],
            projectTypes: [projectType],
            projects: [existingProject]);

        var result = await service.CreateAsync(
            ValidCreateDto(producer.UserId, locationManager.UserId, projectType.Id),
            currentUser.UserId);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Null(projectRepository.AddedProject);
    }

    [Fact]
    public async Task CreateAsync_NonOverlappingProjectForSameManager_Succeeds()
    {
        var currentUser = NewUser(1, EntityType.ProductionCompany, 5);
        var producer = NewUser(2, EntityType.ProductionCompany, 5);
        var locationManager = NewUser(3, EntityType.ProductionCompany, 5);
        var projectType = NewProjectType(7);

        var existingProject = NewProject(
            20,
            producer.UserId,
            locationManager.UserId,
            projectType.Id,
            productionCompanyId: 5);

        existingProject.StartDate = new DateTime(2025, 1, 1);
        existingProject.EndDate = new DateTime(2025, 5, 31);

        var (service, _) = BuildService(
            users: [currentUser, producer, locationManager],
            projectTypes: [projectType],
            projects: [existingProject]);

        var result = await service.CreateAsync(
            ValidCreateDto(producer.UserId, locationManager.UserId, projectType.Id),
            currentUser.UserId);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task CreateAsync_SameBoundaryCalendarDate_Fails()
    {
        var currentUser = NewUser(1, EntityType.ProductionCompany, 5);
        var producer = NewUser(2, EntityType.ProductionCompany, 5);
        var locationManager = NewUser(3, EntityType.ProductionCompany, 5);
        var projectType = NewProjectType(7);

        var existingProject = NewProject(
            20,
            producer.UserId,
            locationManager.UserId,
            projectType.Id,
            productionCompanyId: 5);

        existingProject.StartDate = new DateTime(2026, 1, 1);
        existingProject.EndDate = new DateTime(2026, 5, 31, 0, 0, 0);

        var dto = ValidCreateDto(
            producer.UserId,
            locationManager.UserId,
            projectType.Id);

        dto.StartDate = new DateTime(2026, 5, 31, 15, 0, 0);
        dto.EndDate = new DateTime(2026, 8, 31);

        var (service, _) = BuildService(
            users: [currentUser, producer, locationManager],
            projectTypes: [projectType],
            projects: [existingProject]);

        var result = await service.CreateAsync(dto, currentUser.UserId);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task CreateAsync_StartsNextCalendarDayAfterExistingEnd_Succeeds()
    {
        var currentUser = NewUser(1, EntityType.ProductionCompany, 5);
        var producer = NewUser(2, EntityType.ProductionCompany, 5);
        var locationManager = NewUser(3, EntityType.ProductionCompany, 5);
        var projectType = NewProjectType(7);

        var existingProject = NewProject(
            20,
            producer.UserId,
            locationManager.UserId,
            projectType.Id,
            productionCompanyId: 5);

        existingProject.StartDate = new DateTime(2026, 1, 1);
        existingProject.EndDate = new DateTime(2026, 5, 31, 23, 59, 59);

        var dto = ValidCreateDto(
            producer.UserId,
            locationManager.UserId,
            projectType.Id);

        dto.StartDate = new DateTime(2026, 6, 1);
        dto.EndDate = new DateTime(2026, 8, 31);

        var (service, _) = BuildService(
            users: [currentUser, producer, locationManager],
            projectTypes: [projectType],
            projects: [existingProject]);

        var result = await service.CreateAsync(dto, currentUser.UserId);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task CreateAsync_DifferentLocationManagerWithOverlappingDates_Succeeds()
    {
        var currentUser = NewUser(1, EntityType.ProductionCompany, 5);
        var producer = NewUser(2, EntityType.ProductionCompany, 5);
        var existingManager = NewUser(3, EntityType.ProductionCompany, 5);
        var newManager = NewUser(4, EntityType.ProductionCompany, 5);
        var projectType = NewProjectType(7);

        var existingProject = NewProject(
            20,
            producer.UserId,
            existingManager.UserId,
            projectType.Id,
            productionCompanyId: 5);

        var (service, _) = BuildService(
            users: [currentUser, producer, existingManager, newManager],
            projectTypes: [projectType],
            projects: [existingProject]);

        var result = await service.CreateAsync(
            ValidCreateDto(producer.UserId, newManager.UserId, projectType.Id),
            currentUser.UserId);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task CreateAsync_InactiveProjectStillConflicts()
    {
        var currentUser = NewUser(1, EntityType.ProductionCompany, 5);
        var producer = NewUser(2, EntityType.ProductionCompany, 5);
        var locationManager = NewUser(3, EntityType.ProductionCompany, 5);
        var projectType = NewProjectType(7);

        var existingProject = NewProject(
            20,
            producer.UserId,
            locationManager.UserId,
            projectType.Id,
            productionCompanyId: 5);

        existingProject.IsActive = false;

        var (service, _) = BuildService(
            users: [currentUser, producer, locationManager],
            projectTypes: [projectType],
            projects: [existingProject]);

        var result = await service.CreateAsync(
            ValidCreateDto(producer.UserId, locationManager.UserId, projectType.Id),
            currentUser.UserId);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task CreateAsync_SoftDeletedProjectDoesNotCauseOverlap_Succeeds()
    {
        var currentUser = NewUser(1, EntityType.ProductionCompany, 5);
        var producer = NewUser(2, EntityType.ProductionCompany, 5);
        var manager = NewUser(3, EntityType.ProductionCompany, 5);
        var projectType = NewProjectType(7);
        var deletedProject = NewProject(20, producer.UserId, manager.UserId, projectType.Id, 5);
        deletedProject.IsDeleted = true;

        var (service, repository) = BuildService(
            users: [currentUser, producer, manager],
            projectTypes: [projectType],
            projects: [deletedProject]);

        var result = await service.CreateAsync(
            ValidCreateDto(producer.UserId, manager.UserId, projectType.Id),
            currentUser.UserId);

        Assert.True(result.Success);
        Assert.NotNull(repository.AddedProject);
    }

    [Fact]
    public async Task CreateAsync_SameCalendarDateWithDifferentTimes_Fails()
    {
        var currentUser = NewUser(1, EntityType.ProductionCompany, 5);
        var producer = NewUser(2, EntityType.ProductionCompany, 5);
        var locationManager = NewUser(3, EntityType.ProductionCompany, 5);
        var projectType = NewProjectType(7);

        var existingProject = NewProject(
            20,
            producer.UserId,
            locationManager.UserId,
            projectType.Id,
            productionCompanyId: 5);

        existingProject.StartDate = new DateTime(2026, 1, 1);
        existingProject.EndDate = new DateTime(2026, 6, 1, 0, 0, 0);

        var dto = ValidCreateDto(
            producer.UserId,
            locationManager.UserId,
            projectType.Id);

        dto.StartDate = new DateTime(2026, 6, 1, 15, 0, 0);
        dto.EndDate = new DateTime(2026, 8, 31);

        var (service, _) = BuildService(
            users: [currentUser, producer, locationManager],
            projectTypes: [projectType],
            projects: [existingProject]);

        var result = await service.CreateAsync(dto, currentUser.UserId);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task UpdateAsync_ChangingManagerToConflictingManager_Fails()
    {
        var currentUser = NewUser(1, EntityType.ProductionCompany, 5);
        var producer = NewUser(2, EntityType.ProductionCompany, 5);
        var managerToAssign = NewUser(3, EntityType.ProductionCompany, 5);
        var currentManager = NewUser(4, EntityType.ProductionCompany, 5);
        var projectType = NewProjectType(7);

        var projectBeingUpdated = NewProject(
            10,
            producer.UserId,
            currentManager.UserId,
            projectType.Id,
            productionCompanyId: 5);

        var conflictingProject = NewProject(
            20,
            producer.UserId,
            managerToAssign.UserId,
            projectType.Id,
            productionCompanyId: 5);

        projectBeingUpdated.StartDate = new DateTime(2026, 1, 1);
        projectBeingUpdated.EndDate = new DateTime(2026, 2, 28);
        conflictingProject.StartDate = new DateTime(2026, 3, 1);
        conflictingProject.EndDate = new DateTime(2026, 4, 1);

        var (service, _) = BuildService(
            users: [currentUser, producer, managerToAssign, currentManager],
            projectTypes: [projectType],
            projects: [projectBeingUpdated, conflictingProject]);

        var dto = ValidUpdateDto(producer.UserId, managerToAssign.UserId, projectType.Id);
        dto.StartDate = new DateTime(2026, 2, 1);
        dto.EndDate = new DateTime(2026, 5, 31);

        var result = await service.UpdateAsync(
            projectBeingUpdated.Id,
            dto,
            currentUser.UserId);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal(currentManager.UserId, projectBeingUpdated.LocationManagerUserId);
        Assert.Equal(new DateTime(2026, 1, 1), projectBeingUpdated.StartDate);
    }

    [Fact]
    public async Task UpdateAsync_ChangingProjectDatesToConflict_Fails()
    {
        var currentUser = NewUser(1, EntityType.ProductionCompany, 5);
        var producer = NewUser(2, EntityType.ProductionCompany, 5);
        var locationManager = NewUser(3, EntityType.ProductionCompany, 5);
        var projectType = NewProjectType(7);

        var projectBeingUpdated = NewProject(
            10,
            producer.UserId,
            locationManager.UserId,
            projectType.Id,
            productionCompanyId: 5);

        var conflictingProject = NewProject(
            20,
            producer.UserId,
            locationManager.UserId,
            projectType.Id,
            productionCompanyId: 5);

        projectBeingUpdated.StartDate = new DateTime(2026, 1, 1);
        projectBeingUpdated.EndDate = new DateTime(2026, 2, 28);
        conflictingProject.StartDate = new DateTime(2026, 3, 1);
        conflictingProject.EndDate = new DateTime(2026, 4, 1);

        var (service, _) = BuildService(
            users: [currentUser, producer, locationManager],
            projectTypes: [projectType],
            projects: [projectBeingUpdated, conflictingProject]);

        var dto = ValidUpdateDto(producer.UserId, locationManager.UserId, projectType.Id);
        dto.StartDate = new DateTime(2026, 2, 15);
        dto.EndDate = new DateTime(2026, 4, 15);

        var result = await service.UpdateAsync(
            projectBeingUpdated.Id,
            dto,
            currentUser.UserId);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal(new DateTime(2026, 1, 1), projectBeingUpdated.StartDate);
        Assert.Equal(new DateTime(2026, 2, 28), projectBeingUpdated.EndDate);
    }

    [Fact]
    public async Task UpdateAsync_ExcludesCurrentProjectFromOverlapCheck_Succeeds()
    {
        var currentUser = NewUser(1, EntityType.ProductionCompany, 5);
        var producer = NewUser(2, EntityType.ProductionCompany, 5);
        var locationManager = NewUser(3, EntityType.ProductionCompany, 5);
        var projectType = NewProjectType(7);

        var projectBeingUpdated = NewProject(
            10,
            producer.UserId,
            locationManager.UserId,
            projectType.Id,
            productionCompanyId: 5);

        var (service, _) = BuildService(
            users: [currentUser, producer, locationManager],
            projectTypes: [projectType],
            projects: [projectBeingUpdated]);

        var result = await service.UpdateAsync(
            projectBeingUpdated.Id,
            ValidUpdateDto(producer.UserId, locationManager.UserId, projectType.Id),
            currentUser.UserId);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task CreateAsync_ValidRequestFromProductionCompanyUser_Succeeds()
    {
        var currentUser = NewUser(1, EntityType.ProductionCompany, 5);
        var producer = NewUser(2, EntityType.ProductionCompany, 5);
        var locationManager = NewUser(3, EntityType.ProductionCompany, 5);
        var projectType = NewProjectType(7);

        var (service, projectRepository) = BuildService(
            users: [currentUser, producer, locationManager],
            projectTypes: [projectType]);

        var result = await service.CreateAsync(ValidCreateDto(producer.UserId, locationManager.UserId, projectType.Id), currentUser.UserId);

        Assert.Equal("New Documentary", result.Data!.ProjectNameEn);
        Assert.Equal(5, result.Data!.ProductionCompanyId);
        Assert.Equal(producer.UserId, result.Data!.ProducerUserId);
        Assert.Equal($"{producer.FirstNameEn} {producer.LastNameEn}", result.Data!.ProducerNameEn);
        Assert.False(result.Data!.IsDeleted);
        Assert.NotNull(projectRepository.AddedProject);
        Assert.Equal(currentUser.UserId, projectRepository.AddedProject!.CreatedBy);
    }

    [Fact]
    public async Task CreateAsync_CurrentUserNotFound_ReturnsFailure()
    {
        var (service, _) = BuildService(users: [], projectTypes: [NewProjectType(1)]);

        var result = await service.CreateAsync(ValidCreateDto(2, 3, 1), currentUserId: 999);

        Assert.False(result.Success);
        Assert.Equal(401, result.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_CurrentUserNotProductionCompany_ReturnsFailure()
    {
        var currentUser = NewUser(1, EntityType.Association, 5);
        var (service, _) = BuildService(users: [currentUser], projectTypes: [NewProjectType(1)]);

        var result = await service.CreateAsync(ValidCreateDto(2, 3, 1), currentUser.UserId);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_MissingEnglishName_ReturnsFailure()
    {
        var currentUser = NewUser(1, EntityType.ProductionCompany, 5);
        var producer = NewUser(2, EntityType.ProductionCompany, 5);
        var locationManager = NewUser(3, EntityType.ProductionCompany, 5);
        var projectType = NewProjectType(7);
        var (service, _) = BuildService(users: [currentUser, producer, locationManager], projectTypes: [projectType]);

        var dto = ValidCreateDto(producer.UserId, locationManager.UserId, projectType.Id);
        dto.ProjectNameEn = "   ";

        var result = await service.CreateAsync(dto, currentUser.UserId);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_EndDateNotAfterStartDate_ReturnsFailure()
    {
        var currentUser = NewUser(1, EntityType.ProductionCompany, 5);
        var producer = NewUser(2, EntityType.ProductionCompany, 5);
        var locationManager = NewUser(3, EntityType.ProductionCompany, 5);
        var projectType = NewProjectType(7);
        var (service, _) = BuildService(users: [currentUser, producer, locationManager], projectTypes: [projectType]);

        var dto = ValidCreateDto(producer.UserId, locationManager.UserId, projectType.Id);
        dto.EndDate = dto.StartDate;

        var result = await service.CreateAsync(dto, currentUser.UserId);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_ProducerSameAsLocationManager_ReturnsFailure()
    {
        var currentUser = NewUser(1, EntityType.ProductionCompany, 5);
        var producer = NewUser(2, EntityType.ProductionCompany, 5);
        var projectType = NewProjectType(7);
        var (service, _) = BuildService(users: [currentUser, producer], projectTypes: [projectType]);

        var result = await service.CreateAsync(ValidCreateDto(producer.UserId, producer.UserId, projectType.Id), currentUser.UserId);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_InvalidProjectType_ReturnsFailure()
    {
        var currentUser = NewUser(1, EntityType.ProductionCompany, 5);
        var producer = NewUser(2, EntityType.ProductionCompany, 5);
        var locationManager = NewUser(3, EntityType.ProductionCompany, 5);
        var (service, _) = BuildService(users: [currentUser, producer, locationManager], projectTypes: []);

        var result = await service.CreateAsync(ValidCreateDto(producer.UserId, locationManager.UserId, 999), currentUser.UserId);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_ProducerNotFound_ReturnsFailure()
    {
        var currentUser = NewUser(1, EntityType.ProductionCompany, 5);
        var locationManager = NewUser(3, EntityType.ProductionCompany, 5);
        var projectType = NewProjectType(7);
        var (service, _) = BuildService(users: [currentUser, locationManager], projectTypes: [projectType]);

        var result = await service.CreateAsync(ValidCreateDto(producerUserId: 404, locationManager.UserId, projectType.Id), currentUser.UserId);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_ProducerDifferentCompany_ReturnsFailure()
    {
        var currentUser = NewUser(1, EntityType.ProductionCompany, 5);
        var outsideProducer = NewUser(2, EntityType.ProductionCompany, 99);
        var locationManager = NewUser(3, EntityType.ProductionCompany, 5);
        var projectType = NewProjectType(7);
        var (service, _) = BuildService(users: [currentUser, outsideProducer, locationManager], projectTypes: [projectType]);

        var result = await service.CreateAsync(ValidCreateDto(outsideProducer.UserId, locationManager.UserId, projectType.Id), currentUser.UserId);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
    }

    // ---------------------------------------------------------------------
    // GetAllAsync / GetByIdAsync
    // ---------------------------------------------------------------------

    [Fact]
    public async Task GetAllAsync_ReturnsMappedActiveProjects()
    {
        var producer = NewUser(2, EntityType.ProductionCompany, 5);
        var locationManager = NewUser(3, EntityType.ProductionCompany, 5);
        var projectType = NewProjectType(7);
        var project = NewProject(id: 10, producer.UserId, locationManager.UserId, projectType.Id, productionCompanyId: 5);

        var (service, _) = BuildService(
            users: [producer, locationManager],
            projectTypes: [projectType],
            projects: [project]);

        var results = await service.GetAllAsync(producer.UserId, new ProjectQueryDto { IsDeleted = false });

        Assert.Single(results.Data!);
        Assert.Equal(10, results.Data![0].Id);
    }

    [Fact]
    public async Task GetAllAsync_ProductionCompanyUserWithoutAllProjectsPermission_SeesOnlyAssignedProjects()
    {
        var currentUser = NewUser(1, EntityType.ProductionCompany, 5);
        currentUser.Role = NewEmptyRole(currentUser.RoleId);

        var otherProducer = NewUser(2, EntityType.ProductionCompany, 5);
        var locationManager = NewUser(3, EntityType.ProductionCompany, 5);
        var projectType = NewProjectType(7);
        var assignedProject = NewProject(id: 10, otherProducer.UserId, currentUser.UserId, projectType.Id, productionCompanyId: 5);
        var hiddenProject = NewProject(id: 11, otherProducer.UserId, locationManager.UserId, projectType.Id, productionCompanyId: 5);

        var (service, _) = BuildService(
            users: [currentUser, otherProducer, locationManager],
            projectTypes: [projectType],
            projects: [assignedProject, hiddenProject]);

        var results = await service.GetAllAsync(currentUser.UserId, new ProjectQueryDto { IsDeleted = false });

        var result = Assert.Single(results.Data!);
        Assert.Equal(assignedProject.Id, result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_UnassignedProductionCompanyUserWithoutAllProjectsPermission_ReturnsFailure()
    {
        var currentUser = NewUser(1, EntityType.ProductionCompany, 5);
        currentUser.Role = NewEmptyRole(currentUser.RoleId);

        var producer = NewUser(2, EntityType.ProductionCompany, 5);
        var locationManager = NewUser(3, EntityType.ProductionCompany, 5);
        var projectType = NewProjectType(7);
        var project = NewProject(id: 10, producer.UserId, locationManager.UserId, projectType.Id, productionCompanyId: 5);

        var (service, _) = BuildService(
            users: [currentUser, producer, locationManager],
            projectTypes: [projectType],
            projects: [project]);

        var result = await service.GetByIdAsync(currentUser.UserId, project.Id);

        Assert.False(result.Success);
        Assert.Equal(403, result.StatusCode);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingProject_ReturnsMappedProject()
    {
        var producer = NewUser(2, EntityType.ProductionCompany, 5);
        var locationManager = NewUser(3, EntityType.ProductionCompany, 5);
        var projectType = NewProjectType(7);
        var project = NewProject(id: 10, producer.UserId, locationManager.UserId, projectType.Id, productionCompanyId: 5);

        var (service, _) = BuildService(
            users: [producer, locationManager],
            projectTypes: [projectType],
            projects: [project]);

        var result = await service.GetByIdAsync(producer.UserId, 10);

        Assert.Equal(10, result.Data!.Id);
        Assert.Equal(projectType.NameEn, result.Data!.ProjectTypeNameEn);
    }

    [Fact]
    public async Task GetByIdAsync_MissingProject_ReturnsFailure()
    {
        var currentUser = NewUser(1, EntityType.ProductionCompany, 5);
        var (service, _) = BuildService(users: [currentUser], projectTypes: []);

        var result = await service.GetByIdAsync(currentUser.UserId, 404);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task GetByIdAsync_SoftDeletedProject_ReturnsFailure()
    {
        var producer = NewUser(2, EntityType.ProductionCompany, 5);
        var locationManager = NewUser(3, EntityType.ProductionCompany, 5);
        var projectType = NewProjectType(7);
        var project = NewProject(id: 10, producer.UserId, locationManager.UserId, projectType.Id, productionCompanyId: 5);
        project.IsDeleted = true;

        var (service, _) = BuildService(
            users: [producer, locationManager],
            projectTypes: [projectType],
            projects: [project]);

        // Mirrors MaydanDbContext's global soft-delete query filter — a deleted project is
        // invisible to the normal get-by-id path. The current service returns FailureResponse.
        var result = await service.GetByIdAsync(producer.UserId, 10);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
    }

    // ---------------------------------------------------------------------
    // UpdateAsync
    // ---------------------------------------------------------------------

    [Fact]
    public async Task UpdateAsync_ValidRequest_UpdatesAndReturnsProject()
    {
        var currentUser = NewUser(1, EntityType.ProductionCompany, 5);
        var producer = NewUser(2, EntityType.ProductionCompany, 5);
        var locationManager = NewUser(3, EntityType.ProductionCompany, 5);
        var newProducer = NewUser(4, EntityType.ProductionCompany, 5);
        var projectType = NewProjectType(7);
        var project = NewProject(id: 10, producer.UserId, locationManager.UserId, projectType.Id, productionCompanyId: 5);
        project.WorkPermitImagePath = "/uploads/work-permits/original.pdf";

        var (service, _) = BuildService(
            users: [currentUser, producer, locationManager, newProducer],
            projectTypes: [projectType],
            projects: [project]);

        var result = await service.UpdateAsync(10, ValidUpdateDto(newProducer.UserId, locationManager.UserId, projectType.Id), currentUser.UserId);

        Assert.Equal("Updated Documentary", result.Data!.ProjectNameEn);
        Assert.Equal(newProducer.UserId, result.Data!.ProducerUserId);
        // WorkPermitImagePath was null on the update request — the existing path must survive.
        Assert.Equal("/uploads/work-permits/original.pdf", result.Data!.WorkPermitImagePath);
    }

    [Fact]
    public async Task UpdateAsync_NewWorkPermitImageProvided_ReplacesExistingPath()
    {
        var currentUser = NewUser(1, EntityType.ProductionCompany, 5);
        var producer = NewUser(2, EntityType.ProductionCompany, 5);
        var locationManager = NewUser(3, EntityType.ProductionCompany, 5);
        var projectType = NewProjectType(7);
        var project = NewProject(id: 10, producer.UserId, locationManager.UserId, projectType.Id, productionCompanyId: 5);
        project.WorkPermitImagePath = "/uploads/work-permits/original.pdf";

        var (service, _) = BuildService(
            users: [currentUser, producer, locationManager],
            projectTypes: [projectType],
            projects: [project]);

        var dto = ValidUpdateDto(producer.UserId, locationManager.UserId, projectType.Id);
        dto.WorkPermitImagePath = "/uploads/work-permits/replaced.pdf";

        var result = await service.UpdateAsync(10, dto, currentUser.UserId);

        Assert.Equal("/uploads/work-permits/replaced.pdf", result.Data!.WorkPermitImagePath);
    }

    [Fact]
    public async Task UpdateAsync_ProjectNotFound_ReturnsFailure()
    {
        var currentUser = NewUser(1, EntityType.ProductionCompany, 5);
        var (service, _) = BuildService(users: [currentUser], projectTypes: []);

        var result = await service.UpdateAsync(404, ValidUpdateDto(2, 3, 1), currentUser.UserId);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task UpdateAsync_ProjectBelongsToDifferentProductionCompany_ReturnsFailure()
    {
        var currentUser = NewUser(1, EntityType.ProductionCompany, 5);
        var producer = NewUser(2, EntityType.ProductionCompany, 99);
        var locationManager = NewUser(3, EntityType.ProductionCompany, 99);
        var projectType = NewProjectType(7);
        var project = NewProject(id: 10, producer.UserId, locationManager.UserId, projectType.Id, productionCompanyId: 99);

        var (service, _) = BuildService(
            users: [currentUser, producer, locationManager],
            projectTypes: [projectType],
            projects: [project]);

        var result = await service.UpdateAsync(10, ValidUpdateDto(producer.UserId, locationManager.UserId, projectType.Id), currentUser.UserId);

        Assert.False(result.Success);
        Assert.Equal(403, result.StatusCode);
    }

    [Fact]
    public async Task UpdateAsync_InvalidPayload_ReusesCreateValidation_ReturnsFailure()
    {
        var currentUser = NewUser(1, EntityType.ProductionCompany, 5);
        var producer = NewUser(2, EntityType.ProductionCompany, 5);
        var locationManager = NewUser(3, EntityType.ProductionCompany, 5);
        var projectType = NewProjectType(7);
        var project = NewProject(id: 10, producer.UserId, locationManager.UserId, projectType.Id, productionCompanyId: 5);

        var (service, _) = BuildService(
            users: [currentUser, producer, locationManager],
            projectTypes: [projectType],
            projects: [project]);

        var dto = ValidUpdateDto(producer.UserId, locationManager.UserId, projectType.Id);
        dto.EndDate = dto.StartDate;

        var result = await service.UpdateAsync(10, dto, currentUser.UserId);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
    }

    // ---------------------------------------------------------------------
    // DeleteAsync
    // ---------------------------------------------------------------------

    [Fact]
    public async Task DeleteAsync_ValidRequest_SoftDeletesProject()
    {
        var currentUser = NewUser(1, EntityType.ProductionCompany, 5);
        var producer = NewUser(2, EntityType.ProductionCompany, 5);
        var locationManager = NewUser(3, EntityType.ProductionCompany, 5);
        var projectType = NewProjectType(7);
        var project = NewProject(id: 10, producer.UserId, locationManager.UserId, projectType.Id, productionCompanyId: 5);

        var (service, projectRepository) = BuildService(
            users: [currentUser, producer, locationManager],
            projectTypes: [projectType],
            projects: [project]);

        await service.DeleteAsync(10, currentUser.UserId);

        // Remove()+SaveChangesAsync must never hard-delete — only flip the soft-delete flags.
        Assert.True(project.IsDeleted);
        Assert.NotNull(project.DeletedAt);
        Assert.Same(project, projectRepository.RemovedProject);
    }

    [Fact]
    public async Task DeleteAsync_ProjectNotFound_ReturnsFailure()
    {
        var currentUser = NewUser(1, EntityType.ProductionCompany, 5);
        var (service, _) = BuildService(users: [currentUser], projectTypes: []);

        var result = await service.DeleteAsync(404, currentUser.UserId);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task DeleteAsync_ProjectBelongsToDifferentProductionCompany_ReturnsFailure()
    {
        var currentUser = NewUser(1, EntityType.ProductionCompany, 5);
        var producer = NewUser(2, EntityType.ProductionCompany, 99);
        var locationManager = NewUser(3, EntityType.ProductionCompany, 99);
        var projectType = NewProjectType(7);
        var project = NewProject(id: 10, producer.UserId, locationManager.UserId, projectType.Id, productionCompanyId: 99);

        var (service, _) = BuildService(
            users: [currentUser, producer, locationManager],
            projectTypes: [projectType],
            projects: [project]);

        var result = await service.DeleteAsync(10, currentUser.UserId);

        Assert.False(result.Success);
        Assert.Equal(403, result.StatusCode);
    }

    // ---------------------------------------------------------------------
    // RestoreAsync
    // ---------------------------------------------------------------------

    [Fact]
    public async Task RestoreAsync_DeletedProject_RestoresAndReturnsProject()
    {
        var currentUser = NewUser(1, EntityType.ProductionCompany, 5);
        var producer = NewUser(2, EntityType.ProductionCompany, 5);
        var locationManager = NewUser(3, EntityType.ProductionCompany, 5);
        var projectType = NewProjectType(7);
        var project = NewProject(id: 10, producer.UserId, locationManager.UserId, projectType.Id, productionCompanyId: 5);
        project.IsDeleted = true;
        project.DeletedAt = DateTime.UtcNow;

        var (service, _) = BuildService(
            users: [currentUser, producer, locationManager],
            projectTypes: [projectType],
            projects: [project]);

        var result = await service.RestoreAsync(10, currentUser.UserId);

        Assert.False(result.Data!.IsDeleted);
        Assert.False(project.IsDeleted);
        Assert.Null(project.DeletedAt);
    }

    [Fact]
    public async Task RestoreAsync_ProjectNotFound_ReturnsFailure()
    {
        var currentUser = NewUser(1, EntityType.ProductionCompany, 5);
        var (service, _) = BuildService(users: [currentUser], projectTypes: []);

        var result = await service.RestoreAsync(404, currentUser.UserId);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task RestoreAsync_ProjectNotDeleted_ReturnsFailure()
    {
        var currentUser = NewUser(1, EntityType.ProductionCompany, 5);
        var producer = NewUser(2, EntityType.ProductionCompany, 5);
        var locationManager = NewUser(3, EntityType.ProductionCompany, 5);
        var projectType = NewProjectType(7);
        var project = NewProject(id: 10, producer.UserId, locationManager.UserId, projectType.Id, productionCompanyId: 5);

        var (service, _) = BuildService(
            users: [currentUser, producer, locationManager],
            projectTypes: [projectType],
            projects: [project]);

        var result = await service.RestoreAsync(10, currentUser.UserId);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task RestoreAsync_ProjectBelongsToDifferentProductionCompany_ReturnsFailure()
    {
        var currentUser = NewUser(1, EntityType.ProductionCompany, 5);
        var producer = NewUser(2, EntityType.ProductionCompany, 99);
        var locationManager = NewUser(3, EntityType.ProductionCompany, 99);
        var projectType = NewProjectType(7);
        var project = NewProject(id: 10, producer.UserId, locationManager.UserId, projectType.Id, productionCompanyId: 99);
        project.IsDeleted = true;

        var (service, _) = BuildService(
            users: [currentUser, producer, locationManager],
            projectTypes: [projectType],
            projects: [project]);

        var result = await service.RestoreAsync(10, currentUser.UserId);

        Assert.False(result.Success);
        Assert.Equal(403, result.StatusCode);
    }

    // ---------------------------------------------------------------------
    // GetProjectTypesAsync
    // ---------------------------------------------------------------------

    [Fact]
    public async Task GetProjectTypesAsync_ReturnsMappedCatalog()
    {
        var (service, _) = BuildService(users: [], projectTypes: [NewProjectType(1), NewProjectType(2)]);

        var result = await service.GetProjectTypesAsync();

        Assert.Equal(2, result.Data!.Count);
        Assert.All(result.Data!, dto => Assert.Equal("Feature Film", dto.NameEn));
    }

    // ---------------------------------------------------------------------
    // Test infrastructure
    // ---------------------------------------------------------------------

    private static Project NewProject(int id, int producerUserId, int locationManagerUserId, int projectTypeId, int productionCompanyId) => new()
    {
        Id = id,
        ProjectNameEn = "Existing Project",
        ProjectNameAr = "مشروع قائم",
        StartDate = new DateTime(2026, 1, 1),
        EndDate = new DateTime(2026, 6, 1),
        ProjectTypeId = projectTypeId,
        ProducerUserId = producerUserId,
        LocationManagerUserId = locationManagerUserId,
        WorkPermitImagePath = "/uploads/work-permits/existing.pdf",
        ProductionCompanyId = productionCompanyId,
        IsActive = true
    };

    private static (ProjectService Service, FakeProjectRepository ProjectRepository) BuildService(
        User[] users,
        ProjectType[] projectTypes,
        Project[]? projects = null)
    {
        var usersById = users.ToDictionary(u => u.UserId);
        var projectTypesById = projectTypes.ToDictionary(pt => pt.Id);

        var userRepository = new FakeUserRepository(usersById);
        var projectTypeRepository = new FakeProjectTypeRepository(projectTypes);
        var projectRepository = new FakeProjectRepository(usersById, projectTypesById, projects ?? []);

        var unitOfWork = new FakeUnitOfWork(userRepository, projectRepository, projectTypeRepository);
        return (new ProjectService(unitOfWork), projectRepository);
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly Dictionary<int, User> _usersById;

        public FakeUserRepository(Dictionary<int, User> usersById) => _usersById = usersById;
        public Task<List<User>> GetAssociationSupervisorsAsync(EntityType entityType, int? entityId = null, CancellationToken cancellationToken = default)
    => Task.FromResult(new List<User>());
        public Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_usersById.GetValueOrDefault(userId));

        // Not exercised by ProjectService.
        public Task<User?> GetByEmailWithAccessAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<User>> GetByEntityAsync(EntityType entityType, int entityId, string? search, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<User>> GetDeletedByEntityAsync(EntityType entityType, int entityId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(List<User> Users, int TotalCount)> GetPagedByEntityAsync(EntityType entityType, int entityId, string? search, bool? isActive, int page, int pageSize, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetDetailsAsync(int userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetDetailsReadOnlyAsync(int userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByUserNameEnAsync(User user, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByUserNameArAsync(User user, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetWithPermissionsAsync(int userId, CancellationToken cancellationToken = default)
        {
            var user = _usersById.GetValueOrDefault(userId);
            if (user is not null && user.Role == null)
            {
                user.Role = BuildProjectRole(user.RoleId);
            }

            return Task.FromResult(user);
        }
        public Task<List<User>> GetByIdsInEntityAsync(IEnumerable<int> userIds, EntityType entityType, int entityId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddAsync(User user, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public void Remove(User user) => throw new NotSupportedException();

        private static Role BuildProjectRole(int roleId)
        {
            var role = new Role
            {
                RoleId = roleId,
                RoleNameEn = $"Role {roleId}",
                RoleNameAr = $"Role {roleId}",
                IsActive = true
            };

            foreach (var permissionId in new[] { 25, 26, 27, 28, 29, 30 })
            {
                var permission = new Permission
                {
                    PermissionId = permissionId,
                    PermissionNameEn = $"Permission {permissionId}",
                    PermissionNameAr = $"Permission {permissionId}",
                    Module = "Projects",
                    IsActive = true
                };

                role.RolePermissions.Add(new RolePermission
                {
                    RoleId = role.RoleId,
                    Role = role,
                    PermissionId = permissionId,
                    Permission = permission,
                    IsActive = true
                });
            }

            return role;
        }

        public Task<bool> PhoneExistsAsync(string phoneNumber, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<User?> GetAssociationAdminForUpdateAsync(int associationId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> EmailExistsForAnotherUserAsync(string email, int excludedUserId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> PhoneExistsForAnotherUserAsync(string phoneNumber, int excludedUserId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<List<User>> GetAssociationUsersForUpdateAsync(int associationId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<List<User>> GetDeletedAssociationUsersForUpdateAsync(int associationId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }

    private sealed class FakeProjectTypeRepository : IProjectTypeRepository
    {
        private readonly List<ProjectType> _projectTypes;
        private readonly HashSet<int> _existingIds;

        public FakeProjectTypeRepository(IEnumerable<ProjectType> projectTypes)
        {
            _projectTypes = projectTypes.ToList();
            _existingIds = _projectTypes.Select(pt => pt.Id).ToHashSet();
        }

        public Task<bool> ExistsAsync(int projectTypeId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_existingIds.Contains(projectTypeId));

        public Task<List<ProjectType>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(_projectTypes);
    }

    // Re-hydrates Producer/LocationManager/ProjectType navigation properties from current FK
    // values on every fetch — simulating a fresh EF .Include() query, which is what makes
    // UpdateAsync's re-fetch-after-save correctly reflect a changed ProducerUserId/etc. Remove()
    // simulates MaydanDbContext.SaveChangesAsync's global soft-delete interception directly
    // (there's no real change-tracking pipeline in this fake to intercept), matching the exact
    // documented behavior in ProjectService.DeleteAsync's own comment.
    private sealed class FakeProjectRepository : IProjectRepository
    {
        private readonly Dictionary<int, Project> _projectsById;
        private readonly Dictionary<int, User> _usersById;
        private readonly Dictionary<int, ProjectType> _projectTypesById;
        private int _nextId = 100;

        public FakeProjectRepository(Dictionary<int, User> usersById, Dictionary<int, ProjectType> projectTypesById, IEnumerable<Project> projects)
        {
            _usersById = usersById;
            _projectTypesById = projectTypesById;
            _projectsById = projects.ToDictionary(p => p.Id);
        }

        public Project? AddedProject { get; private set; }
        public Project? RemovedProject { get; private set; }

        public Task<Project?> GetByIdAsync(int projectId, CancellationToken cancellationToken = default)
        {
            var project = _projectsById.GetValueOrDefault(projectId);
            if (project is null || project.IsDeleted)
            {
                return Task.FromResult<Project?>(null);
            }

            Hydrate(project);
            return Task.FromResult<Project?>(project);
        }

        public Task<Project?> GetByIdIncludingDeletedAsync(int projectId, CancellationToken cancellationToken = default)
        {
            var project = _projectsById.GetValueOrDefault(projectId);
            if (project is not null)
            {
                Hydrate(project);
            }

            return Task.FromResult(project);
        }

        public Task<List<Project>> GetAllAsync(
          bool isDeleted,
          string? searchTerm,
          DateTime? startDate,
          DateTime? endDate,
          string? searchByProductionCompanyName,
          int? searchByProductionCompanyId,
          int? cityId,
          int? cityLocationId,
          bool? IsActive,
          CancellationToken cancellationToken = default)
        {
            var results = _projectsById.Values.Where(p => p.IsDeleted == isDeleted);

            if (searchByProductionCompanyId.HasValue)
            {
                results = results.Where(p => p.ProductionCompanyId == searchByProductionCompanyId.Value);
            }

            var list = results.ToList();
            foreach (var project in list)
            {
                Hydrate(project);
            }

            return Task.FromResult(list);
        }


        public Task<bool> HasLocationManagerOverlapAsync(
            int locationManagerUserId,
            DateTime projectStartDate,
            DateTime projectEndDate,
            int? excludedProjectId,
            CancellationToken cancellationToken = default)
        {
            var hasOverlap = _projectsById.Values.Any(project =>
                !project.IsDeleted &&
                project.LocationManagerUserId == locationManagerUserId &&
                (!excludedProjectId.HasValue ||
                    project.Id != excludedProjectId.Value) &&
                project.StartDate.Date <= projectEndDate.Date &&
                project.EndDate.Date >= projectStartDate.Date);

            return Task.FromResult(hasOverlap);
        }


        public Task AddAsync(Project project, CancellationToken cancellationToken = default)
        {
            project.Id = _nextId++;
            Hydrate(project);
            AddedProject = project;
            _projectsById[project.Id] = project;
            return Task.CompletedTask;
        }

        public void Remove(Project project)
        {
            project.IsDeleted = true;
            project.DeletedAt = DateTime.UtcNow;
            RemovedProject = project;
        }

        // Not exercised by ProjectService.
        public Task<List<Project>> GetByProductionCompanyIdAsync(int productionCompanyId, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        private void Hydrate(Project project)
        {
            project.Producer = _usersById[project.ProducerUserId];
            project.LocationManager = _usersById[project.LocationManagerUserId];
            project.ProjectType = _projectTypesById[project.ProjectTypeId];
        }
        public Task<string?> CheckOverlapConflictsAsync(
     DateTime startDate,
     DateTime endDate,
     int producerId,
     int locationManagerId,
     int cityLocationId,
     int? excludeProjectId = null,
     CancellationToken cancellationToken = default)
        {
         
            return Task.FromResult<string?>(null);
        }

        public Task<List<int>> GetBusyUserIdsAsync(int productionCompanyId, DateTime startDate, DateTime endDate, int? excludeProjectId = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public FakeUnitOfWork(IUserRepository users, IProjectRepository projects, IProjectTypeRepository projectTypes)
        {
            Users = users;
            Projects = projects;
            ProjectTypes = projectTypes;
        }

        public IUserRepository Users { get; }
        public IProjectRepository Projects { get; }
        public IProjectTypeRepository ProjectTypes { get; }
        public IRoleRepository Roles => throw new NotSupportedException();
        public IPermissionRepository Permissions => throw new NotSupportedException();
        public IGroupRepository Groups => throw new NotSupportedException();
        public ICountryRepository Countries => throw new NotSupportedException();
        public ICityRepository Cities => throw new NotSupportedException();
        public IAssociationRepository Associations => throw new NotSupportedException();
        public IProductionCompanyRepository ProductionCompanies => throw new NotSupportedException();
        public IWorkerRepository Workers => throw new NotSupportedException();
        public ICityLocationRepository CityLocations => throw new NotSupportedException();
        public IAssociationProjectSupervisorRepository AssociationProjectSupervisors => throw new NotSupportedException();
        public IPasswordResetTokenRepository PasswordResetTokens => throw new NotSupportedException();
        public IRefreshTokenRepository RefreshTokens => throw new NotSupportedException();
        public ISystemConfigurationRepository SystemConfigurations => throw new NotSupportedException();
        public IServiceRepository Services => throw new NotSupportedException();

        public Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
    }
}
