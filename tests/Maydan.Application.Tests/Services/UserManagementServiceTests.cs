using Maydan.Application.DTOs.UserManagement;
using Maydan.Application.Interfaces;
using Maydan.Application.Services;
using Maydan.Domain.Entities;
using Maydan.Domain.Enums;

namespace Maydan.Application.Tests.Services;

// Audit follow-up (entityType/entityId mis-scoping found during the RBAC ticket's live
// verification): CreateUserAsync always stamps the new user's EntityType/EntityId from the
// CALLER, never from dto.RoleId. That's correct for same-role creation (the only case the real UI
// ever sends) but was silently wrong for a cross-role call. These tests prove
// UserManagementService.EnsureSameEntityCreation() rejects the cross-role case for everyone EXCEPT
// Bayt-AlUrdon, while leaving the same-role, self-service path untouched.
//
// MAYD-1 real fix (2026-09-24, product-owner-confirmed): Bayt-AlUrdon's confirmed authority is
// view + CREATE across every entity. The old "CreateUserAsync_DifferentRoleThanCreator_..." test
// below used Bayt-AlUrdon as the CALLER and asserted a throw — that assumption is no longer true
// and the test is rewritten (as
// CreateUserAsync_NonBaytAlUrdonDifferentRoleThanCreator_ThrowsAndNeverCreatesTheUser) to use a
// non-Bayt-AlUrdon caller instead, which is still correctly rejected. New tests below cover the
// Bayt-AlUrdon exception itself, including that the created user's EntityType/EntityId resolve to
// the TARGET role's entity (not the caller's Bayt-AlUrdon entity), and that group-id validation
// during that create is scoped to the target entity too (EnsureGroupsInEntityAsync's own fix).
public class UserManagementServiceTests
{
    [Fact]
    public async Task CreateUserAsync_SameRoleAsCreator_SucceedsAndInheritsCreatorsEntity()
    {
        var associationRole = new Role { RoleId = 4, RoleNameEn = "Association", RoleNameAr = "الجمعية" };
        var currentUser = new User
        {
            UserId = 1,
            RoleId = associationRole.RoleId,
            Role = associationRole,
            EntityType = EntityType.Association,
            EntityId = 7,
            IsActive = true
        };

        var userRepository = new FakeUserRepository(currentUser, associationRole);
        var unitOfWork = new FakeUnitOfWork(userRepository, new FakeRoleRepository(associationRole));
        var service = new UserManagementService(unitOfWork, new FakePasswordHasher());

        var dto = new CreateEntityUserDto
        {
            FirstNameEn = "New",
            LastNameEn = "Employee",
            FirstNameAr = "جديد",
            LastNameAr = "موظف",
            Email = "new.employee@example.org",
            PhoneNumber = "+962700000000",
            InitialPassword = "P@ssw0rd!",
            RoleId = associationRole.RoleId // same role as the creator
        };

        var result = await service.CreateUserAsync(currentUser.UserId, dto);

        Assert.Equal(EntityType.Association, result.EntityType);
        Assert.Equal(7, result.EntityId);
        Assert.NotNull(userRepository.AddedUser);
        Assert.Equal(EntityType.Association, userRepository.AddedUser!.EntityType);
        Assert.Equal(7, userRepository.AddedUser.EntityId);
    }

