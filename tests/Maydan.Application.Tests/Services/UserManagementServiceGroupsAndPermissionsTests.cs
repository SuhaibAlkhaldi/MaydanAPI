using Maydan.Application.DTOs.UserManagement;
using Maydan.Application.Interfaces;
using Maydan.Application.Services;
using Maydan.Domain.Entities;
using Maydan.Domain.Enums;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Maydan.Application.Tests.Services;

public class UserManagementServiceGroupsAndPermissionsTests
{
    private static Role BuildRole(int roleId, string nameEn) => new() { RoleId = roleId, RoleNameEn = nameEn, RoleNameAr = nameEn };

    private static User BuildUser(int userId, EntityType entityType, int entityId, Role role) => new()
    {
        UserId = userId,
        RoleId = role.RoleId,
        Role = role,
        EntityType = entityType,
        EntityId = entityId,
        IsActive = true
    };

    private static Permission BuildPermission(int id, string module, string nameEn) =>
        new() { PermissionId = id, PermissionNameEn = nameEn, PermissionNameAr = nameEn, Module = module, IsActive = true };

    private static Group BuildGroup(int groupId, EntityType entityType, int entityId, string nameEn, params Permission[] permissions)
    {
        var group = new Group { GroupId = groupId, GroupNameEn = nameEn, GroupNameAr = nameEn, EntityType = entityType, EntityId = entityId, IsActive = true };
        foreach (var permission in permissions)
        {
            group.GroupPermissions.Add(new GroupPermission { GroupId = groupId, Group = group, PermissionId = permission.PermissionId, Permission = permission, IsActive = true });
        }

        return group;
    }

    [Fact]
    public async Task GetGroupsAsync_only_returns_the_callers_own_entitys_groups_never_another_entitys()
    {
        var role = BuildRole(4, "Association");
        var caller = BuildUser(1, EntityType.Association, 5, role);

        var ownGroup = BuildGroup(10, EntityType.Association, 5, "Own Entity Group");
        var otherEntityGroup = BuildGroup(11, EntityType.Association, 999, "Other Association's Group");
        var differentTypeGroup = BuildGroup(12, EntityType.BaytAlUrdon, 1, "Bayt-AlUrdon's Group");

        var repository = new FakeGroupRepository(new[] { ownGroup, otherEntityGroup, differentTypeGroup });
        var service = BuildService(caller, groupRepository: repository);

        var result = await service.GetGroupsAsync(1);

        Assert.Single(result.Data!);
        Assert.Equal(10, result.Data![0].GroupId);
        Assert.Equal(EntityType.Association, repository.LastQueriedEntityType);
        Assert.Equal(5, repository.LastQueriedEntityId);
    }

    [Fact]
    public async Task GetGroupsAsync_reports_the_real_permission_count_and_a_capped_ordered_preview()
    {
        var role = BuildRole(1, "Bayt-AlUrdon");
        var caller = BuildUser(1, EntityType.BaytAlUrdon, 1, role);

        var permissions = new[]
        {
            BuildPermission(5, "Zeta", "Zeta Permission"),
            BuildPermission(1, "Alpha", "Alpha Permission"),
            BuildPermission(2, "Alpha", "Beta Permission"),
            BuildPermission(3, "Bravo", "Gamma Permission"),
            BuildPermission(4, "Charlie", "Delta Permission")
        };
        var group = BuildGroup(20, EntityType.BaytAlUrdon, 1, "Big Group", permissions);

        var repository = new FakeGroupRepository(new[] { group });
        var service = BuildService(caller, groupRepository: repository);

        var result = await service.GetGroupsAsync(1);

        var summary = Assert.Single(result.Data!);
        Assert.Equal(5, summary.PermissionCount);
        Assert.Equal(4, summary.PermissionsPreview.Count);
        Assert.Equal(new[] { "Alpha Permission", "Beta Permission", "Gamma Permission", "Delta Permission" },
            summary.PermissionsPreview.Select(p => p.PermissionNameEn));
    }

    [Fact]
    public async Task GetGroupsAsync_excludes_inactive_permissions_from_both_count_and_preview()
    {
        var role = BuildRole(1, "Bayt-AlUrdon");
        var caller = BuildUser(1, EntityType.BaytAlUrdon, 1, role);

        var active = BuildPermission(1, "Users", "Active Permission");
        var inactive = BuildPermission(2, "Users", "Retired Permission");
        inactive.IsActive = false;

        var group = BuildGroup(30, EntityType.BaytAlUrdon, 1, "Group", active, inactive);
        var repository = new FakeGroupRepository(new[] { group });
        var service = BuildService(caller, groupRepository: repository);

        var groupsResponse = await service.GetGroupsAsync(1);
        var summary = Assert.Single(groupsResponse.Data!);

        Assert.Equal(1, summary.PermissionCount);
        Assert.Equal(new[] { "Active Permission" }, summary.PermissionsPreview.Select(p => p.PermissionNameEn));
    }

    [Fact]
    public async Task GetAvailablePermissionsAsync_with_no_roleId_returns_the_full_real_catalog()
    {
        var role = BuildRole(1, "Bayt-AlUrdon");
        var caller = BuildUser(1, EntityType.BaytAlUrdon, 1, role);
        var catalog = new[] { BuildPermission(1, "Users", "View Users"), BuildPermission(2, "Groups", "View Groups") };

        var service = BuildService(caller, permissionRepository: new FakePermissionRepository(catalog));

        var result = await service.GetAvailablePermissionsAsync(1, null);

        Assert.Equal(2, result.Data!.Count);
    }

