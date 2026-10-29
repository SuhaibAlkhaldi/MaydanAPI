using Maydan.Application.DTOs.Auth;
using Maydan.Application.Interfaces;
using Maydan.Application.Services;
using Maydan.Domain.Entities;
using Maydan.Domain.Enums;

namespace Maydan.Application.Tests.Services;

// Entity onboarding Stage 1 (2026-09-22): proves the public production-company self-registration
// endpoint creates the company + its first admin together, wires EntityType/EntityId correctly
// (the soft-FK case IUnitOfWork.ExecuteInTransactionAsync exists for), and grants the admin every
// permission the ProductionHouse role allows — not a hand-picked subset — while rejecting the
// duplicate-email/duplicate-registration-number cases without creating anything.
public class ProductionCompanyOnboardingServiceTests
{
    private static RegisterProductionCompanyDto ValidDto() => new(
        CompanyNameEn: "Acme Productions",
        CompanyNameAr: "أكمي للإنتاج",
        RegistrationNumber: "REG-12345",
        CityId: 1,
        AdminFirstName: "Sam",
        AdminLastName: "Carter",
        AdminFirstNameAr: "سام",
        AdminLastNameAr: "كارتر",
        MobileCountryCode: "+962",
        MobileNumber: "799999999",
        Email: "sam.carter@example.org",
        Password: "P@ssw0rd!");

    private static Role ProductionHouseRoleWithPermissions()
    {
        var role = new Role { RoleId = 3, RoleNameEn = "ProductionHouse", RoleNameAr = "شركة الإنتاج" };

        var viewProjects = new Permission { PermissionId = 25, PermissionNameEn = "View Projects", Module = "Projects", IsActive = true };
        var manageAttendance = new Permission { PermissionId = 34, PermissionNameEn = "Manage Attendance", Module = "Attendance", IsActive = true };
        var inactivePermission = new Permission { PermissionId = 99, PermissionNameEn = "Retired Permission", Module = "Legacy", IsActive = false };

        role.RolePermissions.Add(new RolePermission { RoleId = 3, Role = role, PermissionId = viewProjects.PermissionId, Permission = viewProjects, IsActive = true });
        role.RolePermissions.Add(new RolePermission { RoleId = 3, Role = role, PermissionId = manageAttendance.PermissionId, Permission = manageAttendance, IsActive = true });
        // Inactive RolePermission row — must NOT be granted to the new admin.
        role.RolePermissions.Add(new RolePermission { RoleId = 3, Role = role, PermissionId = inactivePermission.PermissionId, Permission = inactivePermission, IsActive = false });

        return role;
    }

    [Fact]
    public async Task RegisterAsync_ValidRequest_CreatesCompanyAndFullAdminTogether()
    {
        var role = ProductionHouseRoleWithPermissions();
        var userRepository = new FakeUserRepository();
        var companyRepository = new FakeProductionCompanyRepository();
        var unitOfWork = new FakeUnitOfWork(userRepository, companyRepository, new FakeRoleRepository(role), new FakeCityRepository(cityExists: true));
        var service = new ProductionCompanyOnboardingService(unitOfWork, new FakePasswordHasher());

        var result = await service.RegisterAsync(ValidDto());

        Assert.NotNull(companyRepository.AddedCompany);
        Assert.True(companyRepository.AddedCompany!.IsSelfRegistered);
        Assert.True(companyRepository.AddedCompany.IsActive);
        Assert.Equal("REG-12345", companyRepository.AddedCompany.RegistrationNumber);
        Assert.Equal(1, companyRepository.AddedCompany.CityId);

        Assert.NotNull(userRepository.AddedUser);
        var user = userRepository.AddedUser!;
        Assert.Equal(EntityType.ProductionCompany, user.EntityType);
        Assert.Equal(companyRepository.AddedCompany.Id, user.EntityId);
        Assert.Equal(3, user.RoleId);
        Assert.False(user.MustResetPassword);
        Assert.True(user.IsActive);
        Assert.Equal("hashed:P@ssw0rd!", user.PasswordHash);
        Assert.Equal("Sam", user.FirstNameEn);
        Assert.Equal("Carter", user.LastNameEn);
        Assert.Equal("سام", user.FirstNameAr);
        Assert.Equal("كارتر", user.LastNameAr);

        // Full role permission set, not a hand-picked subset — and the inactive row excluded.
        var grantedPermissionIds = user.UserPermissions.Select(up => up.PermissionId).OrderBy(id => id).ToList();
        Assert.Equal(new[] { 25, 34 }, grantedPermissionIds);

        Assert.Equal(companyRepository.AddedCompany.Id, result.Data!.ProductionCompanyId);
        Assert.Equal(user.UserId, result.Data!.AdminUserId);
    }