    [Fact]
    public async Task CreateUserAsync_NonBaytAlUrdonDifferentRoleThanCreator_ThrowsAndNeverCreatesTheUser()
    {
        var asezaRole = new Role { RoleId = 2, RoleNameEn = "ASEZA", RoleNameAr = "أسيزا" };
        var currentUser = new User
        {
            UserId = 1,
            RoleId = asezaRole.RoleId,
            Role = asezaRole,
            EntityType = EntityType.Aseza,
            EntityId = 1,
            IsActive = true
        };

        var userRepository = new FakeUserRepository(currentUser, asezaRole);
        // The role catalog lookup is never reached — EnsureSameEntityCreation rejects before it —
        // so the repository only needs to know the creator's own role.
        var unitOfWork = new FakeUnitOfWork(userRepository, new FakeRoleRepository(asezaRole));
        var service = new UserManagementService(unitOfWork, new FakePasswordHasher());

        var dto = new CreateEntityUserDto
        {
            FirstNameEn = "Cross",
            LastNameEn = "Role",
            FirstNameAr = "عبر",
            LastNameAr = "دور",
            Email = "cross.role@example.org",
            PhoneNumber = "+962700000001",
            InitialPassword = "P@ssw0rd!",
            RoleId = 4 // Association — different from the caller's own role (2, ASEZA)
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateUserAsync(currentUser.UserId, dto));

        Assert.Contains("different role", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Null(userRepository.AddedUser);
    }

    [Theory]
    [InlineData(4, EntityType.Association)] // Association
    [InlineData(3, EntityType.ProductionCompany)] // ProductionHouse
    [InlineData(2, EntityType.Aseza)] // ASEZA
    public async Task CreateUserAsync_BaytAlUrdonCreatingADifferentRole_SucceedsScopedToTheTargetRolesEntity(int targetRoleId, EntityType expectedEntityType)
    {
        var baytAlUrdonRole = new Role { RoleId = 1, RoleNameEn = "Bayt-AlUrdon", RoleNameAr = "بيت الأردن" };
        var targetRole = new Role { RoleId = targetRoleId, RoleNameEn = "Target", RoleNameAr = "هدف" };
        var currentUser = new User
        {
            UserId = 1,
            RoleId = baytAlUrdonRole.RoleId,
            Role = baytAlUrdonRole,
            EntityType = EntityType.BaytAlUrdon,
            EntityId = 1,
            IsActive = true
        };

        var userRepository = new FakeUserRepository(currentUser, targetRole);
        var unitOfWork = new FakeUnitOfWork(userRepository, new FakeRoleRepository(baytAlUrdonRole, targetRole));
        var service = new UserManagementService(unitOfWork, new FakePasswordHasher());

        var dto = new CreateEntityUserDto
        {
            FirstNameEn = "Cross",
            LastNameEn = "Entity",
            FirstNameAr = "عبر",
            LastNameAr = "كيان",
            Email = $"cross.entity.{targetRoleId}@example.org",
            PhoneNumber = "+962700000002",
            InitialPassword = "P@ssw0rd!",
            RoleId = targetRoleId
        };

        var result = await service.CreateUserAsync(currentUser.UserId, dto);

        // Never the CALLER's own Bayt-AlUrdon entity — the resolved TARGET role's entity instead.
        Assert.Equal(expectedEntityType, result.EntityType);
        Assert.NotEqual(EntityType.BaytAlUrdon, result.EntityType);
        Assert.Equal(1, result.EntityId); // the established single-placeholder-entity convention
        Assert.NotNull(userRepository.AddedUser);
        Assert.Equal(expectedEntityType, userRepository.AddedUser!.EntityType);
    }

    // Association Management, Phase 2b (2026-09-26): the real gap this closes — every prior test
    // above proves the PLACEHOLDER (EntityId = 1) is used for a cross-entity create; these prove
    // the opt-in override actually reaches a real Association row when the caller supplies one
    // (e.g. Bayt-AlUrdon creating a user from /associations/9004/users/new), and that every other
    // combination above (no dto.EntityId at all) is completely untouched by this addition.
    [Fact]
    public async Task CreateUserAsync_BaytAlUrdonCreatingAssociationRoleWithARealEntityId_UsesTheRealAssociationIdNotThePlaceholder()
    {
        var baytAlUrdonRole = new Role { RoleId = 1, RoleNameEn = "Bayt-AlUrdon", RoleNameAr = "بيت الأردن" };
        var associationRole = new Role { RoleId = 4, RoleNameEn = "Association", RoleNameAr = "الجمعية" };
        var currentUser = new User
        {
            UserId = 1,
            RoleId = baytAlUrdonRole.RoleId,
            Role = baytAlUrdonRole,
            EntityType = EntityType.BaytAlUrdon,
            EntityId = 1,
            IsActive = true
        };
        var realAssociation = new Association { Id = 9004, EnglishName = "Real Association", ArabicName = "جمعية حقيقية" };

        var userRepository = new FakeUserRepository(currentUser, associationRole);
        var associationRepository = new FakeAssociationRepository(realAssociation);
        var unitOfWork = new FakeUnitOfWork(userRepository, new FakeRoleRepository(baytAlUrdonRole, associationRole), associations: associationRepository);
        var service = new UserManagementService(unitOfWork, new FakePasswordHasher());

        var dto = new CreateEntityUserDto
        {
            FirstNameEn = "Real",
            LastNameEn = "Association User",
            FirstNameAr = "حقيقي",
            LastNameAr = "مستخدم جمعية",
            Email = "real.association.user@example.org",
            PhoneNumber = "+962700000003",
            InitialPassword = "P@ssw0rd!",
            RoleId = associationRole.RoleId,
            EntityId = realAssociation.Id
        };

        var result = await service.CreateUserAsync(currentUser.UserId, dto);

        Assert.Equal(EntityType.Association, result.EntityType);
        Assert.Equal(9004, result.EntityId); // the REAL association id, not the placeholder 1
        Assert.NotNull(userRepository.AddedUser);
        Assert.Equal(9004, userRepository.AddedUser!.EntityId);
    }

    [Fact]
    public async Task CreateUserAsync_BaytAlUrdonCreatingAssociationRoleWithAnUnknownEntityId_ThrowsKeyNotFoundExceptionAndNeverCreatesTheUser()
    {
        var baytAlUrdonRole = new Role { RoleId = 1, RoleNameEn = "Bayt-AlUrdon", RoleNameAr = "بيت الأردن" };
        var associationRole = new Role { RoleId = 4, RoleNameEn = "Association", RoleNameAr = "الجمعية" };
        var currentUser = new User
        {
            UserId = 1,
            RoleId = baytAlUrdonRole.RoleId,
            Role = baytAlUrdonRole,
            EntityType = EntityType.BaytAlUrdon,
            EntityId = 1,
            IsActive = true
        };

        var userRepository = new FakeUserRepository(currentUser, associationRole);
        var associationRepository = new FakeAssociationRepository(); // empty — no association 999
        var unitOfWork = new FakeUnitOfWork(userRepository, new FakeRoleRepository(baytAlUrdonRole, associationRole), associations: associationRepository);
        var service = new UserManagementService(unitOfWork, new FakePasswordHasher());

        var dto = new CreateEntityUserDto
        {
            FirstNameEn = "Bad",
            LastNameEn = "Entity",
            FirstNameAr = "سيء",
            LastNameAr = "كيان",
            Email = "bad.entity@example.org",
            PhoneNumber = "+962700000004",
            InitialPassword = "P@ssw0rd!",
            RoleId = associationRole.RoleId,
            EntityId = 999
        };

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateUserAsync(currentUser.UserId, dto));
        Assert.Null(userRepository.AddedUser);
    }

