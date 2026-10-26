using Maydan.Application.Interfaces;
using Maydan.Application.Services;
using Maydan.Domain.Entities;
using Maydan.Domain.Enums;

namespace Maydan.Application.Tests.Services;

// MAYD-36 (entity-scoping verification pass, 2026-09-24): a real scoping inconsistency the
// systematic sweep caught that the piecemeal MAYD-20/MAYD-25 passes each missed by only testing
// their own endpoint. GetUsersPagedAsync (Users List) already lets Bayt-AlUrdon/ASEZA view a
// DIFFERENT entity's users (see UserManagementServiceGetUsersPagedTests.cs); GetUserDetailsAsync
// (a single user's own Details page — what that list's own rows link to) did NOT share the same
// override, confirmed live via a real 403 on a user the List had just successfully returned. Fixed
// by routing GetUserDetailsAsync through the new GetViewableUserAsync (read-only override), while
// UpdateDirectPermissionsAsync/UpdateGroupsAsync deliberately keep the strict, no-override
// GetScopedUserAsync — see both methods' own comments in UserManagementService.cs for why.
public class UserManagementServiceGetUserDetailsTests
{
    private static Role BuildRole(int roleId, string nameEn) => new() { RoleId = roleId, RoleNameEn = nameEn, RoleNameAr = nameEn };

    private static User BuildUser(int userId, EntityType entityType, int entityId, Role role) => new()
    {
        UserId = userId,
        RoleId = role.RoleId,
        Role = role,
        EntityType = entityType,
        EntityId = entityId,
        IsActive = true,
        FirstNameEn = "Test",
        LastNameEn = "User",
        FirstNameAr = "اختبار",
        LastNameAr = "مستخدم",
        Email = $"user{userId}@example.org",
        PhoneNumber = "+962700000000"
    };

    [Theory]
    [InlineData(1, "BaytAlUrdon")] // Super Admin
    [InlineData(2, "Aseza")]
    public async Task BaytAlUrdon_and_Aseza_can_view_a_different_entitys_user_details(int callerRoleId, string callerEntityTypeName)
    {
        var callerEntityType = Enum.Parse<EntityType>(callerEntityTypeName);
        var caller = BuildUser(1, callerEntityType, 1, BuildRole(callerRoleId, callerEntityTypeName));
        var target = BuildUser(2, EntityType.ProductionCompany, 7, BuildRole(3, "ProductionHouse"));
        var repository = new FakeUserRepository(caller, target);
        var service = BuildService(repository);

        var result = await service.GetUserDetailsAsync(1, 2);

        Assert.Equal(2, result.Data!.UserId);
    }

    [Theory]
    [InlineData("ProductionCompany", 3)]
    [InlineData("Association", 4)]
    public async Task EntityAdmin_is_rejected_viewing_another_entitys_user_details(string callerEntityTypeName, int callerRoleId)
    {
        var callerEntityType = Enum.Parse<EntityType>(callerEntityTypeName);
        var caller = BuildUser(1, callerEntityType, 1, BuildRole(callerRoleId, callerEntityTypeName));
        var target = BuildUser(2, EntityType.BaytAlUrdon, 1, BuildRole(1, "Bayt-AlUrdon"));
        var repository = new FakeUserRepository(caller, target);
        var service = BuildService(repository);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetUserDetailsAsync(1, 2));
    }

    [Fact]
    public async Task BaytAlUrdon_cross_entity_view_does_NOT_extend_to_editing_that_users_permissions()
    {
        // The read-only override (GetViewableUserAsync) must not leak into the WRITE path
        // (GetScopedUserAsync, still strict) — see UserManagementService.cs's own comment on why.
        var caller = BuildUser(1, EntityType.BaytAlUrdon, 1, BuildRole(1, "Bayt-AlUrdon"));
        var target = BuildUser(2, EntityType.ProductionCompany, 7, BuildRole(3, "ProductionHouse"));
        var repository = new FakeUserRepository(caller, target);
        var service = BuildService(repository);

        // View still works...
        await service.GetUserDetailsAsync(1, 2);

        // ...but editing the same user's direct permissions is still rejected.
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.UpdateDirectPermissionsAsync(1, 2, new Maydan.Application.DTOs.UserManagement.UpdateUserPermissionsDto { PermissionIds = new List<int>() }));
    }

    private static UserManagementService BuildService(FakeUserRepository repository) =>
        new(new FakeUnitOfWork(repository), new FakePasswordHasher());

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string HashPassword(string password) => $"hashed:{password}";
        public bool VerifyPassword(string password, string passwordHash) => passwordHash == $"hashed:{password}";
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly User _currentUser;
        private readonly User _target;

        public FakeUserRepository(User currentUser, User target)
        {
            _currentUser = currentUser;
            _target = target;
        }
        public Task<List<User>> GetAssociationSupervisorsAsync(EntityType entityType, int? entityId = null, CancellationToken cancellationToken = default)
    => Task.FromResult(new List<User>());
        public Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(userId == _currentUser.UserId ? _currentUser : null);

        public Task<User?> GetDetailsAsync(int userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(userId == _target.UserId ? _target : userId == _currentUser.UserId ? _currentUser : null);

        public Task<User?> GetWithPermissionsAsync(int userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByEmailWithAccessAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<User>> GetByEntityAsync(EntityType entityType, int entityId, string? search, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<User>> GetDeletedByEntityAsync(EntityType entityType, int entityId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetDetailsReadOnlyAsync(int userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByUserNameEnAsync(User user, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByUserNameArAsync(User user, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<User>> GetByIdsInEntityAsync(IEnumerable<int> userIds, EntityType entityType, int entityId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(List<User> Users, int TotalCount)> GetPagedByEntityAsync(EntityType entityType, int entityId, string? search, bool? isActive, int page, int pageSize, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddAsync(User user, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public void Remove(User user) => throw new NotSupportedException();
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public FakeUnitOfWork(IUserRepository users) => Users = users;

        // Services not used in these tests; satisfy interface by throwing when accessed.
        public IServiceRepository Services => throw new NotSupportedException();

        public IUserRepository Users { get; }
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
        // duplicate property removed

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
        public Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default) => operation();
    }
}
