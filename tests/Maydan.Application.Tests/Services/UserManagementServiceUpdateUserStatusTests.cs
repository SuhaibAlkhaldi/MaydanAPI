using Maydan.Application.DTOs.UserManagement;
using Maydan.Application.Interfaces;
using Maydan.Application.Services;
using Maydan.Domain.Entities;
using Maydan.Domain.Enums;

namespace Maydan.Application.Tests.Services;

// Activate/Deactivate User (UserManagementModule.md Phase 4.3) — no MAYD subtask covers this
// directly (confirmed via search), and there was no test coverage at all before this pass since the
// capability didn't exist. Own dedicated fakes rather than reusing
// UserManagementServiceGroupsAndPermissionsTests.cs's — that file's FakeUserRepository only ever
// holds ONE user and throws NotSupportedException from GetWithPermissionsAsync, both wrong for this
// method (it needs a distinct caller + target, and GetWithPermissionsAsync is the one it actually
// calls) — same "each test file owns its own fakes" convention ProjectServiceTests/
// LocationServiceTests already established in this codebase.
public class UserManagementServiceUpdateUserStatusTests
{
    private const int ManageUsersPermissionId = 3;

    private static Role BuildRole(int roleId, string nameEn, params Permission[] rolePermissions)
    {
        var role = new Role { RoleId = roleId, RoleNameEn = nameEn, RoleNameAr = nameEn };
        foreach (var permission in rolePermissions)
        {
            role.RolePermissions.Add(new RolePermission { RoleId = roleId, Role = role, PermissionId = permission.PermissionId, Permission = permission, IsActive = true });
        }

        return role;
    }

    private static Permission BuildPermission(int id, string nameEn) =>
        new() { PermissionId = id, PermissionNameEn = nameEn, PermissionNameAr = nameEn, Module = "Users", IsActive = true };

    private static User BuildUser(int userId, EntityType entityType, int entityId, Role role, bool isActive = true) => new()
    {
        UserId = userId,
        FirstNameEn = $"First{userId}",
        LastNameEn = $"Last{userId}",
        FirstNameAr = $"اول{userId}",
        LastNameAr = $"اخير{userId}",
        RoleId = role.RoleId,
        Role = role,
        EntityType = entityType,
        EntityId = entityId,
        IsActive = isActive
    };

    [Fact]
    public async Task UpdateUserStatusAsync_CallerWithManageUsers_DeactivatesSameEntityUser()
    {
        var manageUsers = BuildPermission(ManageUsersPermissionId, "Manage Users");
        var caller = BuildUser(1, EntityType.BaytAlUrdon, 1, BuildRole(1, "Bayt-AlUrdon", manageUsers));
        var target = BuildUser(2, EntityType.BaytAlUrdon, 1, BuildRole(2, "Bayt-AlUrdon Employee"));

        var service = BuildService(caller, target);

        var result = await service.UpdateUserStatusAsync(caller.UserId, target.UserId, new UpdateUserStatusDto { IsActive = false });

        Assert.False(result.IsActive);
        Assert.False(target.IsActive);
    }

    [Fact]
    public async Task UpdateUserStatusAsync_CallerWithManageUsers_ReactivatesSameEntityUser()
    {
        var manageUsers = BuildPermission(ManageUsersPermissionId, "Manage Users");
        var caller = BuildUser(1, EntityType.BaytAlUrdon, 1, BuildRole(1, "Bayt-AlUrdon", manageUsers));
        var target = BuildUser(2, EntityType.BaytAlUrdon, 1, BuildRole(2, "Bayt-AlUrdon Employee"), isActive: false);

        var service = BuildService(caller, target);

        var result = await service.UpdateUserStatusAsync(caller.UserId, target.UserId, new UpdateUserStatusDto { IsActive = true });

        Assert.True(result.IsActive);
        Assert.True(target.IsActive);
    }

