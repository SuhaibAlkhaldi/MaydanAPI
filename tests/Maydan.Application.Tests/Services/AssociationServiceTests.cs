using Maydan.Application.DTOs.Associations;
using Maydan.Application.DTOs.Onboarding;
using Maydan.Application.Interfaces;
using Maydan.Application.Services;
using Maydan.Domain.Entities;
using Maydan.Domain.Enums;

namespace Maydan.Application.Tests.Services;

// Association Management, Phase 2a (MAYD-4, MAYD-40..54) — AssociationService is the first real
// consumer of IAssociationRepository's new members; these tests cover the CRUD+restore surface and
// the fine-grained Associations permission catalog (ViewAssociations/CreateAssociations/
// EditAssociations/DeleteAssociations/ManageAssociations — five distinct ids, not just two) this
// pass wires up. Same hand-rolled-fake-per-file pattern as LocationServiceTests/
// UserManagementServiceUpdateUserStatusTests (no mocking library in this test project).
public class AssociationServiceTests
{
    private const int ViewAssociations = 9;
    private const int CreateAssociations = 10;
    private const int EditAssociations = 11;
    private const int DeleteAssociations = 12;
    private const int ManageAssociations = 13;
    private const int ViewAssociationUsers = 14;
    private const int AssociationRoleId = 4;

    private static Role BuildRole(int roleId, string nameEn, params int[] permissionIds)
    {
        var role = new Role { RoleId = roleId, RoleNameEn = nameEn, RoleNameAr = nameEn };
        foreach (var permissionId in permissionIds)
        {
            var permission = new Permission { PermissionId = permissionId, PermissionNameEn = $"Permission{permissionId}", PermissionNameAr = $"Permission{permissionId}", Module = "Associations", IsActive = true };
            role.RolePermissions.Add(new RolePermission { RoleId = roleId, Role = role, PermissionId = permissionId, Permission = permission, IsActive = true });
        }

        return role;
    }

