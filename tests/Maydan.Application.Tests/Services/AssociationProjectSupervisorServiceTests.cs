using Maydan.Application.DTOs.Associations;
using Maydan.Application.Interfaces;
using Maydan.Application.Services;
using Maydan.Domain.Entities;
using Maydan.Domain.Enums;

namespace Maydan.Application.Tests.Services;

// Association Management, Phase 2e — AssociationProjectSupervisorService is the first real
// consumer of IAssociationProjectSupervisorRepository. Same hand-rolled-fake-per-file pattern as
// every other service test file in this project (no mocking library).
public class AssociationProjectSupervisorServiceTests
{
    private static Role BuildRole(int roleId, string nameEn) => new() { RoleId = roleId, RoleNameEn = nameEn, RoleNameAr = nameEn };

    private static User BuildCaller(int userId) => new()
    {
        UserId = userId,
        FirstNameEn = "Caller",
        LastNameEn = $"User{userId}",
        FirstNameAr = "مستخدم",
        LastNameAr = $"{userId}",
        Role = BuildRole(1, "Bayt-AlUrdon"),
        EntityType = EntityType.BaytAlUrdon,
        EntityId = 1,
        IsActive = true
    };

    private static User BuildAssociationUser(int userId, int associationId, bool isActive = true) => new()
    {
        UserId = userId,
        FirstNameEn = $"First{userId}",
        LastNameEn = $"Last{userId}",
        FirstNameAr = $"اول{userId}",
        LastNameAr = $"اخير{userId}",
        Email = $"user{userId}@test.local",
        PhoneNumber = "+962700000000",
        Role = BuildRole(4, "Association"),
        EntityType = EntityType.Association,
        EntityId = associationId,
        IsActive = isActive
    };

    private static User BuildProductionCompanyUser(int userId) => new()
    {
        UserId = userId,
        FirstNameEn = $"First{userId}",
        LastNameEn = $"Last{userId}",
        FirstNameAr = $"اول{userId}",
        LastNameAr = $"اخير{userId}",
        Role = BuildRole(3, "ProductionHouse"),
        EntityType = EntityType.ProductionCompany,
        EntityId = 1,
        IsActive = true
    };

    private static Project BuildProject(int id, string nameEn = "Project") => new()
    {
        Id = id,
        ProjectNameEn = $"{nameEn}{id}",
        ProjectNameAr = $"مشروع{id}",
        IsActive = true
    };

    private static Association BuildAssociation(int id, string nameEn = "Association") => new()
    {
        Id = id,
        EnglishName = $"{nameEn}{id}",
        ArabicName = $"جمعية{id}",
        IsActive = true
    };

    [Fact]
    public async Task CreateAsync_ValidRequest_Succeeds()
    {
        var caller = BuildCaller(1);
        var project = BuildProject(10);
        var association = BuildAssociation(20);
        var associationUser = BuildAssociationUser(30, association.Id);
        var (service, _) = BuildService(users: [caller, associationUser], projects: [project], associations: [association]);

        var result = await service.CreateAsync(
            new CreateAssociationProjectSupervisorDto { ProjectId = project.Id, AssociationUserId = associationUser.UserId },
            caller.UserId);

        Assert.Equal(project.Id, result.ProjectId);
        Assert.Equal(associationUser.UserId, result.AssociationUserId);
        Assert.Equal(association.Id, result.AssociationId);
        Assert.Equal(project.ProjectNameEn, result.ProjectName);
        Assert.Equal(association.EnglishName, result.AssociationName);
        Assert.Equal("First30 Last30", result.UserName);
        Assert.Equal(associationUser.Email, result.Email);
        Assert.Equal(associationUser.PhoneNumber, result.PhoneNumber);
    }

