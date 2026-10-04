using Maydan.Application.DTOs.Projects;
using Maydan.Application.Interfaces;
using Maydan.Application.Services;
using Maydan.Domain.Entities;
using Maydan.Domain.Enums;

namespace Maydan.Application.Tests.Services;

// Projects audit follow-up: ProjectService.CreateAsync used to throw a bare Exception for every
// validation failure, which ApiControllerBase.HandleException's switch has no case for (always
// fell through to 500 regardless of the real problem). These tests cover the full CRUD+restore
// surface added in this pass, asserting the specific exception type each failure now throws
// (InvalidOperationException / KeyNotFoundException / UnauthorizedAccessException), matching the
// convention UserManagementServiceTests.cs already established — same hand-rolled-fake pattern,
// no mocking library exists in this test project.
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
    // CreateAsync — regression coverage now that every throw has a real type
    // ---------------------------------------------------------------------

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

        Assert.Equal("New Documentary", result.ProjectNameEn);
        Assert.Equal(5, result.ProductionCompanyId);
        Assert.Equal(producer.UserId, result.ProducerUserId);
        Assert.Equal($"{producer.FirstNameEn} {producer.LastNameEn}", result.ProducerNameEn);
        Assert.False(result.IsDeleted);
        Assert.NotNull(projectRepository.AddedProject);
        Assert.Equal(currentUser.UserId, projectRepository.AddedProject!.CreatedBy);
    }

    [Fact]
    public async Task CreateAsync_CurrentUserNotFound_ThrowsUnauthorizedAccessException()
    {
        var (service, _) = BuildService(users: [], projectTypes: [NewProjectType(1)]);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.CreateAsync(ValidCreateDto(2, 3, 1), currentUserId: 999));
    }

    [Fact]
    public async Task CreateAsync_CurrentUserNotProductionCompany_ThrowsInvalidOperationException()
    {
        var currentUser = NewUser(1, EntityType.Association, 5);
        var (service, _) = BuildService(users: [currentUser], projectTypes: [NewProjectType(1)]);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(ValidCreateDto(2, 3, 1), currentUser.UserId));

        Assert.Contains("Production Company", exception.Message);
    }

    [Fact]
    public async Task CreateAsync_MissingEnglishName_ThrowsInvalidOperationException()
    {
        var currentUser = NewUser(1, EntityType.ProductionCompany, 5);
        var producer = NewUser(2, EntityType.ProductionCompany, 5);
        var locationManager = NewUser(3, EntityType.ProductionCompany, 5);
        var projectType = NewProjectType(7);
        var (service, _) = BuildService(users: [currentUser, producer, locationManager], projectTypes: [projectType]);

        var dto = ValidCreateDto(producer.UserId, locationManager.UserId, projectType.Id);
        dto.ProjectNameEn = "   ";

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(dto, currentUser.UserId));
    }

    [Fact]
    public async Task CreateAsync_EndDateNotAfterStartDate_ThrowsInvalidOperationException()
    {
        var currentUser = NewUser(1, EntityType.ProductionCompany, 5);
        var producer = NewUser(2, EntityType.ProductionCompany, 5);
        var locationManager = NewUser(3, EntityType.ProductionCompany, 5);
        var projectType = NewProjectType(7);
        var (service, _) = BuildService(users: [currentUser, producer, locationManager], projectTypes: [projectType]);

        var dto = ValidCreateDto(producer.UserId, locationManager.UserId, projectType.Id);
        dto.EndDate = dto.StartDate;

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(dto, currentUser.UserId));
    }

    [Fact]
    public async Task CreateAsync_ProducerSameAsLocationManager_ThrowsInvalidOperationException()
    {
        var currentUser = NewUser(1, EntityType.ProductionCompany, 5);
        var producer = NewUser(2, EntityType.ProductionCompany, 5);
        var projectType = NewProjectType(7);
        var (service, _) = BuildService(users: [currentUser, producer], projectTypes: [projectType]);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(ValidCreateDto(producer.UserId, producer.UserId, projectType.Id), currentUser.UserId));
    }

    [Fact]
    public async Task CreateAsync_InvalidProjectType_ThrowsKeyNotFoundException()
    {
        var currentUser = NewUser(1, EntityType.ProductionCompany, 5);
        var producer = NewUser(2, EntityType.ProductionCompany, 5);
        var locationManager = NewUser(3, EntityType.ProductionCompany, 5);
        var (service, _) = BuildService(users: [currentUser, producer, locationManager], projectTypes: []);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.CreateAsync(ValidCreateDto(producer.UserId, locationManager.UserId, 999), currentUser.UserId));
    }

    [Fact]
    public async Task CreateAsync_ProducerNotFound_ThrowsKeyNotFoundException()
    {
        var currentUser = NewUser(1, EntityType.ProductionCompany, 5);
        var locationManager = NewUser(3, EntityType.ProductionCompany, 5);
        var projectType = NewProjectType(7);
        var (service, _) = BuildService(users: [currentUser, locationManager], projectTypes: [projectType]);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.CreateAsync(ValidCreateDto(producerUserId: 404, locationManager.UserId, projectType.Id), currentUser.UserId));
    }

    [Fact]
    public async Task CreateAsync_ProducerDifferentCompany_ThrowsInvalidOperationException()
    {
        var currentUser = NewUser(1, EntityType.ProductionCompany, 5);
        var outsideProducer = NewUser(2, EntityType.ProductionCompany, 99);
        var locationManager = NewUser(3, EntityType.ProductionCompany, 5);
        var projectType = NewProjectType(7);
        var (service, _) = BuildService(users: [currentUser, outsideProducer, locationManager], projectTypes: [projectType]);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(ValidCreateDto(outsideProducer.UserId, locationManager.UserId, projectType.Id), currentUser.UserId));

        Assert.Contains("same Production Company", exception.Message);
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

        Assert.Single(results);
        Assert.Equal(10, results[0].Id);
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

        var result = Assert.Single(results);
        Assert.Equal(assignedProject.Id, result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_UnassignedProductionCompanyUserWithoutAllProjectsPermission_ThrowsUnauthorizedAccessException()
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

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.GetByIdAsync(currentUser.UserId, project.Id));
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

        Assert.Equal(10, result.Id);
        Assert.Equal(projectType.NameEn, result.ProjectTypeNameEn);
    }

    [Fact]
    public async Task GetByIdAsync_MissingProject_ThrowsKeyNotFoundException()
    {
        var currentUser = NewUser(1, EntityType.ProductionCompany, 5);
        var (service, _) = BuildService(users: [currentUser], projectTypes: []);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetByIdAsync(currentUser.UserId, 404));
    }

    [Fact]
    public async Task GetByIdAsync_SoftDeletedProject_ThrowsKeyNotFoundException()
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
        // invisible to the normal get-by-id path, the same way it would 404 in production.
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetByIdAsync(producer.UserId, 10));
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

        Assert.Equal("Updated Documentary", result.ProjectNameEn);
        Assert.Equal(newProducer.UserId, result.ProducerUserId);
        // WorkPermitImagePath was null on the update request — the existing path must survive.
        Assert.Equal("/uploads/work-permits/original.pdf", result.WorkPermitImagePath);
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

        Assert.Equal("/uploads/work-permits/replaced.pdf", result.WorkPermitImagePath);
    }

    [Fact]
    public async Task UpdateAsync_ProjectNotFound_ThrowsKeyNotFoundException()
    {
        var currentUser = NewUser(1, EntityType.ProductionCompany, 5);
        var (service, _) = BuildService(users: [currentUser], projectTypes: []);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.UpdateAsync(404, ValidUpdateDto(2, 3, 1), currentUser.UserId));
    }

    [Fact]
    public async Task UpdateAsync_ProjectBelongsToDifferentProductionCompany_ThrowsUnauthorizedAccessException()
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

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.UpdateAsync(10, ValidUpdateDto(producer.UserId, locationManager.UserId, projectType.Id), currentUser.UserId));
    }

    [Fact]
    public async Task UpdateAsync_InvalidPayload_ReusesCreateValidation_ThrowsInvalidOperationException()
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

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateAsync(10, dto, currentUser.UserId));
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
    public async Task DeleteAsync_ProjectNotFound_ThrowsKeyNotFoundException()
    {
        var currentUser = NewUser(1, EntityType.ProductionCompany, 5);
        var (service, _) = BuildService(users: [currentUser], projectTypes: []);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.DeleteAsync(404, currentUser.UserId));
    }

    [Fact]
    public async Task DeleteAsync_ProjectBelongsToDifferentProductionCompany_ThrowsUnauthorizedAccessException()
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

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.DeleteAsync(10, currentUser.UserId));
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

        Assert.False(result.IsDeleted);
        Assert.False(project.IsDeleted);
        Assert.Null(project.DeletedAt);
    }

    [Fact]
    public async Task RestoreAsync_ProjectNotFound_ThrowsKeyNotFoundException()
    {
        var currentUser = NewUser(1, EntityType.ProductionCompany, 5);
        var (service, _) = BuildService(users: [currentUser], projectTypes: []);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.RestoreAsync(404, currentUser.UserId));
    }

    [Fact]
    public async Task RestoreAsync_ProjectNotDeleted_ThrowsInvalidOperationException()
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

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RestoreAsync(10, currentUser.UserId));
    }

    [Fact]
    public async Task RestoreAsync_ProjectBelongsToDifferentProductionCompany_ThrowsUnauthorizedAccessException()
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

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.RestoreAsync(10, currentUser.UserId));
    }

    // ---------------------------------------------------------------------
    // GetProjectTypesAsync
    // ---------------------------------------------------------------------

    [Fact]
    public async Task GetProjectTypesAsync_ReturnsMappedCatalog()
    {
        var (service, _) = BuildService(users: [], projectTypes: [NewProjectType(1), NewProjectType(2)]);

        var result = await service.GetProjectTypesAsync();

        Assert.Equal(2, result.Count);
        Assert.All(result, dto => Assert.Equal("Feature Film", dto.NameEn));
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


        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
    }
}
