using Maydan.Application.DTOs.Onboarding;
using Maydan.Application.Interfaces;
using Maydan.Application.Services;
using Maydan.Domain.Entities;
using Maydan.Domain.Enums;

namespace Maydan.Application.Tests.Services;

// Entity onboarding Stage 2 (2026-09-22): proves EntityOnboardingService.OnboardAssociationAdminAsync
// enforces both halves of "caller has permission 37 AND is Bayt-AlUrdon" independently (neither
// check alone is enough), rejects a target Association that already has an admin or doesn't exist,
// and — on the happy path — grants exactly the Association role's full active permission set with
// MustResetPassword forced true (the opposite of Stage 1's self-registration reasoning).
public class EntityOnboardingServiceTests
{
    private static OnboardAssociationAdminDto ValidDto() => new()
    {
        FirstNameEn = "New",
        LastNameEn = "Admin",
        FirstNameAr = "جديد",
        LastNameAr = "مدير",
        Email = "new.admin@example.org",
        PhoneNumber = "+962700000000",
        InitialPassword = "P@ssw0rd!"
    };

    private static Role BaytAlUrdonRole(bool withOnboardPermission)
    {
        var role = new Role { RoleId = 1, RoleNameEn = "Bayt-AlUrdon", RoleNameAr = "بيت الأردن" };

        if (withOnboardPermission)
        {
            var onboardPermission = new Permission { PermissionId = 37, PermissionNameEn = "Onboard Entities", Module = "Onboarding", IsActive = true };
            role.RolePermissions.Add(new RolePermission { RoleId = 1, Role = role, PermissionId = 37, Permission = onboardPermission, IsActive = true });
        }
        else
        {
            var unrelatedPermission = new Permission { PermissionId = 1, PermissionNameEn = "View Users", Module = "Users", IsActive = true };
            role.RolePermissions.Add(new RolePermission { RoleId = 1, Role = role, PermissionId = 1, Permission = unrelatedPermission, IsActive = true });
        }

        return role;
    }

    private static Role AssociationRoleWithPermissions()
    {
        var role = new Role { RoleId = 4, RoleNameEn = "Association", RoleNameAr = "الجمعية" };

        var viewAssociations = new Permission { PermissionId = 9, PermissionNameEn = "View Associations", Module = "Associations", IsActive = true };
        var manageAttendance = new Permission { PermissionId = 34, PermissionNameEn = "Manage Attendance", Module = "Attendance", IsActive = true };
        var inactivePermission = new Permission { PermissionId = 99, PermissionNameEn = "Retired Permission", Module = "Legacy", IsActive = false };

        role.RolePermissions.Add(new RolePermission { RoleId = 4, Role = role, PermissionId = viewAssociations.PermissionId, Permission = viewAssociations, IsActive = true });
        role.RolePermissions.Add(new RolePermission { RoleId = 4, Role = role, PermissionId = manageAttendance.PermissionId, Permission = manageAttendance, IsActive = true });
        role.RolePermissions.Add(new RolePermission { RoleId = 4, Role = role, PermissionId = inactivePermission.PermissionId, Permission = inactivePermission, IsActive = false });

        return role;
    }

    private static Association TargetAssociation() => new() { Id = 501, EnglishName = "Amman Association", ArabicName = "جمعية عمان", CityId = 1 };

    [Fact]
    public async Task OnboardAssociationAdminAsync_HappyPath_GrantsFullAssociationPermissionSetAndForcesReset()
    {
        var callerRole = BaytAlUrdonRole(withOnboardPermission: true);
        var caller = new User { UserId = 1, RoleId = callerRole.RoleId, Role = callerRole, EntityType = EntityType.BaytAlUrdon, EntityId = 1, IsActive = true };
        var association = TargetAssociation();
        var associationRole = AssociationRoleWithPermissions();

        var userRepository = new FakeUserRepository(caller, existingEntityUsers: new List<User>(), roleForNewUsers: associationRole);
        var unitOfWork = new FakeUnitOfWork(userRepository, new FakeAssociationRepository(association), new FakeRoleRepository(associationRole));
        var service = new EntityOnboardingService(unitOfWork, new FakePasswordHasher());

        var result = await service.OnboardAssociationAdminAsync(caller.UserId, association.Id, ValidDto());

        Assert.NotNull(userRepository.AddedUser);
        var user = userRepository.AddedUser!;
        Assert.Equal(EntityType.Association, user.EntityType);
        Assert.Equal(association.Id, user.EntityId);
        Assert.Equal(4, user.RoleId);
        Assert.True(user.MustResetPassword);
        Assert.True(user.IsActive);

        var grantedPermissionIds = user.UserPermissions.Select(up => up.PermissionId).OrderBy(id => id).ToList();
        Assert.Equal(new[] { 9, 34 }, grantedPermissionIds);

        Assert.Equal(EntityType.Association, result.EntityType);
        Assert.Equal(association.Id, result.EntityId);
        Assert.True(result.MustResetPassword);
    }

