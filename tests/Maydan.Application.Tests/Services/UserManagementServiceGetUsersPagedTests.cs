using Maydan.Application.Interfaces;
using Maydan.Application.Services;
using Maydan.Domain.Entities;
using Maydan.Domain.Enums;

namespace Maydan.Application.Tests.Services;

// MAYD-20: the real Users List page's backend. Proves the actual Business Rule (MAYD-1) — Bayt-
// AlUrdon and ASEZA may view a DIFFERENT entity's users; every other role may only ever see its
// own — plus real search/status/pagination behavior, and that this reuses the real ViewUsers/
// ManageUsers permission gate (PermissionSeedConfiguration.cs ids 1/3), not an ad hoc role check.
public class UserManagementServiceGetUsersPagedTests
{
    private static Role RoleWithViewUsers(int roleId, string nameEn) => BuildRole(roleId, nameEn, withViewUsers: true);
    private static Role RoleWithoutViewUsers(int roleId, string nameEn) => BuildRole(roleId, nameEn, withViewUsers: false);

    private static Role BuildRole(int roleId, string nameEn, bool withViewUsers)
    {
        var role = new Role { RoleId = roleId, RoleNameEn = nameEn, RoleNameAr = nameEn };
        var permissionId = withViewUsers ? 1 : 99;
        var permission = new Permission { PermissionId = permissionId, PermissionNameEn = withViewUsers ? "View Users" : "Unrelated", Module = "x", IsActive = true };
        role.RolePermissions.Add(new RolePermission { RoleId = roleId, Role = role, PermissionId = permissionId, Permission = permission, IsActive = true });
        return role;
    }

    private static User BuildUser(int userId, EntityType entityType, int entityId, Role role) => new()
    {
        UserId = userId,
        RoleId = role.RoleId,
        Role = role,
        EntityType = entityType,
        EntityId = entityId,
        IsActive = true
    };

    [Fact]
    public async Task SuperAdmin_can_view_a_different_entitys_users_via_explicit_override()
    {
        var superAdmin = BuildUser(1, EntityType.BaytAlUrdon, 1, RoleWithViewUsers(1, "Bayt-AlUrdon"));
        var repository = new FakeUserRepository(superAdmin);
        var service = BuildService(repository);

        var result = await service.GetUsersPagedAsync(1, null, null, 1, 10, EntityType.Association, 42);

        Assert.Equal(EntityType.Association, repository.LastQueriedEntityType);
        Assert.Equal(42, repository.LastQueriedEntityId);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Aseza_can_view_its_own_entity_by_default_and_a_different_entity_via_override()
    {
        var asezaAdmin = BuildUser(2, EntityType.Aseza, 1, RoleWithViewUsers(2, "ASEZA"));
        var repository = new FakeUserRepository(asezaAdmin);
        var service = BuildService(repository);

        await service.GetUsersPagedAsync(2, null, null, 1, 10, null, null);
        Assert.Equal(EntityType.Aseza, repository.LastQueriedEntityType);
        Assert.Equal(1, repository.LastQueriedEntityId);

        await service.GetUsersPagedAsync(2, null, null, 1, 10, EntityType.ProductionCompany, 7);
        Assert.Equal(EntityType.ProductionCompany, repository.LastQueriedEntityType);
        Assert.Equal(7, repository.LastQueriedEntityId);
    }

    [Theory]
    [InlineData("Association")]
    [InlineData("ProductionCompany")]
    public async Task EntityAdmin_sees_only_their_own_entity_and_is_rejected_for_any_other(string entityTypeName)
    {
        var entityType = Enum.Parse<EntityType>(entityTypeName);
        var roleId = entityType == EntityType.Association ? 4 : 3;
        var admin = BuildUser(3, entityType, 5, RoleWithViewUsers(roleId, entityTypeName));
        var repository = new FakeUserRepository(admin);
        var service = BuildService(repository);

        // No override -> own entity, succeeds.
        await service.GetUsersPagedAsync(3, null, null, 1, 10, null, null);
        Assert.Equal(entityType, repository.LastQueriedEntityType);
        Assert.Equal(5, repository.LastQueriedEntityId);

        // Explicitly requesting their OWN entity is fine too.
        await service.GetUsersPagedAsync(3, null, null, 1, 10, entityType, 5);

        // Any other entity is rejected outright.
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.GetUsersPagedAsync(3, null, null, 1, 10, EntityType.Association, 999));
    }