    [Fact]
    public async Task CreateAsync_UnknownProject_ThrowsKeyNotFoundException()
    {
        var caller = BuildCaller(1);
        var association = BuildAssociation(20);
        var associationUser = BuildAssociationUser(30, association.Id);
        var (service, _) = BuildService(users: [caller, associationUser], projects: [], associations: [association]);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.CreateAsync(new CreateAssociationProjectSupervisorDto { ProjectId = 999, AssociationUserId = associationUser.UserId }, caller.UserId));
    }

    [Fact]
    public async Task CreateAsync_UnknownUser_ThrowsKeyNotFoundException()
    {
        var caller = BuildCaller(1);
        var project = BuildProject(10);
        var (service, _) = BuildService(users: [caller], projects: [project], associations: []);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.CreateAsync(new CreateAssociationProjectSupervisorDto { ProjectId = project.Id, AssociationUserId = 999 }, caller.UserId));
    }

    [Fact]
    public async Task CreateAsync_NonAssociationUser_ThrowsInvalidOperationException()
    {
        var caller = BuildCaller(1);
        var project = BuildProject(10);
        var productionCompanyUser = BuildProductionCompanyUser(40);
        var (service, _) = BuildService(users: [caller, productionCompanyUser], projects: [project], associations: []);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(new CreateAssociationProjectSupervisorDto { ProjectId = project.Id, AssociationUserId = productionCompanyUser.UserId }, caller.UserId));
    }

    [Fact]
    public async Task GetAllAsync_NoFilters_ReturnsEverything()
    {
        var caller = BuildCaller(1);
        var projectA = BuildProject(10);
        var projectB = BuildProject(11);
        var associationA = BuildAssociation(20);
        var associationB = BuildAssociation(21);
        var userA = BuildAssociationUser(30, associationA.Id);
        var userB = BuildAssociationUser(31, associationB.Id);

        var (service, repository) = BuildService(
            users: [caller, userA, userB], projects: [projectA, projectB], associations: [associationA, associationB]);

        repository.Seed(new AssociationProjectSupervisor { Id = 1, ProjectId = projectA.Id, UserId = userA.UserId, Project = projectA, User = userA });
        repository.Seed(new AssociationProjectSupervisor { Id = 2, ProjectId = projectB.Id, UserId = userB.UserId, Project = projectB, User = userB });

        var result = await service.GetAllAsync(projectId: 0, associationId: 0);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetAllAsync_FilterByProjectId_ReturnsOnlyMatching()
    {
        var caller = BuildCaller(1);
        var projectA = BuildProject(10);
        var projectB = BuildProject(11);
        var associationA = BuildAssociation(20);
        var userA = BuildAssociationUser(30, associationA.Id);
        var userB = BuildAssociationUser(31, associationA.Id);

        var (service, repository) = BuildService(
            users: [caller, userA, userB], projects: [projectA, projectB], associations: [associationA]);

        repository.Seed(new AssociationProjectSupervisor { Id = 1, ProjectId = projectA.Id, UserId = userA.UserId, Project = projectA, User = userA });
        repository.Seed(new AssociationProjectSupervisor { Id = 2, ProjectId = projectB.Id, UserId = userB.UserId, Project = projectB, User = userB });

        var result = await service.GetAllAsync(projectId: projectA.Id, associationId: 0);

        var dto = Assert.Single(result);
        Assert.Equal(projectA.Id, dto.ProjectId);
    }

    [Fact]
    public async Task GetAllAsync_FilterByAssociationId_ReturnsOnlyMatching()
    {
        var caller = BuildCaller(1);
        var project = BuildProject(10);
        var associationA = BuildAssociation(20);
        var associationB = BuildAssociation(21);
        var userA = BuildAssociationUser(30, associationA.Id);
        var userB = BuildAssociationUser(31, associationB.Id);

        var (service, repository) = BuildService(
            users: [caller, userA, userB], projects: [project], associations: [associationA, associationB]);

        repository.Seed(new AssociationProjectSupervisor { Id = 1, ProjectId = project.Id, UserId = userA.UserId, Project = project, User = userA });
        repository.Seed(new AssociationProjectSupervisor { Id = 2, ProjectId = project.Id, UserId = userB.UserId, Project = project, User = userB });

        var result = await service.GetAllAsync(projectId: 0, associationId: associationA.Id);

        var dto = Assert.Single(result);
        Assert.Equal(associationA.Id, dto.AssociationId);
    }

    [Fact]
    public async Task DeleteAsync_ExistingAssignment_SoftDeletes()
    {
        var caller = BuildCaller(1);
        var project = BuildProject(10);
        var association = BuildAssociation(20);
        var associationUser = BuildAssociationUser(30, association.Id);

        var (service, repository) = BuildService(
            users: [caller, associationUser], projects: [project], associations: [association]);

        var supervisor = new AssociationProjectSupervisor { Id = 1, ProjectId = project.Id, UserId = associationUser.UserId, Project = project, User = associationUser };
        repository.Seed(supervisor);

        await service.DeleteAsync(supervisor.Id, caller.UserId);

        Assert.True(supervisor.IsDeleted);
    }

    [Fact]
    public async Task DeleteAsync_UnknownId_ThrowsKeyNotFoundException()
    {
        var caller = BuildCaller(1);
        var (service, _) = BuildService(users: [caller], projects: [], associations: []);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.DeleteAsync(999, caller.UserId));
    }

    private static (AssociationProjectSupervisorService Service, FakeAssociationProjectSupervisorRepository Repository) BuildService(
        User[] users, Project[] projects, Association[] associations)
    {
        var userRepository = new FakeUserRepository(users);
        var projectRepository = new FakeProjectRepository(projects);
        var associationRepository = new FakeAssociationRepository(associations);
        var supervisorRepository = new FakeAssociationProjectSupervisorRepository();

        var unitOfWork = new FakeUnitOfWork(userRepository, projectRepository, associationRepository, supervisorRepository);
        return (new AssociationProjectSupervisorService(unitOfWork), supervisorRepository);
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly Dictionary<int, User> _usersById;

        public FakeUserRepository(IEnumerable<User> users) => _usersById = users.ToDictionary(u => u.UserId);

        public Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken = default)
        {
            var user = _usersById.GetValueOrDefault(userId);
            return Task.FromResult(user is { IsDeleted: false } ? user : null);
        }

        public Task<User?> GetByEmailWithAccessAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<User>> GetByEntityAsync(EntityType entityType, int entityId, string? search, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<User>> GetDeletedByEntityAsync(EntityType entityType, int entityId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(List<User> Users, int TotalCount)> GetPagedByEntityAsync(EntityType entityType, int entityId, string? search, bool? isActive, int page, int pageSize, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetDetailsAsync(int userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetDetailsReadOnlyAsync(int userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByUserNameEnAsync(User user, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByUserNameArAsync(User user, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetWithPermissionsAsync(int userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<User>> GetByIdsInEntityAsync(IEnumerable<int> userIds, EntityType entityType, int entityId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddAsync(User user, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public void Remove(User user) => throw new NotSupportedException();
    }

    private sealed class FakeProjectRepository : IProjectRepository
    {
        private readonly Dictionary<int, Project> _projectsById;

        public FakeProjectRepository(IEnumerable<Project> projects) => _projectsById = projects.ToDictionary(p => p.Id);

        public Task<Project?> GetByIdAsync(int projectId, CancellationToken cancellationToken = default)
        {
            var project = _projectsById.GetValueOrDefault(projectId);
            return Task.FromResult(project is { IsDeleted: false } ? project : null);
        }

        public Task<Project?> GetByIdIncludingDeletedAsync(int projectId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<Project>> GetByProductionCompanyIdAsync(int productionCompanyId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<Project>> GetAllAsync(bool isDeleted, string? searchTerm, DateTime? startDate, DateTime? endDate, string? searchByProductionCompanyName, int? searchByProductionCompanyId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddAsync(Project project, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public void Remove(Project project) => throw new NotSupportedException();
    }

    private sealed class FakeAssociationRepository : IAssociationRepository
    {
        private readonly Dictionary<int, Association> _associationsById;

        public FakeAssociationRepository(IEnumerable<Association> associations) => _associationsById = associations.ToDictionary(a => a.Id);

        public Task<Association?> GetByIdAsync(int associationId, CancellationToken cancellationToken = default)
        {
            var association = _associationsById.GetValueOrDefault(associationId);
            return Task.FromResult(association is { IsDeleted: false } ? association : null);
        }

        public Task<Association?> GetByIdIncludingDeletedAsync(int associationId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Association?> GetByCityIdAsync(int cityId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Association?> GetByIdWithWorkersAsync(int associationId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(Association Association, int WorkersCount)?> GetByIdWithWorkersCountAsync(int associationId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<Association>> GetAllAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<(Association Association, int WorkersCount)>> QueryAsync(bool isDeleted, string? searchTerm = null, bool? orderByWorkersCountAscending = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddAsync(Association association, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public void Remove(Association association) => throw new NotSupportedException();
    }

    // Applies the exact same nullable-filter semantics IAssociationProjectSupervisorRepository's
    // own real implementation does (ProjectId equality; associationId against the assigned User's
    // own EntityId where EntityType == Association) — the service's 0-vs-null translation is what
    // these tests actually exercise, not EF query mechanics.
    private sealed class FakeAssociationProjectSupervisorRepository : IAssociationProjectSupervisorRepository
    {
        private readonly List<AssociationProjectSupervisor> _supervisors = new();

        public void Seed(AssociationProjectSupervisor supervisor) => _supervisors.Add(supervisor);

        public Task<List<AssociationProjectSupervisor>> GetAllAsync(int? projectId, int? associationId, CancellationToken cancellationToken = default)
        {
            IEnumerable<AssociationProjectSupervisor> query = _supervisors.Where(s => !s.IsDeleted);

            if (projectId is not null)
            {
                query = query.Where(s => s.ProjectId == projectId);
            }

            if (associationId is not null)
            {
                query = query.Where(s => s.User.EntityType == EntityType.Association && s.User.EntityId == associationId);
            }

            return Task.FromResult(query.ToList());
        }

        public Task<AssociationProjectSupervisor?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var supervisor = _supervisors.FirstOrDefault(s => s.Id == id);
            return Task.FromResult(supervisor is { IsDeleted: false } ? supervisor : null);
        }

        public Task AddAsync(AssociationProjectSupervisor supervisor, CancellationToken cancellationToken = default)
        {
            supervisor.Id = supervisor.Id == 0 ? _supervisors.Count + 100 : supervisor.Id;
            _supervisors.Add(supervisor);
            return Task.CompletedTask;
        }

        public void Remove(AssociationProjectSupervisor supervisor)
        {
            supervisor.IsDeleted = true;
            supervisor.DeletedAt = DateTime.UtcNow;
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {


        public FakeUnitOfWork(IUserRepository users, IProjectRepository projects, IAssociationRepository associations, IAssociationProjectSupervisorRepository associationProjectSupervisors)
        {
            Users = users;
            Projects = projects;
            Associations = associations;
            AssociationProjectSupervisors = associationProjectSupervisors;
        }

        public IUserRepository Users { get; }
        public IProjectRepository Projects { get; }
        public IAssociationRepository Associations { get; }
        public IAssociationProjectSupervisorRepository AssociationProjectSupervisors { get; }
        public IRoleRepository Roles => throw new NotSupportedException();
        public IPermissionRepository Permissions => throw new NotSupportedException();
        public IGroupRepository Groups => throw new NotSupportedException();
        public IProjectTypeRepository ProjectTypes => throw new NotSupportedException();
        public ICountryRepository Countries => throw new NotSupportedException();
        public ICityRepository Cities => throw new NotSupportedException();
        public ICityLocationRepository CityLocations => throw new NotSupportedException();
        public IProductionCompanyRepository ProductionCompanies => throw new NotSupportedException();
        public IWorkerRepository Workers => throw new NotSupportedException();
        public IPasswordResetTokenRepository PasswordResetTokens => throw new NotSupportedException();
        public IRefreshTokenRepository RefreshTokens => throw new NotSupportedException();
        public ISystemConfigurationRepository SystemConfigurations => throw new NotSupportedException();
        public IServiceRepository Services => throw new NotSupportedException();

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
        public Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default) => operation();
    }
}