    // UserManagementModule.md Phase 5.4's "cannot modify your own permissions" rule, applied the
    // same way to account status — rejected before any repository lookup, same ordering as
    // UpdateDirectPermissionsAsync's own self-check.
    [Fact]
    public async Task UpdateUserStatusAsync_SelfDeactivation_ThrowsUnauthorizedAccessException()
    {
        var manageUsers = BuildPermission(ManageUsersPermissionId, "Manage Users");
        var caller = BuildUser(1, EntityType.BaytAlUrdon, 1, BuildRole(1, "Bayt-AlUrdon", manageUsers));

        var service = BuildService(caller);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.UpdateUserStatusAsync(caller.UserId, caller.UserId, new UpdateUserStatusDto { IsActive = false }));
    }

    // Same rule applies regardless of direction — a deactivated caller could never reach this
    // endpoint to reactivate themselves anyway, so there is no legitimate self-reactivation case to
    // carve out.
    [Fact]
    public async Task UpdateUserStatusAsync_SelfReactivation_ThrowsUnauthorizedAccessException()
    {
        var manageUsers = BuildPermission(ManageUsersPermissionId, "Manage Users");
        var caller = BuildUser(1, EntityType.BaytAlUrdon, 1, BuildRole(1, "Bayt-AlUrdon", manageUsers));

        var service = BuildService(caller);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.UpdateUserStatusAsync(caller.UserId, caller.UserId, new UpdateUserStatusDto { IsActive = true }));
    }

    // Strict same-entity-only, matching UpdateDirectPermissionsAsync/UpdateGroupsAsync's own
    // GetScopedUserAsync — deliberately NOT given the Bayt-AlUrdon/ASEZA cross-entity view override.
    [Fact]
    public async Task UpdateUserStatusAsync_CrossEntityTarget_ThrowsUnauthorizedAccessException()
    {
        var manageUsers = BuildPermission(ManageUsersPermissionId, "Manage Users");
        var caller = BuildUser(1, EntityType.Association, 5, BuildRole(4, "Association", manageUsers));
        var target = BuildUser(2, EntityType.BaytAlUrdon, 1, BuildRole(1, "Bayt-AlUrdon"));

        var service = BuildService(caller, target);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.UpdateUserStatusAsync(caller.UserId, target.UserId, new UpdateUserStatusDto { IsActive = false }));
    }

    [Fact]
    public async Task UpdateUserStatusAsync_CallerWithoutManageUsersPermission_ThrowsUnauthorizedAccessException()
    {
        var viewUsers = BuildPermission(1, "View Users");
        var caller = BuildUser(1, EntityType.BaytAlUrdon, 1, BuildRole(1, "Bayt-AlUrdon", viewUsers));
        var target = BuildUser(2, EntityType.BaytAlUrdon, 1, BuildRole(2, "Bayt-AlUrdon Employee"));

        var service = BuildService(caller, target);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.UpdateUserStatusAsync(caller.UserId, target.UserId, new UpdateUserStatusDto { IsActive = false }));
    }

    [Fact]
    public async Task UpdateUserStatusAsync_InactiveCaller_ThrowsUnauthorizedAccessException()
    {
        var manageUsers = BuildPermission(ManageUsersPermissionId, "Manage Users");
        var caller = BuildUser(1, EntityType.BaytAlUrdon, 1, BuildRole(1, "Bayt-AlUrdon", manageUsers), isActive: false);
        var target = BuildUser(2, EntityType.BaytAlUrdon, 1, BuildRole(2, "Bayt-AlUrdon Employee"));

        var service = BuildService(caller, target);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.UpdateUserStatusAsync(caller.UserId, target.UserId, new UpdateUserStatusDto { IsActive = false }));
    }

    [Fact]
    public async Task UpdateUserStatusAsync_UnknownTargetUser_ThrowsKeyNotFoundException()
    {
        var manageUsers = BuildPermission(ManageUsersPermissionId, "Manage Users");
        var caller = BuildUser(1, EntityType.BaytAlUrdon, 1, BuildRole(1, "Bayt-AlUrdon", manageUsers));

        var service = BuildService(caller);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.UpdateUserStatusAsync(caller.UserId, 999, new UpdateUserStatusDto { IsActive = false }));
    }

    private static UserManagementService BuildService(User caller, params User[] otherUsers)
    {
        var users = new List<User> { caller };
        users.AddRange(otherUsers);

        var unitOfWork = new FakeUnitOfWork(new FakeUserRepository(users));
        return new UserManagementService(unitOfWork, new FakePasswordHasher());
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string HashPassword(string password) => $"hashed:{password}";
        public bool VerifyPassword(string password, string passwordHash) => passwordHash == $"hashed:{password}";
    }

    // No real navigation-loading distinction between GetByIdAsync/GetDetailsAsync/
    // GetWithPermissionsAsync in this fake — every registered User already carries whatever
    // Role.RolePermissions/UserPermissions/UserGroups the test needs, same simplification
    // UserManagementServiceGroupsAndPermissionsTests.FakeUserRepository's own comment notes.
    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly Dictionary<int, User> _usersById;

        public FakeUserRepository(IEnumerable<User> users) => _usersById = users.ToDictionary(u => u.UserId);

        public Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_usersById.GetValueOrDefault(userId));

        public Task<User?> GetDetailsAsync(int userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_usersById.GetValueOrDefault(userId));

        public Task<User?> GetWithPermissionsAsync(int userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_usersById.GetValueOrDefault(userId));

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

        public Task<bool> PhoneExistsAsync(string phone, CancellationToken cancellationToken = default)
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


        public IWorkerRepositoryV2 WorkersV2 => throw new NotImplementedException();

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
        public Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default) => operation();
    }
}