    [Fact]
    public async Task CreateUserAsync_BaytAlUrdonCreatingADifferentRole_ScopesGroupIdValidationToTheTargetEntityNotTheCallers()
    {
        var baytAlUrdonRole = new Role { RoleId = 1, RoleNameEn = "Bayt-AlUrdon", RoleNameAr = "بيت الأردن" };
        var associationRole = new Role { RoleId = 4, RoleNameEn = "Association", RoleNameAr = "الجمعية" };
        var currentUser = new User
        {
            UserId = 1,
            RoleId = baytAlUrdonRole.RoleId,
            Role = baytAlUrdonRole,
            EntityType = EntityType.BaytAlUrdon,
            EntityId = 1,
            IsActive = true
        };

        var group = new Group { GroupId = 5, GroupNameEn = "g", GroupNameAr = "g", EntityType = EntityType.Association, EntityId = 1, IsActive = true };
        var userRepository = new FakeUserRepository(currentUser, associationRole, groupsById: new Dictionary<int, Group> { [5] = group });
        var groupRepository = new FakeGroupRepository(group);
        var unitOfWork = new FakeUnitOfWork(userRepository, new FakeRoleRepository(baytAlUrdonRole, associationRole), groupRepository);
        var service = new UserManagementService(unitOfWork, new FakePasswordHasher());

        var dto = new CreateEntityUserDto
        {
            FirstNameEn = "Cross",
            LastNameEn = "Entity",
            FirstNameAr = "عبر",
            LastNameAr = "كيان",
            Email = "cross.entity.groups@example.org",
            PhoneNumber = "+962700000003",
            InitialPassword = "P@ssw0rd!",
            RoleId = associationRole.RoleId,
            GroupIds = new List<int> { 5 }
        };

        await service.CreateUserAsync(currentUser.UserId, dto);

        // Proves EnsureGroupsInEntityAsync was queried against the TARGET (Association) entity —
        // not EntityType.BaytAlUrdon/1, the caller's own entity — the exact bug this fix closes.
        Assert.Equal(EntityType.Association, groupRepository.LastQueriedEntityType);
        Assert.Equal(1, groupRepository.LastQueriedEntityId);
    }

