using Maydan.Application.Interfaces;
using Maydan.Application.Services;
using Maydan.Domain.Entities;
using Maydan.Domain.Enums;

namespace Maydan.Application.Tests.Services;

// System Configuration gate (MAYD-133, 2026-09-24) — Business Rule #5's real decision, in
// isolation from the MVC pipeline (see SystemConfigurationGateFilter's own comment on why the
// filter itself stays thin and untested here). The core regression this proves: the gate is
// super-admin-only, not system-wide — a non-super-admin role is never blocked, under any
// configuration state, exactly matching the scope Yousef confirmed (2026-09-23).
public class SystemConfigurationGateServiceTests
{
    private static User SuperAdmin(int userId = 1, bool isActive = true) =>
        new() { UserId = userId, RoleId = 1, EntityType = EntityType.BaytAlUrdon, IsActive = isActive };

    private static User NonSuperAdmin(int roleId, EntityType entityType) =>
        new() { UserId = 7, RoleId = roleId, EntityType = entityType, IsActive = true };

    [Fact]
    public async Task ShouldBlockAsync_returns_true_for_super_admin_when_not_configured()
    {
        var gate = BuildGate(new FakeUserRepository(SuperAdmin()), isConfigured: false);

        var blocked = await gate.ShouldBlockAsync(1);

        Assert.True(blocked);
    }

    [Fact]
    public async Task ShouldBlockAsync_returns_false_for_super_admin_once_configured()
    {
        var gate = BuildGate(new FakeUserRepository(SuperAdmin()), isConfigured: true);

        var blocked = await gate.ShouldBlockAsync(1);

        Assert.False(blocked);
    }

    [Theory]
    [InlineData(2, EntityType.Aseza)]
    [InlineData(3, EntityType.ProductionCompany)]
    [InlineData(4, EntityType.Association)]
    public async Task ShouldBlockAsync_never_blocks_a_non_super_admin_role_regardless_of_configuration_state(int roleId, EntityType entityType)
    {
        var user = NonSuperAdmin(roleId, entityType);

        var gateWhileUnconfigured = BuildGate(new FakeUserRepository(user), isConfigured: false);
        var gateWhileConfigured = BuildGate(new FakeUserRepository(user), isConfigured: true);

        Assert.False(await gateWhileUnconfigured.ShouldBlockAsync(user.UserId));
        Assert.False(await gateWhileConfigured.ShouldBlockAsync(user.UserId));
    }

    [Fact]
    public async Task ShouldBlockAsync_returns_false_for_an_inactive_super_admin()
    {
        var gate = BuildGate(new FakeUserRepository(SuperAdmin(isActive: false)), isConfigured: false);

        var blocked = await gate.ShouldBlockAsync(1);

        Assert.False(blocked);
    }

    [Fact]
    public async Task ShouldBlockAsync_returns_false_when_the_caller_is_not_a_real_user()
    {
        var gate = BuildGate(new FakeUserRepository(SuperAdmin()), isConfigured: false);

        var blocked = await gate.ShouldBlockAsync(currentUserId: 999);

        Assert.False(blocked);
    }

    private static SystemConfigurationGateService BuildGate(IUserRepository users, bool isConfigured) =>
        new(new FakeUnitOfWork(users), new FakeSystemConfigurationService(isConfigured));

    private sealed class FakeSystemConfigurationService : ISystemConfigurationService
    {
        private readonly bool _isConfigured;

        public FakeSystemConfigurationService(bool isConfigured) => _isConfigured = isConfigured;

        public Task<bool> IsConfiguredAsync(CancellationToken cancellationToken = default) => Task.FromResult(_isConfigured);

        public Task<DTOs.SystemConfiguration.SystemConfigurationDto> GetAsync(int currentUserId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<DTOs.SystemConfiguration.SystemConfigurationDto> UpdateAsync(int currentUserId, DTOs.SystemConfiguration.UpdateSystemConfigurationDto dto, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly User _user;

        public FakeUserRepository(User user) => _user = user;

        public Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(userId == _user.UserId ? _user : null);

        public Task<User?> GetByEmailWithAccessAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<User>> GetByEntityAsync(EntityType entityType, int entityId, string? search, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<User>> GetDeletedByEntityAsync(EntityType entityType, int entityId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(List<User> Users, int TotalCount)> GetPagedByEntityAsync(EntityType entityType, int entityId, string? search, bool? isActive, int page, int pageSize, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetDetailsAsync(int userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetDetailsReadOnlyAsync(int userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByUserNameEnAsync(User user, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByUserNameArAsync(User user, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetWithPermissionsAsync(int userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<User>> GetByIdsInEntityAsync(IEnumerable<int> userIds, EntityType entityType, int entityId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddAsync(User user, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public void Remove(User user) => throw new NotSupportedException();

        public Task<bool> PhoneExistsAsync(string phoneNumber, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public FakeUnitOfWork(IUserRepository users) => Users = users;

        public IUserRepository Users { get; }
        public IServiceRepository Services => throw new NotSupportedException();
        public IRoleRepository Roles => throw new NotSupportedException();
        public IPermissionRepository Permissions => throw new NotSupportedException();
        public IGroupRepository Groups => throw new NotSupportedException();
        public IProjectTypeRepository ProjectTypes => throw new NotSupportedException();
        public ICountryRepository Countries => throw new NotSupportedException();
        public ICityRepository Cities => throw new NotSupportedException();
        public ICityLocationRepository CityLocations => throw new NotSupportedException();
        public IAssociationProjectSupervisorRepository AssociationProjectSupervisors => throw new NotSupportedException();
        public IAssociationRepository Associations => throw new NotSupportedException();
        public IProductionCompanyRepository ProductionCompanies => throw new NotSupportedException();
        public IProjectRepository Projects => throw new NotSupportedException();
        public IWorkerRepository Workers => throw new NotSupportedException();
        public IPasswordResetTokenRepository PasswordResetTokens => throw new NotSupportedException();
        public IRefreshTokenRepository RefreshTokens => throw new NotSupportedException();
        public ISystemConfigurationRepository SystemConfigurations => throw new NotSupportedException();



        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
        public Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default) => operation();
    }
}
