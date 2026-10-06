using Maydan.Application.DTOs.ProductionCompanies;
using Maydan.Application.Interfaces;
using Maydan.Application.Services;
using Maydan.Domain.Entities;
using Maydan.Domain.Enums;

namespace Maydan.Application.Tests.Services;

// Production House Management (MAYD-80/81/82) — ProductionCompanyService is the first real
// consumer of IProductionCompanyRepository's QueryAsync/GetByIdAsync(with City/Country). Same
// hand-rolled-fake-per-file pattern as every other service test file (no mocking library).
public class ProductionCompanyServiceTests
{
    private const int ViewProductionCompanies = 19;
    private const int ManageProductionCompanies = 20;

    private static Role BuildRole(int roleId, string nameEn, params int[] permissionIds)
    {
        var role = new Role { RoleId = roleId, RoleNameEn = nameEn, RoleNameAr = nameEn };
        foreach (var permissionId in permissionIds)
        {
            var permission = new Permission { PermissionId = permissionId, PermissionNameEn = $"Permission{permissionId}", PermissionNameAr = $"Permission{permissionId}", Module = "ProductionCompanies", IsActive = true };
            role.RolePermissions.Add(new RolePermission { RoleId = roleId, Role = role, PermissionId = permissionId, Permission = permission, IsActive = true });
        }

        return role;
    }

    private static User BuildCaller(int userId, Role role) => new()
    {
        UserId = userId,
        FirstNameEn = $"First{userId}",
        LastNameEn = $"Last{userId}",
        FirstNameAr = $"اول{userId}",
        LastNameAr = $"اخير{userId}",
        Role = role,
        RoleId = role.RoleId,
        EntityType = EntityType.BaytAlUrdon,
        EntityId = 1,
        IsActive = true
    };

    private static Country BuildCountry(int id) => new() { Id = id, EnglishName = "Jordan", ArabicName = "الأردن", IsActive = true };

    private static City BuildCity(int id, Country country) => new() { Id = id, CountryId = country.Id, Country = country, EnglishName = "Amman", ArabicName = "عمان", IsActive = true };

    private static ProductionCompany BuildCompany(int id, City city, bool isActive = true) => new()
    {
        Id = id,
        EnglishName = $"Company {id}",
        ArabicName = $"شركة {id}",
        RegistrationNumber = $"REG-{id}",
        CityId = city.Id,
        City = city,
        IsActive = isActive,
        IsSelfRegistered = true
    };

    private static User BuildCompanyUser(int userId, int companyId, bool isActive = true) => new()
    {
        UserId = userId,
        FirstNameEn = $"First{userId}",
        LastNameEn = $"Last{userId}",
        FirstNameAr = $"اول{userId}",
        LastNameAr = $"اخير{userId}",
        Role = BuildRole(3, "ProductionHouse"),
        EntityType = EntityType.ProductionCompany,
        EntityId = companyId,
        IsActive = isActive
    };

    [Fact]
    public async Task GetAllAsync_CallerWithViewPermission_ReturnsMappedDtos()
    {
        var caller = BuildCaller(1, BuildRole(1, "Bayt-AlUrdon", ViewProductionCompanies));
        var country = BuildCountry(1);
        var city = BuildCity(1, country);
        var company = BuildCompany(10, city);
        var (service, _) = BuildService(caller, companies: [company]);

        var result = await service.GetAllAsync(caller.UserId, search: null, isDeleted: false);

        var dto = Assert.Single(result.Data!);
        Assert.Equal("Company 10", dto.EnglishName);
        Assert.Equal("REG-10", dto.RegistrationNumber);
        Assert.Equal("Jordan", dto.CountryEnglishName);
        Assert.Equal("Amman", dto.CityEnglishName);
        Assert.True(dto.IsActive);
        Assert.True(dto.IsSelfRegistered);
    }