    [Fact]
    public async Task OnboardAssociationAdminAsync_CallerNotBaytAlUrdon_ThrowsEvenWithDirectPermission37()
    {
        var associationRole = new Role { RoleId = 4, RoleNameEn = "Association", RoleNameAr = "الجمعية" };
        var onboardPermission = new Permission { PermissionId = 37, PermissionNameEn = "Onboard Entities", Module = "Onboarding", IsActive = true };
        // Direct UserPermission grant, not a role grant — proves the explicit EntityType check is
        // a real, independent guard and not just a side effect of the permission catalog.
        var caller = new User { UserId = 2, RoleId = associationRole.RoleId, Role = associationRole, EntityType = EntityType.Association, EntityId = 7, IsActive = true };
        caller.UserPermissions.Add(new UserPermission { UserId = 2, PermissionId = 37, Permission = onboardPermission, IsActive = true });

        var association = TargetAssociation();
        var userRepository = new FakeUserRepository(caller, existingEntityUsers: new List<User>());
        var unitOfWork = new FakeUnitOfWork(userRepository, new FakeAssociationRepository(association), new FakeRoleRepository(AssociationRoleWithPermissions()));
        var service = new EntityOnboardingService(unitOfWork, new FakePasswordHasher());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.OnboardAssociationAdminAsync(caller.UserId, association.Id, ValidDto()));

