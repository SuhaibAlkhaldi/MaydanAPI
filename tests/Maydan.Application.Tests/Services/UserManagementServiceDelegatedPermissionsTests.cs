using Maydan.Application.DTOs.UserManagement;
using Maydan.Application.Interfaces;
using Maydan.Application.Services;
using Maydan.Domain.Entities;
using Maydan.Domain.Enums;

namespace Maydan.Application.Tests.Services;

// MAYD-37 (2026-09-24, product-owner-confirmed): "ASEZA Admin has all view permissions by default;
// a regular ASEZA user gets a custom subset assigned by the Admin" only works if the admin can
// actually hand a permission they hold individually (not via ASEZA's own RolePermissions — see
// RolePermissionSeedConfiguration.cs's own comment on why it can't be role-level) to a fellow ASEZA
// user through the existing, already-verified User Details "Edit Permissions" flow (MAYD-34). These
// tests prove GetCallerDelegatablePermissionIds's real effect: EnsurePermissionsAllowedForRole and
// GetAvailablePermissionsAsync both now additionally accept/offer a permission the CALLER personally
// holds, but ONLY for a same-role target — confirming this can never leak into the unrelated MAYD-1
// cross-role create path (Bayt-AlUrdon creating a different-role user).
public class UserManagementServiceDelegatedPermissionsTests
{
    private static Role BuildRole(int roleId, string nameEn) => new() { RoleId = roleId, RoleNameEn = nameEn, RoleNameAr = nameEn };

    private static Permission BuildPermission(int id, string nameEn, string module = "x") =>
        new() { PermissionId = id, PermissionNameEn = nameEn, PermissionNameAr = nameEn, Module = module, IsActive = true };

    private static User BuildUser(int userId, EntityType entityType, int entityId, Role role) => new()
    {
        UserId = userId,
        RoleId = role.RoleId,
        Role = role,
        EntityType = entityType,
        EntityId = entityId,
        IsActive = true,
        FirstNameEn = "Test", LastNameEn = "User", FirstNameAr = "اختبار", LastNameAr = "مستخدم",
        Email = $"user{userId}@example.org", PhoneNumber = "+962700000000"
    };

    [Fact]
    public async Task UpdateDirectPermissionsAsync_caller_can_delegate_a_permission_they_personally_hold_to_a_same_role_target()
    {
        var asezaRole = BuildRole(2, "ASEZA");
        var pageViewPermission = BuildPermission(9, "View Associations");

        var admin = BuildUser(1000, EntityType.Aseza, 1, asezaRole);
        admin.UserPermissions.Add(new UserPermission { UserId = 1000, PermissionId = 9, Permission = pageViewPermission, IsActive = true });

        var regularUser = BuildUser(1006, EntityType.Aseza, 1, asezaRole);

        var repository = new FakeUserRepository(admin, regularUser);
        var service = BuildService(repository, new FakeRoleRepository(asezaRole));

        // Not in ASEZA's own RolePermissions (empty here) — only assignable because the ADMIN
        // personally holds it and both are the same role.
        var result = await service.UpdateDirectPermissionsAsync(1000, 1006, new UpdateUserPermissionsDto { PermissionIds = new List<int> { 9 } });

        Assert.Contains(result.DirectPermissions, p => p.PermissionId == 9);
    }

    [Fact]
    public async Task UpdateDirectPermissionsAsync_still_rejects_a_permission_the_caller_does_not_personally_hold()
    {
        var asezaRole = BuildRole(2, "ASEZA");
        var admin = BuildUser(1000, EntityType.Aseza, 1, asezaRole); // holds nothing individually
        var regularUser = BuildUser(1006, EntityType.Aseza, 1, asezaRole);

        var repository = new FakeUserRepository(admin, regularUser);
        var service = BuildService(repository, new FakeRoleRepository(asezaRole));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateDirectPermissionsAsync(1000, 1006, new UpdateUserPermissionsDto { PermissionIds = new List<int> { 9 } }));