    [Fact]
    public async Task Caller_without_ViewUsers_or_ManageUsers_is_rejected()
    {
        var noAccess = BuildUser(4, EntityType.Association, 5, RoleWithoutViewUsers(4, "Association"));
        var repository = new FakeUserRepository(noAccess);
        var service = BuildService(repository);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.GetUsersPagedAsync(4, null, null, 1, 10, null, null));
    }

    [Fact]
    public async Task Providing_only_one_of_entityType_or_entityId_is_rejected()
    {
        var superAdmin = BuildUser(1, EntityType.BaytAlUrdon, 1, RoleWithViewUsers(1, "Bayt-AlUrdon"));
        var repository = new FakeUserRepository(superAdmin);
        var service = BuildService(repository);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetUsersPagedAsync(1, null, null, 1, 10, EntityType.Association, null));
    }

    [Theory]
    [InlineData("john", "john.doe@example.org")]
    [InlineData("Smith", "jane.smith@example.org")]
    public async Task Search_matches_first_or_last_name(string term, string expectedEmail)
    {
        var superAdmin = BuildUser(1, EntityType.BaytAlUrdon, 1, RoleWithViewUsers(1, "Bayt-AlUrdon"));
        var repository = new FakeUserRepository(superAdmin);
        repository.SeedRealUsers(
            new SeedUser("John", "Doe", "john.doe@example.org", "+962700000010", true),
            new SeedUser("Jane", "Smith", "jane.smith@example.org", "+962700000011", true));
        var service = BuildService(repository);

        var result = await service.GetUsersPagedAsync(1, term, null, 1, 10, EntityType.BaytAlUrdon, 1);

        Assert.Single(result.Data!.Items);
        Assert.Equal(expectedEmail, result.Data!.Items[0].Email);
    }

    [Fact]
    public async Task Search_matches_email()
    {
        var superAdmin = BuildUser(1, EntityType.BaytAlUrdon, 1, RoleWithViewUsers(1, "Bayt-AlUrdon"));
        var repository = new FakeUserRepository(superAdmin);
        repository.SeedRealUsers(
            new SeedUser("John", "Doe", "john.doe@example.org", "+962700000010", true),
            new SeedUser("Jane", "Smith", "jane.smith@example.org", "+962700000011", true));
        var service = BuildService(repository);

        var result = await service.GetUsersPagedAsync(1, "jane.smith", null, 1, 10, EntityType.BaytAlUrdon, 1);

        Assert.Single(result.Data!.Items);
        Assert.Equal("jane.smith@example.org", result.Data!.Items[0].Email);
    }

    [Fact]
    public async Task Search_matches_mobile_number()
    {
        var superAdmin = BuildUser(1, EntityType.BaytAlUrdon, 1, RoleWithViewUsers(1, "Bayt-AlUrdon"));
        var repository = new FakeUserRepository(superAdmin);
        repository.SeedRealUsers(
            new SeedUser("John", "Doe", "john.doe@example.org", "+962700000010", true),
            new SeedUser("Jane", "Smith", "jane.smith@example.org", "+962700000011", true));
        var service = BuildService(repository);

        var result = await service.GetUsersPagedAsync(1, "0000011", null, 1, 10, EntityType.BaytAlUrdon, 1);

        Assert.Single(result.Data!.Items);
        Assert.Equal("jane.smith@example.org", result.Data!.Items[0].Email);
    }

    [Fact]
    public async Task Status_filter_returns_only_matching_users()
    {
        var superAdmin = BuildUser(1, EntityType.BaytAlUrdon, 1, RoleWithViewUsers(1, "Bayt-AlUrdon"));
        var repository = new FakeUserRepository(superAdmin);
        repository.SeedRealUsers(
            new SeedUser("Active", "One", "active@example.org", "+962700000020", true),
            new SeedUser("Inactive", "One", "inactive@example.org", "+962700000021", false));
        var service = BuildService(repository);

        var activeOnly = await service.GetUsersPagedAsync(1, null, true, 1, 10, EntityType.BaytAlUrdon, 1);
        var inactiveOnly = await service.GetUsersPagedAsync(1, null, false, 1, 10, EntityType.BaytAlUrdon, 1);
        var everyone = await service.GetUsersPagedAsync(1, null, null, 1, 10, EntityType.BaytAlUrdon, 1);

        Assert.Single(activeOnly.Data!.Items);
        Assert.Equal("active@example.org", activeOnly.Data!.Items[0].Email);
        Assert.Single(inactiveOnly.Data!.Items);
        Assert.Equal("inactive@example.org", inactiveOnly.Data!.Items[0].Email);
        Assert.Equal(2, everyone.Data!.Items.Count);
    }

    [Fact]
    public async Task Pagination_returns_the_right_page_and_a_real_total_count()
    {
        var superAdmin = BuildUser(1, EntityType.BaytAlUrdon, 1, RoleWithViewUsers(1, "Bayt-AlUrdon"));
        var repository = new FakeUserRepository(superAdmin);
        repository.SeedRealUsers(Enumerable.Range(1, 25)
            .Select(i => new SeedUser($"User{i:D2}", "Test", $"user{i:D2}@example.org", $"+96270000{i:D4}", true))
            .ToArray());
        var service = BuildService(repository);

        var page1 = await service.GetUsersPagedAsync(1, null, null, 1, 10, EntityType.BaytAlUrdon, 1);
        var page3 = await service.GetUsersPagedAsync(1, null, null, 3, 10, EntityType.BaytAlUrdon, 1);

        Assert.Equal(25, page1.Data!.TotalCount);
        Assert.Equal(10, page1.Data!.Items.Count);
        Assert.Equal(1, page1.Data!.Page);
        Assert.Equal(25, page3.Data!.TotalCount);
        Assert.Equal(5, page3.Data!.Items.Count); // last partial page
        Assert.Equal(3, page3.Data!.Page);
    }

    [Fact]
    public async Task Invalid_page_and_pageSize_are_clamped_to_safe_defaults()
    {
        var superAdmin = BuildUser(1, EntityType.BaytAlUrdon, 1, RoleWithViewUsers(1, "Bayt-AlUrdon"));
        var repository = new FakeUserRepository(superAdmin);
        var service = BuildService(repository);

        var result = await service.GetUsersPagedAsync(1, null, null, 0, 0, null, null);

        Assert.Equal(1, result.Data!.Page);
        Assert.Equal(10, result.Data!.PageSize);

        var result2 = await service.GetUsersPagedAsync(1, null, null, -5, 500, null, null);
        Assert.Equal(1, result2.Data!.Page);
        Assert.Equal(10, result2.Data!.PageSize);
    }

    private static UserManagementService BuildService(FakeUserRepository repository) =>
        new(new FakeUnitOfWork(repository), new FakePasswordHasher());

    private sealed record SeedUser(string FirstNameEn, string LastNameEn, string Email, string PhoneNumber, bool IsActive);

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string HashPassword(string password) => $"hashed:{password}";
        public bool VerifyPassword(string password, string passwordHash) => passwordHash == $"hashed:{password}";
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly User _currentUser;
        private List<User> _seededUsers = new();

        public FakeUserRepository(User currentUser) => _currentUser = currentUser;

        public EntityType? LastQueriedEntityType { get; private set; }
        public int? LastQueriedEntityId { get; private set; }

        public void SeedRealUsers(params SeedUser[] users)
        {
            var role = _currentUser.Role;
            _seededUsers = users.Select((u, index) => new User
            {
                UserId = 100 + index,
                RoleId = role.RoleId,
                Role = role,
                EntityType = _currentUser.EntityType,
                EntityId = _currentUser.EntityId,
                FirstNameEn = u.FirstNameEn,
                LastNameEn = u.LastNameEn,
                FirstNameAr = u.FirstNameEn,
                LastNameAr = u.LastNameEn,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                IsActive = u.IsActive
            }).ToList();
        }

        public Task<User?> GetWithPermissionsAsync(int userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(userId == _currentUser.UserId ? _currentUser : null);

        public Task<(List<User> Users, int TotalCount)> GetPagedByEntityAsync(
            EntityType entityType, int entityId, string? search, bool? isActive, int page, int pageSize,
            CancellationToken cancellationToken = default)
        {
            LastQueriedEntityType = entityType;
            LastQueriedEntityId = entityId;

            // Real filtering logic, same semantics as the real EF query (UserRepository.
            // GetPagedByEntityAsync) — proves the SERVICE's own page/search/status wiring, not a
            // re-test of EF's Where/Skip/Take (that's the repository's own concern).
            var query = _seededUsers.Where(u => u.EntityType == entityType && u.EntityId == entityId);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(u =>
                    u.FirstNameEn.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    u.LastNameEn.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    u.Email.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    u.PhoneNumber.Contains(term, StringComparison.OrdinalIgnoreCase));
            }

            if (isActive.HasValue)
            {
                query = query.Where(u => u.IsActive == isActive.Value);
            }

            var list = query.OrderBy(u => u.FirstNameEn).ToList();
            var totalCount = list.Count;
            var pageItems = list.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            return Task.FromResult((pageItems, totalCount));
        }

        public Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByEmailWithAccessAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<User>> GetByEntityAsync(EntityType entityType, int entityId, string? search, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<User>> GetDeletedByEntityAsync(EntityType entityType, int entityId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetDetailsAsync(int userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetDetailsReadOnlyAsync(int userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByUserNameEnAsync(User user, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByUserNameArAsync(User user, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<User>> GetByIdsInEntityAsync(IEnumerable<int> userIds, EntityType entityType, int entityId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddAsync(User user, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public void Remove(User user) => throw new NotSupportedException();

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

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public FakeUnitOfWork(IUserRepository users) => Users = users;

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
        public IServiceRepository Services => throw new NotSupportedException();

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
        public Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default) => operation();
    }
}