    [Fact]
    public async Task RegisterAsync_ArabicNameStoredAsGiven_NotDerivedFromLatinName()
    {
        // Stage 3 regression guard: Stage 1's stopgap duplicated the Latin name into
        // FirstNameAr/LastNameAr. Using values that share no characters with their Latin
        // counterparts makes any regression back to that behavior fail loudly.
        var dto = ValidDto() with { AdminFirstNameAr = "محمد", AdminLastNameAr = "العبدالله" };

        var role = ProductionHouseRoleWithPermissions();
        var userRepository = new FakeUserRepository();
        var companyRepository = new FakeProductionCompanyRepository();
        var unitOfWork = new FakeUnitOfWork(userRepository, companyRepository, new FakeRoleRepository(role), new FakeCityRepository(cityExists: true));
        var service = new ProductionCompanyOnboardingService(unitOfWork, new FakePasswordHasher());

        await service.RegisterAsync(dto);

        var user = userRepository.AddedUser!;
        Assert.Equal("محمد", user.FirstNameAr);
        Assert.Equal("العبدالله", user.LastNameAr);
        Assert.Equal("Sam", user.FirstNameEn);
        Assert.Equal("Carter", user.LastNameEn);
        Assert.NotEqual(user.FirstNameEn, user.FirstNameAr);
        Assert.NotEqual(user.LastNameEn, user.LastNameAr);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ThrowsAndCreatesNothing()
    {
        var userRepository = new FakeUserRepository(emailExists: true);
        var companyRepository = new FakeProductionCompanyRepository();
        var unitOfWork = new FakeUnitOfWork(userRepository, companyRepository, new FakeRoleRepository(ProductionHouseRoleWithPermissions()), new FakeCityRepository(cityExists: true));
        var service = new ProductionCompanyOnboardingService(unitOfWork, new FakePasswordHasher());

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RegisterAsync(ValidDto()));

        Assert.Null(companyRepository.AddedCompany);
        Assert.Null(userRepository.AddedUser);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateRegistrationNumber_ThrowsAndCreatesNothing()
    {
        var userRepository = new FakeUserRepository();
        var companyRepository = new FakeProductionCompanyRepository(registrationNumberExists: true);
        var unitOfWork = new FakeUnitOfWork(userRepository, companyRepository, new FakeRoleRepository(ProductionHouseRoleWithPermissions()), new FakeCityRepository(cityExists: true));
        var service = new ProductionCompanyOnboardingService(unitOfWork, new FakePasswordHasher());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.RegisterAsync(ValidDto()));

        Assert.Contains("registration number", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Null(companyRepository.AddedCompany);
        Assert.Null(userRepository.AddedUser);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEnglishName_ThrowsAndCreatesNothing()
    {
        // MAYD-79 gap fix: CompanyNameEn ("must be unique" per the ticket's own text) had no
        // uniqueness check at all before this phase — only Email and RegistrationNumber did.
        var userRepository = new FakeUserRepository();
        var companyRepository = new FakeProductionCompanyRepository(englishNameExists: true);
        var unitOfWork = new FakeUnitOfWork(userRepository, companyRepository, new FakeRoleRepository(ProductionHouseRoleWithPermissions()), new FakeCityRepository(cityExists: true));
        var service = new ProductionCompanyOnboardingService(unitOfWork, new FakePasswordHasher());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.RegisterAsync(ValidDto()));

        Assert.Contains("English name", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Null(companyRepository.AddedCompany);
        Assert.Null(userRepository.AddedUser);
    }

    [Fact]
    public async Task RegisterAsync_CityNotFound_ThrowsAndCreatesNothing()
    {
        var userRepository = new FakeUserRepository();
        var companyRepository = new FakeProductionCompanyRepository();
        var unitOfWork = new FakeUnitOfWork(userRepository, companyRepository, new FakeRoleRepository(ProductionHouseRoleWithPermissions()), new FakeCityRepository(cityExists: false));
        var service = new ProductionCompanyOnboardingService(unitOfWork, new FakePasswordHasher());

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.RegisterAsync(ValidDto()));

        Assert.Null(companyRepository.AddedCompany);
        Assert.Null(userRepository.AddedUser);
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly bool _emailExists;

        public FakeUserRepository(bool emailExists = false) => _emailExists = emailExists;

        public User? AddedUser { get; private set; }

        public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default) =>
            Task.FromResult(_emailExists);
        public Task<List<User>> GetAssociationSupervisorsAsync(EntityType entityType, int? entityId = null, CancellationToken cancellationToken = default)
    => Task.FromResult(new List<User>());
        public Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            user.UserId = 200;
            AddedUser = user;
            return Task.CompletedTask;
        }