        Assert.Contains("not available for the selected role", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetAvailablePermissionsAsync_offers_the_callers_own_delegatable_permissions_for_a_same_role_target()
    {
        var asezaRole = BuildRole(2, "ASEZA");
        var pageViewPermission = BuildPermission(25, "View Projects", "Projects");

        var admin = BuildUser(1000, EntityType.Aseza, 1, asezaRole);
        admin.UserPermissions.Add(new UserPermission { UserId = 1000, PermissionId = 25, Permission = pageViewPermission, IsActive = true });

        var repository = new FakeUserRepository(admin, admin);
        var roleRepository = new FakeRoleRepository(asezaRole);
        var service = BuildService(repository, roleRepository);

        var result = await service.GetAvailablePermissionsAsync(1000, 2);

        Assert.Contains(result, p => p.PermissionId == 25);
    }

    [Fact]
    public async Task GetAvailablePermissionsAsync_does_NOT_leak_the_callers_own_permissions_into_a_DIFFERENT_roles_catalog()
    {
        // MAYD-1's own cross-role create path (Bayt-AlUrdon creating an Association user) must stay
        // completely unaffected — the delegation widening is same-role-only.
        var baytAlUrdonRole = BuildRole(1, "Bayt-AlUrdon");
        var associationRole = BuildRole(4, "Association");
        var baytAlUrdonOnlyPermission = BuildPermission(38, "Manage System Configuration");

        var caller = BuildUser(1, EntityType.BaytAlUrdon, 1, baytAlUrdonRole);
        caller.UserPermissions.Add(new UserPermission { UserId = 1, PermissionId = 38, Permission = baytAlUrdonOnlyPermission, IsActive = true });

        var repository = new FakeUserRepository(caller, caller);
        var roleRepository = new FakeRoleRepository(associationRole); // target role: Association, NOT the caller's own
        var service = BuildService(repository, roleRepository);

        var result = await service.GetAvailablePermissionsAsync(1, 4);

        Assert.DoesNotContain(result, p => p.PermissionId == 38);
    }

    private static UserManagementService BuildService(FakeUserRepository repository, FakeRoleRepository? roleRepository = null) =>
        new(
            new FakeUnitOfWork(repository, roleRepository ?? new FakeRoleRepository(), new FakePermissionRepository(
                BuildPermission(9, "View Associations"), BuildPermission(25, "View Projects", "Projects"), BuildPermission(38, "Manage System Configuration"))),
            new FakePasswordHasher());

    private sealed class FakePermissionRepository : IPermissionRepository
    {
        private readonly List<Permission> _permissions;
        public FakePermissionRepository(params Permission[] permissions) => _permissions = permissions.ToList();

        public Task<List<Permission>> GetByIdsAsync(IEnumerable<int> permissionIds, CancellationToken cancellationToken = default) =>
            Task.FromResult(_permissions.Where(p => permissionIds.Contains(p.PermissionId)).ToList());

        public Task<List<Permission>> GetAllAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Permission?> GetByIdAsync(int permissionId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<Permission>> GetByModuleAsync(string module, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddAsync(Permission permission, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public void Remove(Permission permission) => throw new NotSupportedException();
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string HashPassword(string password) => $"hashed:{password}";
        public bool VerifyPassword(string password, string passwordHash) => passwordHash == $"hashed:{password}";
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        private static readonly Dictionary<int, Permission> PermissionsById = new[]
        {
            BuildPermission(9, "View Associations"), BuildPermission(25, "View Projects", "Projects"), BuildPermission(38, "Manage System Configuration")
        }.ToDictionary(p => p.PermissionId);

        private readonly Dictionary<int, User> _usersById;

        public FakeUserRepository(User caller, User target)
        {
            _usersById = new Dictionary<int, User> { [caller.UserId] = caller };
            _usersById[target.UserId] = target;
        }

        // Real EF Core performs Permission navigation fixup automatically once both entities are
        // tracked in the same DbContext (SyncUserPermissions only ever sets UserPermission.PermissionId,
        // not .Permission — see its own definition) — this plain in-memory fake has no change
        // tracker, so it's done by hand here on every read, matching real runtime behavior, so
        // MapUserDetails has a non-null Permission to read.
        private User? FixUp(User? user)
        {
            if (user is null)
            {
                return null;
            }

            foreach (var up in user.UserPermissions)
            {
                if (up.Permission is null && PermissionsById.TryGetValue(up.PermissionId, out var permission))
                {
                    up.Permission = permission;
                }
            }

            return user;
        }

        public Task<User?> GetDetailsAsync(int userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(FixUp(_usersById.TryGetValue(userId, out var user) ? user : null));

        public Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_usersById.TryGetValue(userId, out var user) ? user : null);

        public Task<User?> GetByEmailWithAccessAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<User>> GetByEntityAsync(EntityType entityType, int entityId, string? search, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<User>> GetDeletedByEntityAsync(EntityType entityType, int entityId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetDetailsReadOnlyAsync(int userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(FixUp(_usersById.TryGetValue(userId, out var user) ? user : null));
        public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByUserNameEnAsync(User user, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByUserNameArAsync(User user, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetWithPermissionsAsync(int userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<User>> GetByIdsInEntityAsync(IEnumerable<int> userIds, EntityType entityType, int entityId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(List<User> Users, int TotalCount)> GetPagedByEntityAsync(EntityType entityType, int entityId, string? search, bool? isActive, int page, int pageSize, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddAsync(User user, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public void Remove(User user) => throw new NotSupportedException();

        public Task<bool> PhoneExistsAsync(string phone, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }

    private sealed class FakeRoleRepository : IRoleRepository
    {
        private readonly Dictionary<int, Role> _rolesById;

        public FakeRoleRepository(params Role[] roles) => _rolesById = roles.ToDictionary(r => r.RoleId);

        public Task<Role?> GetWithPermissionsAsync(int roleId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_rolesById.TryGetValue(roleId, out var role) ? role : null);

        public Task<Role?> GetByIdAsync(int roleId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<Role>> GetAllAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<Role>> GetAllWithPermissionsAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddAsync(Role role, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public void Remove(Role role) => throw new NotSupportedException();
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public FakeUnitOfWork(IUserRepository users, IRoleRepository roles, IPermissionRepository? permissions = null)
        {
            Users = users;
            Roles = roles;
            Permissions = permissions ?? new FakePermissionRepository();
        }

        public IUserRepository Users { get; }
        public IRoleRepository Roles { get; }
        public IPermissionRepository Permissions { get; }
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