        Assert.Null(userRepository.AddedUser);
    }

    [Fact]
    public async Task OnboardAssociationAdminAsync_BaytAlUrdonWithoutPermission37_Throws()
    {
        var callerRole = BaytAlUrdonRole(withOnboardPermission: false);
        var caller = new User { UserId = 3, RoleId = callerRole.RoleId, Role = callerRole, EntityType = EntityType.BaytAlUrdon, EntityId = 1, IsActive = true };
        var association = TargetAssociation();

        var userRepository = new FakeUserRepository(caller, existingEntityUsers: new List<User>());
        var unitOfWork = new FakeUnitOfWork(userRepository, new FakeAssociationRepository(association), new FakeRoleRepository(AssociationRoleWithPermissions()));
        var service = new EntityOnboardingService(unitOfWork, new FakePasswordHasher());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.OnboardAssociationAdminAsync(caller.UserId, association.Id, ValidDto()));

        Assert.Null(userRepository.AddedUser);
    }

    [Fact]
    public async Task OnboardAssociationAdminAsync_AssociationAlreadyHasAdmin_Throws()
    {
        var callerRole = BaytAlUrdonRole(withOnboardPermission: true);
        var caller = new User { UserId = 4, RoleId = callerRole.RoleId, Role = callerRole, EntityType = EntityType.BaytAlUrdon, EntityId = 1, IsActive = true };
        var association = TargetAssociation();
        var existingAdmin = new User { UserId = 999, EntityType = EntityType.Association, EntityId = association.Id, IsActive = true };

        var userRepository = new FakeUserRepository(caller, existingEntityUsers: new List<User> { existingAdmin });
        var unitOfWork = new FakeUnitOfWork(userRepository, new FakeAssociationRepository(association), new FakeRoleRepository(AssociationRoleWithPermissions()));
        var service = new EntityOnboardingService(unitOfWork, new FakePasswordHasher());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.OnboardAssociationAdminAsync(caller.UserId, association.Id, ValidDto()));

        Assert.Contains("already has an admin", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Null(userRepository.AddedUser);
    }

    [Fact]
    public async Task OnboardAssociationAdminAsync_AssociationNotFound_Throws()
    {
        var callerRole = BaytAlUrdonRole(withOnboardPermission: true);
        var caller = new User { UserId = 5, RoleId = callerRole.RoleId, Role = callerRole, EntityType = EntityType.BaytAlUrdon, EntityId = 1, IsActive = true };

        var userRepository = new FakeUserRepository(caller, existingEntityUsers: new List<User>());
        var unitOfWork = new FakeUnitOfWork(userRepository, new FakeAssociationRepository(), new FakeRoleRepository(AssociationRoleWithPermissions()));
        var service = new EntityOnboardingService(unitOfWork, new FakePasswordHasher());

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.OnboardAssociationAdminAsync(caller.UserId, 999999, ValidDto()));

        Assert.Null(userRepository.AddedUser);
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly Dictionary<int, User> _usersById;
        private readonly List<User> _existingEntityUsers;
        private readonly bool _emailExists;
        private readonly Role? _roleForNewUsers;

        public FakeUserRepository(User currentUser, List<User> existingEntityUsers, bool emailExists = false, Role? roleForNewUsers = null)
        {
            _usersById = new Dictionary<int, User> { [currentUser.UserId] = currentUser };
            _existingEntityUsers = existingEntityUsers;
            _emailExists = emailExists;
            _roleForNewUsers = roleForNewUsers;
        }

        public User? AddedUser { get; private set; }

        public Task<User?> GetWithPermissionsAsync(int userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_usersById.TryGetValue(userId, out var user) ? user : null);

        public Task<List<User>> GetByEntityAsync(EntityType entityType, int entityId, string? search, CancellationToken cancellationToken = default) =>
            Task.FromResult(_existingEntityUsers);

        public Task<List<User>> GetDeletedByEntityAsync(EntityType entityType, int entityId, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<(List<User> Users, int TotalCount)> GetPagedByEntityAsync(EntityType entityType, int entityId, string? search, bool? isActive, int page, int pageSize, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default) =>
            Task.FromResult(_emailExists);

        public Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            user.UserId = 300;

            if (_roleForNewUsers is not null)
            {
                user.Role = _roleForNewUsers;

                // A real EF re-fetch (GetDetailsReadOnlyAsync's .Include(...).ThenInclude(Permission))
                // populates each UserPermission's Permission navigation; this fake must do the same
                // by hand so MapUserDetails doesn't see a null Permission.
                var permissionsById = _roleForNewUsers.RolePermissions.ToDictionary(rp => rp.PermissionId, rp => rp.Permission);
                foreach (var userPermission in user.UserPermissions)
                {
                    userPermission.Permission = permissionsById[userPermission.PermissionId];
                }
            }

            AddedUser = user;
            _usersById[user.UserId] = user;
            return Task.CompletedTask;
        }

        public Task<User?> GetDetailsReadOnlyAsync(int userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_usersById.TryGetValue(userId, out var user) ? user : null);

        public Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByEmailWithAccessAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetDetailsAsync(int userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByUserNameEnAsync(User user, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByUserNameArAsync(User user, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<User>> GetByIdsInEntityAsync(IEnumerable<int> userIds, EntityType entityType, int entityId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public void Remove(User user) => throw new NotSupportedException();
    }

    private sealed class FakeAssociationRepository : IAssociationRepository
    {
        private readonly Dictionary<int, Association> _associationsById;

        public FakeAssociationRepository(params Association[] associations) =>
            _associationsById = associations.ToDictionary(a => a.Id);

        public Task<Association?> GetByIdAsync(int associationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_associationsById.TryGetValue(associationId, out var association) ? association : null);

        public Task<List<Association>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(_associationsById.Values.ToList());

        public Task<Association?> GetByCityIdAsync(int cityId, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task AddAsync(Association association, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public void Remove(Association association) => throw new NotSupportedException();
        public Task<Association?> GetByIdIncludingDeletedAsync(int associationId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Association?> GetByIdWithWorkersAsync(int associationId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(Association Association, int WorkersCount)?> GetByIdWithWorkersCountAsync(int associationId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<(Association Association, int WorkersCount)>> QueryAsync(bool isDeleted, string? searchTerm = null, bool? orderByWorkersCountAscending = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class FakeRoleRepository : IRoleRepository
    {
        private readonly Role _role;

        public FakeRoleRepository(Role role) => _role = role;

        public Task<Role?> GetWithPermissionsAsync(int roleId, CancellationToken cancellationToken = default) =>
            Task.FromResult(roleId == _role.RoleId ? _role : null);

        public Task<Role?> GetByIdAsync(int roleId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<Role>> GetAllAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<Role>> GetAllWithPermissionsAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddAsync(Role role, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public void Remove(Role role) => throw new NotSupportedException();
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string HashPassword(string password) => $"hashed:{password}";
        public bool VerifyPassword(string password, string passwordHash) => passwordHash == $"hashed:{password}";
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public FakeUnitOfWork(IUserRepository users, IAssociationRepository associations, IRoleRepository roles)
        {
            Users = users;
            Associations = associations;
            Roles = roles;
        }

        // Services not used in these tests; satisfy interface by throwing when accessed.
        public IServiceRepository Services => throw new NotSupportedException();

        public IUserRepository Users { get; }
        public IAssociationRepository Associations { get; }
        public IRoleRepository Roles { get; }
        public IPermissionRepository Permissions => throw new NotSupportedException();
        public IGroupRepository Groups => throw new NotSupportedException();
        public IProjectTypeRepository ProjectTypes => throw new NotSupportedException();
        public ICountryRepository Countries => throw new NotSupportedException();
        public ICityRepository Cities => throw new NotSupportedException();
        public ICityLocationRepository CityLocations => throw new NotSupportedException();
        public IAssociationProjectSupervisorRepository AssociationProjectSupervisors => throw new NotSupportedException();
        public IProductionCompanyRepository ProductionCompanies => throw new NotSupportedException();
        public IProjectRepository Projects => throw new NotSupportedException();
        public IWorkerRepository Workers => throw new NotSupportedException();
        public IPasswordResetTokenRepository PasswordResetTokens => throw new NotSupportedException();
        public IRefreshTokenRepository RefreshTokens => throw new NotSupportedException();
        public ISystemConfigurationRepository SystemConfigurations => throw new NotSupportedException();
        // duplicate property removed; the above single Services property satisfies the interface

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);

        public Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default) => operation();
    }
}