    [Fact]
    public async Task GetAllAsync_CallerWithoutViewOrManage_ThrowsUnauthorizedAccessException()
    {
        var caller = BuildCaller(1, BuildRole(1, "NoAccess"));
        var (service, _) = BuildService(caller);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetAllAsync(caller.UserId, search: null, isDeleted: false));
    }

    [Fact]
    public async Task GetAllAsync_CallerWithManageOnly_Succeeds()
    {
        // Manage acts as a View substitute, same "Manage is a full CRUD+View superset" convention
        // AssociationService's own permission model already establishes.
        var caller = BuildCaller(1, BuildRole(1, "Manager", ManageProductionCompanies));
        var (service, _) = BuildService(caller);

        var result = await service.GetAllAsync(caller.UserId, search: null, isDeleted: false);

        Assert.Empty(result.Data!);
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ThrowsKeyNotFoundException()
    {
        var caller = BuildCaller(1, BuildRole(1, "Bayt-AlUrdon", ViewProductionCompanies));
        var (service, _) = BuildService(caller);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetByIdAsync(caller.UserId, 999));
    }

    [Fact]
    public async Task UpdateStatusAsync_CallerWithViewOnly_ThrowsUnauthorizedAccessException()
    {
        // Confirms View alone does NOT satisfy the Manage-only status-toggle check — the real
        // requirement behind MAYD-82's "Super Admin only, not ASEZA Admin" (ASEZA only ever holds
        // View for this module, never Manage — see ProductionCompanyService's own comment).
        var caller = BuildCaller(1, BuildRole(1, "ViewOnly", ViewProductionCompanies));
        var country = BuildCountry(1);
        var city = BuildCity(1, country);
        var company = BuildCompany(10, city);
        var (service, _) = BuildService(caller, companies: [company]);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.UpdateStatusAsync(caller.UserId, 10, new UpdateProductionCompanyStatusDto { IsActive = false }));
    }

    [Fact]
    public async Task UpdateStatusAsync_Deactivate_CascadesToActiveUsersUnderThatCompanyOnly()
    {
        var caller = BuildCaller(1, BuildRole(1, "Bayt-AlUrdon", ManageProductionCompanies));
        var country = BuildCountry(1);
        var city = BuildCity(1, country);
        var companyA = BuildCompany(10, city);
        var companyB = BuildCompany(11, city);
        var activeUserA = BuildCompanyUser(30, companyA.Id);
        var alreadyInactiveUserA = BuildCompanyUser(31, companyA.Id, isActive: false);
        var activeUserB = BuildCompanyUser(40, companyB.Id);

        var (service, _) = BuildService(caller, companies: [companyA, companyB], users: [activeUserA, alreadyInactiveUserA, activeUserB]);

        var result = await service.UpdateStatusAsync(caller.UserId, companyA.Id, new UpdateProductionCompanyStatusDto { IsActive = false });

        Assert.False(result.Data!.IsActive);
        Assert.False(companyA.IsActive);
        Assert.False(activeUserA.IsActive);
        Assert.False(alreadyInactiveUserA.IsActive);

        // A different company's users, and the company itself, must be completely untouched.
        Assert.True(companyB.IsActive);
        Assert.True(activeUserB.IsActive);
    }

    [Fact]
    public async Task UpdateStatusAsync_Reactivate_DoesNotCascadeToUsers()
    {
        // The deliberate asymmetry: reactivating the company only flips the company's own flag.
        // Users deactivated by the earlier cascade stay deactivated — each is reactivated
        // individually via the existing per-user Activate/Deactivate flow, not by this call.
        var caller = BuildCaller(1, BuildRole(1, "Bayt-AlUrdon", ManageProductionCompanies));
        var country = BuildCountry(1);
        var city = BuildCity(1, country);
        var company = BuildCompany(10, city, isActive: false);
        var cascadeDeactivatedUser = BuildCompanyUser(30, company.Id, isActive: false);

        var (service, _) = BuildService(caller, companies: [company], users: [cascadeDeactivatedUser]);

        var result = await service.UpdateStatusAsync(caller.UserId, company.Id, new UpdateProductionCompanyStatusDto { IsActive = true });

        Assert.True(result.Data!.IsActive);
        Assert.True(company.IsActive);
        Assert.False(cascadeDeactivatedUser.IsActive);
    }

    [Fact]
    public async Task UpdateStatusAsync_UnknownId_ThrowsKeyNotFoundException()
    {
        var caller = BuildCaller(1, BuildRole(1, "Bayt-AlUrdon", ManageProductionCompanies));
        var (service, _) = BuildService(caller);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.UpdateStatusAsync(caller.UserId, 999, new UpdateProductionCompanyStatusDto { IsActive = false }));
    }

    private static (ProductionCompanyService Service, FakeProductionCompanyRepository Repository) BuildService(
        User caller, ProductionCompany[]? companies = null, User[]? users = null)
    {
        var userRepository = new FakeUserRepository(caller, users);
        var companyRepository = new FakeProductionCompanyRepository(companies ?? []);

        var unitOfWork = new FakeUnitOfWork(userRepository, companyRepository);
        return (new ProductionCompanyService(unitOfWork), companyRepository);
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly User _currentUser;
        private readonly List<User> _users;

        public FakeUserRepository(User currentUser, IEnumerable<User>? users = null)
        {
            _currentUser = currentUser;
            _users = (users ?? Enumerable.Empty<User>()).ToList();
        }

        public Task<User?> GetWithPermissionsAsync(int userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(userId == _currentUser.UserId ? _currentUser : null);

        public Task<List<User>> GetByEntityAsync(EntityType entityType, int entityId, string? search, CancellationToken cancellationToken = default) =>
            Task.FromResult(_users.Where(u => !u.IsDeleted && u.EntityType == entityType && u.EntityId == entityId).ToList());

        public Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByEmailWithAccessAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<User>> GetDeletedByEntityAsync(EntityType entityType, int entityId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(List<User> Users, int TotalCount)> GetPagedByEntityAsync(EntityType entityType, int entityId, string? search, bool? isActive, int page, int pageSize, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetDetailsAsync(int userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetDetailsReadOnlyAsync(int userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByUserNameEnAsync(User user, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByUserNameArAsync(User user, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<User>> GetByIdsInEntityAsync(IEnumerable<int> userIds, EntityType entityType, int entityId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddAsync(User user, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public void Remove(User user) => throw new NotSupportedException();
    }

    private sealed class FakeProductionCompanyRepository : IProductionCompanyRepository
    {
        private readonly Dictionary<int, ProductionCompany> _companiesById;

        public FakeProductionCompanyRepository(IEnumerable<ProductionCompany> companies) => _companiesById = companies.ToDictionary(c => c.Id);

        public Task<ProductionCompany?> GetByIdAsync(int productionCompanyId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_companiesById.GetValueOrDefault(productionCompanyId));

        public Task<List<ProductionCompany>> QueryAsync(bool isDeleted, string? searchTerm = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(_companiesById.Values.Where(c => c.IsDeleted == isDeleted).ToList());

        public Task<List<ProductionCompany>> GetAllAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> RegistrationNumberExistsAsync(string registrationNumber, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> EnglishNameExistsAsync(string englishName, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddAsync(ProductionCompany productionCompany, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public void Remove(ProductionCompany productionCompany) => throw new NotSupportedException();
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public FakeUnitOfWork(IUserRepository users, IProductionCompanyRepository productionCompanies)
        {
            Users = users;
            ProductionCompanies = productionCompanies;
        }

        public IUserRepository Users { get; }
        public IProductionCompanyRepository ProductionCompanies { get; }
        public IRoleRepository Roles => throw new NotSupportedException();
        public IPermissionRepository Permissions => throw new NotSupportedException();
        public IGroupRepository Groups => throw new NotSupportedException();
        public IProjectTypeRepository ProjectTypes => throw new NotSupportedException();
        public ICountryRepository Countries => throw new NotSupportedException();
        public ICityRepository Cities => throw new NotSupportedException();
        public ICityLocationRepository CityLocations => throw new NotSupportedException();
        public IAssociationRepository Associations => throw new NotSupportedException();
        public IAssociationProjectSupervisorRepository AssociationProjectSupervisors => throw new NotSupportedException();
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