    [Fact]
    public async Task GetAvailablePermissionsAsync_with_a_roleId_returns_only_that_roles_active_granted_permissions()
    {
        var targetRole = BuildRole(4, "Association");
        var caller = BuildUser(1, EntityType.Association, 1, targetRole);

        var granted = BuildPermission(1, "Users", "View Users");
        var notGranted = BuildPermission(2, "Groups", "View Groups");
        targetRole.RolePermissions.Add(new RolePermission { RoleId = 4, Role = targetRole, PermissionId = 1, Permission = granted, IsActive = true });

        var roleRepository = new FakeRoleRepository(targetRole);
        var service = BuildService(caller, roleRepository: roleRepository);

        var result = await service.GetAvailablePermissionsAsync(1, 4);

        var permission = Assert.Single(result.Data!);
        Assert.Equal("View Users", permission.PermissionNameEn);
    }

    [Fact]
    public async Task UpdateDirectPermissionsAsync_rejects_a_user_modifying_their_own_permissions()
    {
        var caller = BuildUser(1, EntityType.BaytAlUrdon, 1, BuildRole(1, "Bayt-AlUrdon"));
        var service = BuildService(caller);

        var result = await service.UpdateDirectPermissionsAsync(1, 1, new UpdateUserPermissionsDto { PermissionIds = new List<int> { 1 } });

        Assert.False(result.Success);
        Assert.Contains("cannot modify your own permissions", result.MessageEn, StringComparison.OrdinalIgnoreCase);
    }

  

    private static UserManagementService BuildService(
        User caller,
        FakeGroupRepository? groupRepository = null,
        FakePermissionRepository? permissionRepository = null,
        FakeRoleRepository? roleRepository = null) =>
        new(
            new FakeUnitOfWork(
                new FakeUserRepository(caller),
                groupRepository ?? new FakeGroupRepository(Array.Empty<Group>()),
                permissionRepository ?? new FakePermissionRepository(Array.Empty<Permission>()),
                roleRepository ?? new FakeRoleRepository(null)),
            new FakePasswordHasher());

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string HashPassword(string password) => $"hashed:{password}";
        public bool VerifyPassword(string password, string passwordHash) => passwordHash == $"hashed:{password}";
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly User _currentUser;
        public FakeUserRepository(User currentUser) => _currentUser = currentUser;

        public Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(userId == _currentUser.UserId ? _currentUser : null);

        public Task<User?> GetByEmailWithAccessAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<User>> GetByEntityAsync(EntityType entityType, int entityId, string? search, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<User>> GetDeletedByEntityAsync(EntityType entityType, int entityId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetDetailsAsync(int userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(userId == _currentUser.UserId ? _currentUser : null);
        public Task<User?> GetDetailsReadOnlyAsync(int userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByUserNameEnAsync(User user, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByUserNameArAsync(User user, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetWithPermissionsAsync(int userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<User>> GetByIdsInEntityAsync(IEnumerable<int> userIds, EntityType entityType, int entityId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(List<User> Users, int TotalCount)> GetPagedByEntityAsync(EntityType entityType, int entityId, string? search, bool? isActive, int page, int pageSize, CancellationToken cancellationToken = default) => throw new NotSupportedException();
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

    private sealed class FakeGroupRepository : IGroupRepository
    {
        private readonly List<Group> _groups;
        public FakeGroupRepository(IEnumerable<Group> groups) => _groups = groups.ToList();

        public EntityType? LastQueriedEntityType { get; private set; }
        public int? LastQueriedEntityId { get; private set; }

        public Task<List<Group>> GetByEntityAsync(EntityType entityType, int entityId, CancellationToken cancellationToken = default)
        {
            LastQueriedEntityType = entityType;
            LastQueriedEntityId = entityId;
            return Task.FromResult(_groups.Where(g => g.EntityType == entityType && g.EntityId == entityId).ToList());
        }

        public Task<Group?> GetByIdAsync(int groupId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Group?> GetDetailsAsync(int groupId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<Group>> GetAllAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<Group>> GetByIdsInEntityAsync(IEnumerable<int> groupIds, EntityType entityType, int entityId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> NameExistsInEntityAsync(EntityType entityType, int entityId, string groupNameEn, string groupNameAr, int? excludedGroupId = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddAsync(Group group, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public void Remove(Group group) => throw new NotSupportedException();
    }

    private sealed class FakePermissionRepository : IPermissionRepository
    {
        private readonly List<Permission> _permissions;
        public FakePermissionRepository(IEnumerable<Permission> permissions) => _permissions = permissions.ToList();

        public Task<List<Permission>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult(_permissions);
        public Task<Permission?> GetByIdAsync(int permissionId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<Permission>> GetByIdsAsync(IEnumerable<int> permissionIds, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<Permission>> GetByModuleAsync(string module, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddAsync(Permission permission, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public void Remove(Permission permission) => throw new NotSupportedException();
    }

    private sealed class FakeRoleRepository : IRoleRepository
    {
        private readonly Role? _role;
        public FakeRoleRepository(Role? role) => _role = role;

        public Task<Role?> GetWithPermissionsAsync(int roleId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_role?.RoleId == roleId ? _role : null);

        public Task<Role?> GetByIdAsync(int roleId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<Role>> GetAllAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<Role>> GetAllWithPermissionsAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddAsync(Role role, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public void Remove(Role role) => throw new NotSupportedException();
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public FakeUnitOfWork(IUserRepository users, IGroupRepository groups, IPermissionRepository permissions, IRoleRepository roles)
        {
            Users = users;
            Groups = groups;
            Permissions = permissions;
            Roles = roles;
        }

        public IUserRepository Users { get; }
        public IGroupRepository Groups { get; }
        public IPermissionRepository Permissions { get; }
        public IRoleRepository Roles { get; }
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