    private sealed class FakeAssociationRepository : IAssociationRepository
    {
        private readonly Dictionary<int, Association> _associationsById;
        public FakeAssociationRepository(params Association[] associations) => _associationsById = associations.ToDictionary(a => a.Id);

        public Task<Association?> GetByIdAsync(int associationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_associationsById.TryGetValue(associationId, out var association) ? association : null);

        // Not exercised by CreateUserAsync's entityId-override path.
        public Task<Association?> GetByIdIncludingDeletedAsync(int associationId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Association?> GetByCityIdAsync(int cityId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Association?> GetByIdWithWorkersAsync(int associationId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(Association Association, int WorkersCount)?> GetByIdWithWorkersCountAsync(int associationId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<(Association Association, int WorkersCount)>> QueryAsync(bool isDeleted, string? searchTerm = null, bool? orderByWorkersCountAscending = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<Association>> GetAllAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddAsync(Association association, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public void Remove(Association association) => throw new NotSupportedException();
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly Dictionary<int, User> _usersById;
        private readonly Role _roleForNewUsers;
        private readonly Dictionary<int, Group>? _groupsById;

        public FakeUserRepository(User currentUser, Role roleForNewUsers, Dictionary<int, Group>? groupsById = null)
        {
            _usersById = new Dictionary<int, User> { [currentUser.UserId] = currentUser };
            _roleForNewUsers = roleForNewUsers;
            _groupsById = groupsById;
        }

        public User? AddedUser { get; private set; }

        public Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_usersById.TryGetValue(userId, out var user) ? user : null);

        public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            user.UserId = 100;
            user.Role = _roleForNewUsers;

            // Real EF Core performs Group navigation fixup automatically once both entities are
            // tracked in the same DbContext (CreateUserAsync itself only sets UserGroup.GroupId, not
            // .Group) — this plain in-memory fake has no change tracker, so it's done by hand here,
            // matching real runtime behavior, only for MapUserDetails (called right after AddAsync)
            // to have a non-null Group to read.
            if (_groupsById is not null)
            {
                foreach (var userGroup in user.UserGroups)
                {
                    if (_groupsById.TryGetValue(userGroup.GroupId, out var group))
                    {
                        userGroup.Group = group;
                    }
                }
            }

            AddedUser = user;
            _usersById[user.UserId] = user;
            return Task.CompletedTask;
        }

        public Task<User?> GetDetailsReadOnlyAsync(int userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_usersById.TryGetValue(userId, out var user) ? user : null);

        // Not exercised by either test above.
        public Task<User?> GetByEmailWithAccessAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<User>> GetByEntityAsync(EntityType entityType, int entityId, string? search, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<User>> GetDeletedByEntityAsync(EntityType entityType, int entityId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(List<User> Users, int TotalCount)> GetPagedByEntityAsync(EntityType entityType, int entityId, string? search, bool? isActive, int page, int pageSize, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        // MAYD-37: GetCurrentUserAsync now fetches via GetDetailsAsync (not GetByIdAsync) so
        // UserPermissions/UserGroups are loaded for the caller — same lookup as GetByIdAsync above.
        public Task<User?> GetDetailsAsync(int userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_usersById.TryGetValue(userId, out var user) ? user : null);
        public Task<User?> GetByUserNameEnAsync(User user, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByUserNameArAsync(User user, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetWithPermissionsAsync(int userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<User>> GetByIdsInEntityAsync(IEnumerable<int> userIds, EntityType entityType, int entityId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public void Remove(User user) => throw new NotSupportedException();
    }

    private sealed class FakeGroupRepository : IGroupRepository
    {
        private readonly Dictionary<int, Group> _groupsById;

        public FakeGroupRepository(params Group[] groups) => _groupsById = groups.ToDictionary(g => g.GroupId);

        public EntityType? LastQueriedEntityType { get; private set; }
        public int? LastQueriedEntityId { get; private set; }

        public Task<List<Group>> GetByIdsInEntityAsync(IEnumerable<int> groupIds, EntityType entityType, int entityId, CancellationToken cancellationToken = default)
        {
            LastQueriedEntityType = entityType;
            LastQueriedEntityId = entityId;
            return Task.FromResult(groupIds.Where(id => _groupsById.ContainsKey(id)).Select(id => _groupsById[id]).ToList());
        }

        public Task<Group?> GetByIdAsync(int groupId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Group?> GetDetailsAsync(int groupId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<Group>> GetAllAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<Group>> GetByEntityAsync(EntityType entityType, int entityId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> NameExistsInEntityAsync(EntityType entityType, int entityId, string groupNameEn, string groupNameAr, int? excludedGroupId = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddAsync(Group group, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public void Remove(Group group) => throw new NotSupportedException();
    }

    private sealed class FakeRoleRepository : IRoleRepository
    {
        private readonly Dictionary<int, Role> _rolesById;

        public FakeRoleRepository(params Role[] roles) => _rolesById = roles.ToDictionary(r => r.RoleId);

        public Task<Role?> GetWithPermissionsAsync(int roleId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_rolesById.TryGetValue(roleId, out var role) ? role : null);

        // Not exercised by either test above.
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

    // Users/Roles are always backed by a working fake; Groups/Associations only when a test
    // explicitly passes one (most tests use empty GroupIds, so EnsureGroupsInEntityAsync returns
    // before touching it; Associations is only touched by the new entityId-override path below).
    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        private readonly IGroupRepository? _groups;
        private readonly IAssociationRepository? _associations;

        public FakeUnitOfWork(IUserRepository users, IRoleRepository roles, IGroupRepository? groups = null, IAssociationRepository? associations = null)
        {
            Users = users;
            Roles = roles;
            _groups = groups;
            _associations = associations;
        }

        public IUserRepository Users { get; }
        public IRoleRepository Roles { get; }
        public IPermissionRepository Permissions => throw new NotSupportedException();
        public IGroupRepository Groups => _groups ?? throw new NotSupportedException();
        public IProjectTypeRepository ProjectTypes => throw new NotSupportedException();
        public ICountryRepository Countries => throw new NotSupportedException();
        public ICityRepository Cities => throw new NotSupportedException();
        public ICityLocationRepository CityLocations => throw new NotSupportedException();
        public IAssociationProjectSupervisorRepository AssociationProjectSupervisors => throw new NotSupportedException();
        public IAssociationRepository Associations => _associations ?? throw new NotSupportedException();
        public IProductionCompanyRepository ProductionCompanies => throw new NotSupportedException();
        public IProjectRepository Projects => throw new NotSupportedException();
        public IWorkerRepository Workers => throw new NotSupportedException();
        public IPasswordResetTokenRepository PasswordResetTokens => throw new NotSupportedException();
        public IRefreshTokenRepository RefreshTokens => throw new NotSupportedException();
        public ISystemConfigurationRepository SystemConfigurations => throw new NotSupportedException();
        // Services not used in these tests; satisfy interface by throwing when accessed.
        public IServiceRepository Services => throw new NotSupportedException();

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);

        public Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default) => operation();
    }
}