        public Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByEmailWithAccessAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<User>> GetByEntityAsync(EntityType entityType, int entityId, string? search, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<User>> GetDeletedByEntityAsync(EntityType entityType, int entityId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(List<User> Users, int TotalCount)> GetPagedByEntityAsync(EntityType entityType, int entityId, string? search, bool? isActive, int page, int pageSize, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetDetailsAsync(int userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetDetailsReadOnlyAsync(int userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByUserNameEnAsync(User user, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByUserNameArAsync(User user, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetWithPermissionsAsync(int userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<User>> GetByIdsInEntityAsync(IEnumerable<int> userIds, EntityType entityType, int entityId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
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

    private sealed class FakeProductionCompanyRepository : IProductionCompanyRepository
    {
        private readonly bool _registrationNumberExists;
        private readonly bool _englishNameExists;

        public FakeProductionCompanyRepository(bool registrationNumberExists = false, bool englishNameExists = false)
        {
            _registrationNumberExists = registrationNumberExists;
            _englishNameExists = englishNameExists;
        }

        public ProductionCompany? AddedCompany { get; private set; }

        public Task<bool> RegistrationNumberExistsAsync(string registrationNumber, CancellationToken cancellationToken = default) =>
            Task.FromResult(_registrationNumberExists);

        public Task<bool> EnglishNameExistsAsync(string englishName, CancellationToken cancellationToken = default) =>
            Task.FromResult(_englishNameExists);

        public Task AddAsync(ProductionCompany productionCompany, CancellationToken cancellationToken = default)
        {
            productionCompany.Id = 501;
            AddedCompany = productionCompany;
            return Task.CompletedTask;
        }

        public Task<ProductionCompany?> GetByIdAsync(int productionCompanyId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<ProductionCompany>> GetAllAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<ProductionCompany>> QueryAsync(bool isDeleted, string? searchTerm = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public void Remove(ProductionCompany productionCompany) => throw new NotSupportedException();
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

    private sealed class FakeCityRepository : ICityRepository
    {
        private readonly bool _cityExists;

        public FakeCityRepository(bool cityExists) => _cityExists = cityExists;

        public Task<City?> GetByIdAsync(int cityId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_cityExists ? new City { Id = cityId, EnglishName = "Amman" } : null);

        public Task<City?> GetByIdIncludingDeletedAsync(int cityId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<City>> GetByCountryIdAsync(int countryId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddAsync(City city, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public void Remove(City city) => throw new NotSupportedException();
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string HashPassword(string password) => $"hashed:{password}";
        public bool VerifyPassword(string password, string passwordHash) => passwordHash == $"hashed:{password}";
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public FakeUnitOfWork(IUserRepository users, IProductionCompanyRepository productionCompanies, IRoleRepository roles, ICityRepository cities)
        {
            Users = users;
            ProductionCompanies = productionCompanies;
            Roles = roles;
            Cities = cities;
        }

        public IUserRepository Users { get; }
        public IProductionCompanyRepository ProductionCompanies { get; }
        public IRoleRepository Roles { get; }
        public ICityRepository Cities { get; }
        public IServiceRepository Services => throw new NotSupportedException();
        public ICityLocationRepository CityLocations => throw new NotSupportedException();
        public IAssociationProjectSupervisorRepository AssociationProjectSupervisors => throw new NotSupportedException();
        public IPermissionRepository Permissions => throw new NotSupportedException();
        public IGroupRepository Groups => throw new NotSupportedException();
        public IProjectTypeRepository ProjectTypes => throw new NotSupportedException();
        public ICountryRepository Countries => throw new NotSupportedException();
        public IAssociationRepository Associations => throw new NotSupportedException();
        public IProjectRepository Projects => throw new NotSupportedException();
        public IWorkerRepository Workers => throw new NotSupportedException();
        public IPasswordResetTokenRepository PasswordResetTokens => throw new NotSupportedException();
        public IRefreshTokenRepository RefreshTokens => throw new NotSupportedException();
        public ISystemConfigurationRepository SystemConfigurations => throw new NotSupportedException();



        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);

        public Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default) => operation();
    }
}