    private static User BuildUser(int userId, Role role, bool isActive = true, EntityType entityType = EntityType.BaytAlUrdon, int entityId = 1, bool isDeleted = false, DateTime? deletedAt = null) => new()
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
        IsActive = isActive,
        IsDeleted = isDeleted,
        DeletedAt = deletedAt
    };

    private static Country BuildCountry(int id) => new() { Id = id, EnglishName = "Jordan", ArabicName = "الأردن", IsActive = true };

    private static City BuildCity(int id, Country country) => new() { Id = id, CountryId = country.Id, Country = country, EnglishName = "Amman", ArabicName = "عمان", IsActive = true };

    private static Association BuildAssociation(int id, City city, bool isDeleted = false) => new()
    {
        Id = id,
        EnglishName = $"Association {id}",
        ArabicName = $"جمعية {id}",
        CityId = city.Id,
        City = city,
        IsDeleted = isDeleted,
        IsActive = true
    };

    private static Worker BuildWorker(int id, int associationId, bool isDeleted = false, DateTime? deletedAt = null) => new()
    {
        Id = id,
        AssociationId = associationId,
        IsDeleted = isDeleted,
        DeletedAt = deletedAt,
        IsActive = true
    };

    [Fact]
    public async Task GetAllAsync_CallerWithViewAssociations_ReturnsMappedDtos()
    {
        var caller = BuildUser(1, BuildRole(1, "Bayt-AlUrdon", ViewAssociations));
        var country = BuildCountry(1);
        var city = BuildCity(1, country);
        var association = BuildAssociation(10, city);

        var service = BuildService(caller, associations: [association]);

        var result = await service.GetAllAsync(caller.UserId);

        var dto = Assert.Single(result);
        Assert.Equal("Association 10", dto.EnglishName);
        Assert.Equal(country.Id, dto.CountryId);
        Assert.Equal("Jordan", dto.CountryEnglishName);
        Assert.Equal("Amman", dto.CityEnglishName);
        Assert.False(dto.IsDeleted);
    }

    [Fact]
    public async Task GetAllAsync_CallerWithoutViewOrManage_ThrowsUnauthorizedAccessException()
    {
        var caller = BuildUser(1, BuildRole(1, "NoAccess"));
        var service = BuildService(caller);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetAllAsync(caller.UserId));
    }

    [Fact]
    public async Task GetAllAsync_CallerWithManageAssociationsOnly_Succeeds()
    {
        // ManageAssociations acts as a full View/Create/Edit/Delete substitute — confirmed via both
        // the real seed data (RolePermissionSeedConfiguration.cs's own comment) and the real
        // frontend's own permission gates (associations-list.component.ts).
        var caller = BuildUser(1, BuildRole(1, "Manager", ManageAssociations));
        var service = BuildService(caller);

        var result = await service.GetAllAsync(caller.UserId);

        Assert.Empty(result);
    }

    [Fact]
    public async Task CreateAsync_CallerWithCreateAssociations_Succeeds()
    {
        var caller = BuildUser(1, BuildRole(1, "Bayt-AlUrdon", CreateAssociations, ViewAssociations));
        var country = BuildCountry(1);
        var city = BuildCity(5, country);
        var service = BuildService(caller, cities: [city]);

        var dto = new CreateAssociationDto { EnglishName = "New Assoc", ArabicName = "جمعية جديدة", CityId = city.Id, Latitude = "31.953000", Longitude = "35.910500" };

        var result = await service.CreateAsync(caller.UserId, dto);

        Assert.Equal("New Assoc", result.EnglishName);
        Assert.Equal(city.Id, result.CityId);
        Assert.Equal("31.953", result.Latitude);
    }

    [Fact]
    public async Task CreateAsync_CallerWithoutCreatePermission_ThrowsUnauthorizedAccessException()
    {
        // The real open question this pass flags rather than guesses at: a caller holding ONLY
        // ViewAssociations (read) must not be able to create — proves Create is independently gated,
        // not implied by View.
        var caller = BuildUser(1, BuildRole(1, "ViewOnly", ViewAssociations));
        var service = BuildService(caller);

        var dto = new CreateAssociationDto { EnglishName = "X", ArabicName = "س", CityId = 1 };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.CreateAsync(caller.UserId, dto));
    }

    [Fact]
    public async Task CreateAsync_UnknownCity_ThrowsKeyNotFoundException()
    {
        var caller = BuildUser(1, BuildRole(1, "Bayt-AlUrdon", CreateAssociations));
        var service = BuildService(caller);

        var dto = new CreateAssociationDto { EnglishName = "X", ArabicName = "س", CityId = 999 };

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateAsync(caller.UserId, dto));
    }

    private static OnboardAssociationAdminDto BuildAdminDto(string email = "new-admin@example.org") => new()
    {
        FirstNameEn = "Admin",
        LastNameEn = "One",
        FirstNameAr = "ادمن",
        LastNameAr = "واحد",
        Email = email,
        PhoneNumber = "+962700000000",
        InitialPassword = "Init@12345"
    };

    // Association Admin User gap.
    [Fact]
    public async Task CreateAsync_WithAdminPayload_CreatesAssociationAndAdminUserInSameCall()
    {
        var caller = BuildUser(1, BuildRole(1, "Bayt-AlUrdon", CreateAssociations, ViewAssociations));
        var country = BuildCountry(1);
        var city = BuildCity(5, country);
        var associationRole = BuildRole(AssociationRoleId, "Association", ViewAssociations, ViewAssociationUsers);
        var (service, _, userRepository) = BuildServiceWithRepositoryAndUsers(caller, null, [city], null, null, new FakeRoleRepository(associationRole));

        var dto = new CreateAssociationDto
        {
            EnglishName = "New Assoc",
            ArabicName = "جمعية جديدة",
            CityId = city.Id,
            Latitude = "31.953000",
            Longitude = "35.910500",
            Admin = BuildAdminDto()
        };

        var result = await service.CreateAsync(caller.UserId, dto);

        var createdUsers = await userRepository.GetByEntityAsync(EntityType.Association, result.Id, search: null);
        var admin = Assert.Single(createdUsers);
        Assert.Equal("new-admin@example.org", admin.Email);
        Assert.True(admin.MustResetPassword);
        Assert.True(admin.IsActive);
        Assert.Equal(AssociationRoleId, admin.RoleId);
        Assert.Equal("hashed:Init@12345", admin.PasswordHash);
        // Role permissions copied onto the new admin as direct UserPermissions, same convention
        // OnboardAssociationAdminAsync already used — confirms AssociationAdminUserFactory.Build is
        // genuinely shared, not reimplemented ad hoc for this call site.
        Assert.Equal(2, admin.UserPermissions.Count);
    }

    // Association Admin User gap — transaction safety: a duplicate email must fail BEFORE the
    // Association row is ever inserted (checked up front, outside the transaction), so nothing is
    // left half-created.
    [Fact]
    public async Task CreateAsync_WithAdminPayload_DuplicateEmail_ThrowsAndCreatesNothing()
    {
        var caller = BuildUser(1, BuildRole(1, "Bayt-AlUrdon", CreateAssociations, ViewAssociations));
        var country = BuildCountry(1);
        var city = BuildCity(5, country);
        var existingUser = BuildUser(2, BuildRole(AssociationRoleId, "Association"), entityType: EntityType.Association, entityId: 999);
        existingUser.Email = "taken@example.org";
        var associationRole = BuildRole(AssociationRoleId, "Association", ViewAssociations);
        var (service, associationRepository, userRepository) = BuildServiceWithRepositoryAndUsers(
            caller, null, [city], [existingUser], null, new FakeRoleRepository(associationRole));

        var dto = new CreateAssociationDto
        {
            EnglishName = "New Assoc",
            ArabicName = "جمعية جديدة",
            CityId = city.Id,
            Latitude = "31.953000",
            Longitude = "35.910500",
            Admin = BuildAdminDto(email: "taken@example.org")
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(caller.UserId, dto));

        Assert.Empty(await associationRepository.QueryAsync(isDeleted: false));
        Assert.Single(await userRepository.GetByEntityAsync(EntityType.Association, 999, search: null)); // only the pre-existing one
    }

    // Association Admin User gap — omitted Admin must be a complete no-op for this new behavior:
    // still a single, non-transactional insert, exactly as before this gap.
    [Fact]
    public async Task CreateAsync_WithoutAdminPayload_CreatesAssociationOnlyExactlyAsBefore()
    {
        var caller = BuildUser(1, BuildRole(1, "Bayt-AlUrdon", CreateAssociations, ViewAssociations));
        var country = BuildCountry(1);
        var city = BuildCity(5, country);
        var (service, _, userRepository) = BuildServiceWithRepositoryAndUsers(caller, null, [city], null, null, roles: null);

        var dto = new CreateAssociationDto { EnglishName = "New Assoc", ArabicName = "جمعية جديدة", CityId = city.Id, Latitude = "31.953000", Longitude = "35.910500" };

        var result = await service.CreateAsync(caller.UserId, dto);

        Assert.Empty(await userRepository.GetByEntityAsync(EntityType.Association, result.Id, search: null));
    }

    // Association Users/Details gap.
    [Fact]
    public async Task GetDetailsAsync_CallerWithViewAssociationUsers_ReturnsAssociationFieldsAndUsers()
    {
        var country = BuildCountry(1);
        var city = BuildCity(5, country);
        var association = new Association { Id = 10, EnglishName = "Amman Assoc", ArabicName = "جمعية عمان", CityId = city.Id, City = city, IsActive = true };
        var associationUser = BuildUser(2, BuildRole(AssociationRoleId, "Association"), entityType: EntityType.Association, entityId: association.Id);
        // ASEZA's own real shape (RolePermissionSeedConfiguration.cs): holds ViewAssociationUsers but
        // deliberately NOT ViewAssociations — proves this endpoint doesn't require ViewAssociations.
        var caller = BuildUser(1, BuildRole(1, "ASEZA", ViewAssociationUsers), entityType: EntityType.Aseza);
        var (service, _, _) = BuildServiceWithRepositoryAndUsers(caller, [association], [city], [associationUser], null, roles: null);

        var result = await service.GetDetailsAsync(caller.UserId, association.Id);

        Assert.Equal("Amman Assoc", result.EnglishName);
        var user = Assert.Single(result.Users);
        Assert.Equal(associationUser.UserId, user.UserId);
        Assert.Equal(associationUser.Email, user.Email);
    }

    [Fact]
    public async Task GetDetailsAsync_CallerWithoutViewAssociationUsersOrManage_ThrowsUnauthorizedAccessException()
    {
        var country = BuildCountry(1);
        var city = BuildCity(5, country);
        var association = new Association { Id = 10, EnglishName = "Amman Assoc", ArabicName = "جمعية عمان", CityId = city.Id, City = city, IsActive = true };
        // Holds plain ViewAssociations but not ViewAssociationUsers — must still be rejected: the two
        // are independent permissions, View doesn't imply View-Users.
        var caller = BuildUser(1, BuildRole(1, "ViewOnly", ViewAssociations));
        var service = BuildService(caller, [association], [city]);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetDetailsAsync(caller.UserId, association.Id));
    }

    [Fact]
    public async Task UpdateAsync_CallerWithCreateButNotEditAssociations_ThrowsUnauthorizedAccessException()
    {
        // Direct proof that Create and Edit are genuinely separate gates in this implementation —
        // holding CreateAssociations alone does not imply EditAssociations.
        var caller = BuildUser(1, BuildRole(1, "CreateOnly", CreateAssociations));
        var country = BuildCountry(1);
        var city = BuildCity(1, country);
        var association = BuildAssociation(10, city);
        var service = BuildService(caller, associations: [association], cities: [city]);

        var dto = new UpdateAssociationDto { Id = 10, EnglishName = "Updated", ArabicName = "محدثة", CityId = city.Id };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.UpdateAsync(caller.UserId, dto));
    }

    [Fact]
    public async Task UpdateAsync_CallerWithEditAssociations_Succeeds()
    {
        var caller = BuildUser(1, BuildRole(1, "Editor", EditAssociations));
        var country = BuildCountry(1);
        var city = BuildCity(1, country);
        var association = BuildAssociation(10, city);
        var service = BuildService(caller, associations: [association], cities: [city]);

        var dto = new UpdateAssociationDto { Id = 10, EnglishName = "Updated", ArabicName = "محدثة", CityId = city.Id };

        var result = await service.UpdateAsync(caller.UserId, dto);

        Assert.Equal("Updated", result.EnglishName);
    }

    [Fact]
    public async Task DeleteAsync_CallerWithDeleteAssociations_SoftDeletes()
    {
        var caller = BuildUser(1, BuildRole(1, "Deleter", DeleteAssociations));
        var country = BuildCountry(1);
        var city = BuildCity(1, country);
        var association = BuildAssociation(10, city);
        var (service, repository) = BuildServiceWithRepository(caller, associations: [association]);

        await service.DeleteAsync(caller.UserId, 10);

        Assert.True(association.IsDeleted);
        Assert.NotNull(repository.RemovedAssociation);
    }

    [Fact]
    public async Task DeleteAsync_CallerWithOnlyEditAssociations_ThrowsUnauthorizedAccessException()
    {
        var caller = BuildUser(1, BuildRole(1, "Editor", EditAssociations));
        var country = BuildCountry(1);
        var city = BuildCity(1, country);
        var association = BuildAssociation(10, city);
        var service = BuildService(caller, associations: [association]);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.DeleteAsync(caller.UserId, 10));
    }

    [Fact]
    public async Task RestoreAsync_DeletedAssociation_ClearsIsDeleted()
    {
        var caller = BuildUser(1, BuildRole(1, "Deleter", DeleteAssociations));
        var country = BuildCountry(1);
        var city = BuildCity(1, country);
        var association = BuildAssociation(10, city, isDeleted: true);
        var service = BuildService(caller, associations: [association]);

        var result = await service.RestoreAsync(caller.UserId, 10);

        Assert.False(result.IsDeleted);
        Assert.False(association.IsDeleted);
    }

    [Fact]
    public async Task RestoreAsync_NotDeleted_ThrowsInvalidOperationException()
    {
        var caller = BuildUser(1, BuildRole(1, "Deleter", DeleteAssociations));
        var country = BuildCountry(1);
        var city = BuildCity(1, country);
        var association = BuildAssociation(10, city, isDeleted: false);
        var service = BuildService(caller, associations: [association]);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RestoreAsync(caller.UserId, 10));
    }

    [Fact]
    public async Task DeleteAsync_CascadesToRealUsersAndWorkers_SoftDeletesAllWithTheSameDeletedAt()
    {
        var caller = BuildUser(1, BuildRole(1, "Deleter", DeleteAssociations));
        var country = BuildCountry(1);
        var city = BuildCity(1, country);
        var association = BuildAssociation(10, city);
        var associationUser = BuildUser(2, BuildRole(2, "AssocUser"), entityType: EntityType.Association, entityId: 10);
        var worker = BuildWorker(100, associationId: 10);
        var (service, _) = BuildServiceWithRepository(caller, [association], [city], [associationUser], [worker]);

        await service.DeleteAsync(caller.UserId, 10);

        Assert.True(association.IsDeleted);
        Assert.True(associationUser.IsDeleted);
        Assert.True(worker.IsDeleted);
        Assert.NotNull(association.DeletedAt);
        // MaydanDbContext.SaveChangesAsync computes utcNow ONCE per call and stamps every entity
        // staged for removal in it with that same instant — this is the exact real mechanic
        // RestoreAsync's own cascade depends on to tell "deleted in this cascade" apart from
        // "deleted independently" (see RestoreAsync_OnlyRestoresRowsDeletedInTheSameCascade below).
        Assert.Equal(association.DeletedAt, associationUser.DeletedAt);
        Assert.Equal(association.DeletedAt, worker.DeletedAt);
    }

    [Fact]
    public async Task DeleteAsync_WithExistingWorkers_DoesNotBlockDeletion()
    {
        // The ticket's own explicit requirement: "the existence of workers must not prevent the
        // association from being deleted" — was already true before this phase (no blocking check
        // ever existed); this proves it still holds once the cascade itself is added.
        var caller = BuildUser(1, BuildRole(1, "Deleter", DeleteAssociations));
        var country = BuildCountry(1);
        var city = BuildCity(1, country);
        var association = BuildAssociation(10, city);
        var worker = BuildWorker(100, associationId: 10);
        var (service, _) = BuildServiceWithRepository(caller, [association], [city], users: null, workers: [worker]);

        await service.DeleteAsync(caller.UserId, 10);

        Assert.True(association.IsDeleted);
        Assert.True(worker.IsDeleted);
    }

    [Fact]
    public async Task DeleteAsync_WithNoUsersOrWorkers_SucceedsWithoutError()
    {
        var caller = BuildUser(1, BuildRole(1, "Deleter", DeleteAssociations));
        var country = BuildCountry(1);
        var city = BuildCity(1, country);
        var association = BuildAssociation(10, city);
        var (service, _) = BuildServiceWithRepository(caller, [association], [city], users: null, workers: null);

        await service.DeleteAsync(caller.UserId, 10);

        Assert.True(association.IsDeleted);
    }

    [Fact]
    public async Task RestoreAsync_OnlyRestoresRowsDeletedInTheSameCascade()
    {
        var caller = BuildUser(1, BuildRole(1, "Deleter", DeleteAssociations));
        var country = BuildCountry(1);
        var city = BuildCity(1, country);
        var association = BuildAssociation(10, city);

        // Independently deleted BEFORE the association's own cascade, for an unrelated reason, but
        // scoped to the SAME association — the real case this design protects against. Both are
        // already IsDeleted when DeleteAsync's cascade below runs, so the active-only
        // GetByEntityAsync/GetByAssociationIdAsync queries never touch them (they're not
        // re-soft-deleted, and their own DeletedAt is left completely alone by DeleteAsync).
        var independentDeletedAt = DateTime.UtcNow.AddMinutes(-30);
        var independentUser = BuildUser(3, BuildRole(3, "AssocUser"), entityType: EntityType.Association, entityId: 10, isDeleted: true, deletedAt: independentDeletedAt);
        var independentWorker = BuildWorker(200, associationId: 10, isDeleted: true, deletedAt: independentDeletedAt);

        var associationUser = BuildUser(2, BuildRole(2, "AssocUser"), entityType: EntityType.Association, entityId: 10);
        var worker = BuildWorker(100, associationId: 10);

        var (service, _) = BuildServiceWithRepository(caller, [association], [city], [associationUser, independentUser], [worker, independentWorker]);

        await service.DeleteAsync(caller.UserId, 10);

        var result = await service.RestoreAsync(caller.UserId, 10);

        Assert.False(result.IsDeleted);
        Assert.False(association.IsDeleted);
        Assert.False(associationUser.IsDeleted);
        Assert.Null(associationUser.DeletedAt);
        Assert.False(worker.IsDeleted);
        Assert.Null(worker.DeletedAt);

        // The independently-deleted rows must be completely untouched by the restore.
        Assert.True(independentUser.IsDeleted);
        Assert.Equal(independentDeletedAt, independentUser.DeletedAt);
        Assert.True(independentWorker.IsDeleted);
        Assert.Equal(independentDeletedAt, independentWorker.DeletedAt);
    }

    private static AssociationService BuildService(User caller, Association[]? associations = null, City[]? cities = null) =>
        BuildServiceWithRepository(caller, associations, cities).Service;

    private static (AssociationService Service, FakeAssociationRepository Repository) BuildServiceWithRepository(User caller, Association[]? associations = null, City[]? cities = null) =>
        BuildServiceWithRepository(caller, associations, cities, users: null, workers: null);

    private static (AssociationService Service, FakeAssociationRepository Repository) BuildServiceWithRepository(
        User caller, Association[]? associations, City[]? cities, User[]? users, Worker[]? workers)
    {
        var (service, associationRepository, _) = BuildServiceWithRepositoryAndUsers(caller, associations, cities, users, workers, roles: null);
        return (service, associationRepository);
    }

    // Association Admin User / Association Users-Details gaps: the one place a role repository (for
    // the inline-admin path's role lookup) and the user repository itself (so a test can assert on
    // the admin User the service created) are both exposed — every pre-existing call above still
    // goes through the simpler overloads and is unaffected.
    private static (AssociationService Service, FakeAssociationRepository AssociationRepository, FakeUserRepository UserRepository) BuildServiceWithRepositoryAndUsers(
        User caller, Association[]? associations, City[]? cities, User[]? users, Worker[]? workers, IRoleRepository? roles)
    {
        // MAYD-51 cascade users/workers passed here are on top of the caller — the caller themselves
        // is looked up separately via GetByIdAsync/GetWithPermissionsAsync (see FakeUserRepository),
        // never through GetByEntityAsync, matching the real UserRepository's own separation.
        var userRepository = new FakeUserRepository(caller, users);
        var workerRepository = new FakeWorkerRepository(workers);
        // Same Worker instances handed to FakeWorkerRepository above — GetByIdWithWorkersAsync below
        // filters this same backing list down to the association's own active workers, exactly
        // mirroring EF's Include() + global soft-delete filter, so Remove()ing a worker reached via
        // association.Workers stages the SAME tracked instance FakeWorkerRepository will commit.
        var associationRepository = new FakeAssociationRepository(associations ?? [], cities ?? [], workers);
        var cityRepository = new FakeCityRepository(cities ?? []);

        var unitOfWork = new FakeUnitOfWork(userRepository, associationRepository, cityRepository, workerRepository, roles);
        return (new AssociationService(unitOfWork, new FakePasswordHasher()), associationRepository, userRepository);
    }

    // MAYD-51: the pending-removal + CommitPendingRemovals(utcNow) split below (also used by
    // FakeWorkerRepository and FakeAssociationRepository) simulates
    // MaydanDbContext.SaveChangesAsync's real interceptor precisely — utcNow is computed ONCE by
    // FakeUnitOfWork.SaveChangesAsync and applied to every entity Removed since the last save, not
    // once per Remove() call — so a real test can assert cascade-mates share the exact same
    // DeletedAt instant, exactly like the production interceptor guarantees.
    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly User _currentUser;
        private readonly List<User> _users;
        private readonly List<User> _pendingRemovals = new();

        public FakeUserRepository(User currentUser, IEnumerable<User>? users = null)
        {
            _currentUser = currentUser;
            _users = (users ?? Enumerable.Empty<User>()).ToList();
        }

        public Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(userId == _currentUser.UserId ? _currentUser : null);

        public Task<User?> GetWithPermissionsAsync(int userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(userId == _currentUser.UserId ? _currentUser : null);

        public Task<List<User>> GetByEntityAsync(EntityType entityType, int entityId, string? search, CancellationToken cancellationToken = default) =>
            Task.FromResult(_users.Where(u => !u.IsDeleted && u.EntityType == entityType && u.EntityId == entityId).ToList());

        public Task<List<User>> GetDeletedByEntityAsync(EntityType entityType, int entityId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_users.Where(u => u.IsDeleted && u.EntityType == entityType && u.EntityId == entityId).ToList());

        public void Remove(User user) => _pendingRemovals.Add(user);

        public void CommitPendingRemovals(DateTime utcNow)
        {
            foreach (var user in _pendingRemovals)
            {
                user.IsDeleted = true;
                user.DeletedAt = utcNow;
            }

            _pendingRemovals.Clear();
        }

        public Task<User?> GetByEmailWithAccessAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetDetailsAsync(int userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetDetailsReadOnlyAsync(int userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByUserNameEnAsync(User user, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByUserNameArAsync(User user, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<User>> GetByIdsInEntityAsync(IEnumerable<int> userIds, EntityType entityType, int entityId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(List<User> Users, int TotalCount)> GetPagedByEntityAsync(EntityType entityType, int entityId, string? search, bool? isActive, int page, int pageSize, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        // Association Admin User gap: real behavior needed now that AssociationService.CreateAsync's
        // optional inline-admin path calls both of these — previously unreachable from this test file
        // so both just threw.
        public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default) =>
            Task.FromResult(_users.Any(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase)));

        public Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            user.UserId = user.UserId == 0 ? _users.Count + 900 : user.UserId;
            _users.Add(user);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeRoleRepository : IRoleRepository
    {
        private readonly Role _associationRole;

        public FakeRoleRepository(Role associationRole) => _associationRole = associationRole;

        public Task<Role?> GetWithPermissionsAsync(int roleId, CancellationToken cancellationToken = default) =>
            Task.FromResult(roleId == _associationRole.RoleId ? _associationRole : null);

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

    private sealed class FakeWorkerRepository : IWorkerRepository
    {
        private readonly List<Worker> _workers;
        private readonly List<Worker> _pendingRemovals = new();

        public FakeWorkerRepository(IEnumerable<Worker>? workers = null) => _workers = (workers ?? Enumerable.Empty<Worker>()).ToList();

        public Task<List<Worker>> GetByAssociationIdAsync(int associationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_workers.Where(w => !w.IsDeleted && w.AssociationId == associationId).ToList());

        public Task<List<Worker>> GetDeletedByAssociationIdAsync(int associationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_workers.Where(w => w.IsDeleted && w.AssociationId == associationId).ToList());

        public void Remove(Worker worker) => _pendingRemovals.Add(worker);

        public void CommitPendingRemovals(DateTime utcNow)
        {
            foreach (var worker in _pendingRemovals)
            {
                worker.IsDeleted = true;
                worker.DeletedAt = utcNow;
            }

            _pendingRemovals.Clear();
        }

        public Task<Worker?> GetByIdAsync(int workerId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Worker?> GetByIdWithServicesAsync(int workerId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Worker?> GetByIdIncludingDeletedAsync(int workerId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Worker?> GetByCivilIdHashAsync(string civilIdHash, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddAsync(Worker worker, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(List<Maydan.Application.DTOs.Workers.WorkerSummaryDto> Items, int TotalCount)> GetAllProjectedAsync(int? associationId, string? search, int? serviceId, int page, int pageSize, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Maydan.Application.DTOs.Workers.WorkerDto?> GetByIdProjectedAsync(int workerId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class FakeCityRepository : ICityRepository
    {
        private readonly Dictionary<int, City> _citiesById;
        public FakeCityRepository(IEnumerable<City> cities) => _citiesById = cities.ToDictionary(c => c.Id);

        public Task<City?> GetByIdAsync(int cityId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_citiesById.TryGetValue(cityId, out var city) && !city.IsDeleted ? city : null);

        public Task<City?> GetByIdIncludingDeletedAsync(int cityId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<City>> GetByCountryIdAsync(int countryId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddAsync(City city, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public void Remove(City city) => throw new NotSupportedException();
    }

    private sealed class FakeAssociationRepository : IAssociationRepository
    {
        private readonly Dictionary<int, Association> _associationsById;
        private readonly Dictionary<int, City> _citiesById;
        private readonly List<Worker> _workers;
        private readonly List<Association> _pendingRemovals = new();

        public FakeAssociationRepository(IEnumerable<Association> associations, IEnumerable<City>? cities = null, IEnumerable<Worker>? workers = null)
        {
            _associationsById = associations.ToDictionary(a => a.Id);
            _citiesById = (cities ?? Enumerable.Empty<City>()).ToDictionary(c => c.Id);
            _workers = (workers ?? Enumerable.Empty<Worker>()).ToList();
        }

        public Association? RemovedAssociation { get; private set; }

        public Task<Association?> GetByIdAsync(int associationId, CancellationToken cancellationToken = default)
        {
            var association = _associationsById.GetValueOrDefault(associationId);
            return Task.FromResult(association is { IsDeleted: false } ? association : null);
        }

        public Task<Association?> GetByIdIncludingDeletedAsync(int associationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_associationsById.GetValueOrDefault(associationId));

        // MAYD-51: mirrors AssociationRepository.GetByIdWithWorkersAsync's real Include(a =>
        // a.Workers) — populates the SAME Worker instances FakeWorkerRepository owns (filtered to
        // this association, active-only, exactly like the global soft-delete filter applied to an
        // Included collection), so DeleteAsync's cascade removes the identical tracked objects.
        public Task<Association?> GetByIdWithWorkersAsync(int associationId, CancellationToken cancellationToken = default)
        {
            var association = _associationsById.GetValueOrDefault(associationId);
            if (association is not { IsDeleted: false })
            {
                return Task.FromResult<Association?>(null);
            }

            association.Workers = _workers.Where(w => !w.IsDeleted && w.AssociationId == associationId).ToList();
            return Task.FromResult<Association?>(association);
        }

        public Task<(Association Association, int WorkersCount)?> GetByIdWithWorkersCountAsync(int associationId, CancellationToken cancellationToken = default)
        {
            var association = _associationsById.GetValueOrDefault(associationId);
            return Task.FromResult(association is { IsDeleted: false } ? (association, 0) : ((Association, int)?)null);
        }

        public Task<List<(Association Association, int WorkersCount)>> QueryAsync(bool isDeleted, string? searchTerm = null, bool? orderByWorkersCountAscending = null, CancellationToken cancellationToken = default)
        {
            var results = _associationsById.Values.Where(a => a.IsDeleted == isDeleted);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                results = results.Where(a => a.EnglishName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) || a.ArabicName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
            }

            return Task.FromResult(results.Select(a => (a, 0)).ToList());
        }

        public Task<List<Association>> GetAllAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task AddAsync(Association association, CancellationToken cancellationToken = default)
        {
            association.Id = association.Id == 0 ? _associationsById.Count + 100 : association.Id;
            // Real EF Core performs City navigation fixup automatically once both entities are
            // tracked in the same DbContext (AssociationService.CreateAsync only sets CityId, not
            // .City) — this plain in-memory fake has no change tracker, so it's done by hand here,
            // matching real runtime behavior, for MapToDto (called right after AddAsync via
            // MapExistingAsync) to have a non-null City/Country to read.
            if (_citiesById.TryGetValue(association.CityId, out var city))
            {
                association.City = city;
            }

            _associationsById[association.Id] = association;
            return Task.CompletedTask;
        }

        public void Remove(Association association) => _pendingRemovals.Add(association);

        public void CommitPendingRemovals(DateTime utcNow)
        {
            foreach (var association in _pendingRemovals)
            {
                association.IsDeleted = true;
                association.DeletedAt = utcNow;
                RemovedAssociation = association;
            }

            _pendingRemovals.Clear();
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        private readonly FakeUserRepository _users;
        private readonly FakeAssociationRepository _associations;
        private readonly FakeWorkerRepository _workers;
        private readonly IRoleRepository? _roles;

        public FakeUnitOfWork(FakeUserRepository users, FakeAssociationRepository associations, ICityRepository cities, FakeWorkerRepository? workers = null, IRoleRepository? roles = null)
        {
            _users = users;
            _associations = associations;
            _workers = workers ?? new FakeWorkerRepository();
            _roles = roles;
            Cities = cities;
        }

        public IUserRepository Users => _users;
        public IAssociationRepository Associations => _associations;
        public ICityRepository Cities { get; }
        public ICityLocationRepository CityLocations => throw new NotSupportedException();
        public IAssociationProjectSupervisorRepository AssociationProjectSupervisors => throw new NotSupportedException();
        public IWorkerRepository Workers => _workers;
        public IRoleRepository Roles => _roles ?? throw new NotSupportedException();
        public IPermissionRepository Permissions => throw new NotSupportedException();
        public IGroupRepository Groups => throw new NotSupportedException();
        public IProjectTypeRepository ProjectTypes => throw new NotSupportedException();
        public ICountryRepository Countries => throw new NotSupportedException();
        public IProductionCompanyRepository ProductionCompanies => throw new NotSupportedException();
        public IProjectRepository Projects => throw new NotSupportedException();
        public IPasswordResetTokenRepository PasswordResetTokens => throw new NotSupportedException();
        public IRefreshTokenRepository RefreshTokens => throw new NotSupportedException();
        public ISystemConfigurationRepository SystemConfigurations => throw new NotSupportedException();
        public IServiceRepository Services => throw new NotSupportedException();

        // Mirrors MaydanDbContext.SaveChangesAsync's own interceptor: ONE utcNow computed per call,
        // applied to every entity Removed (across all three fakes) since the last save — see the
        // comment on FakeUserRepository for why this precision matters to the cascade tests.
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var utcNow = DateTime.UtcNow;
            _associations.CommitPendingRemovals(utcNow);
            _users.CommitPendingRemovals(utcNow);
            _workers.CommitPendingRemovals(utcNow);
            return Task.FromResult(1);
        }

        public Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default) => operation();
    }
}
