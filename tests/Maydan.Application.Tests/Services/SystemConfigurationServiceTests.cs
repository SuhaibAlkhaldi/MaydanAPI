using Maydan.Application.DTOs.SystemConfiguration;
using Maydan.Application.Interfaces;
using Maydan.Application.Services;
using Maydan.Domain.Entities;
using Maydan.Domain.Enums;

namespace Maydan.Application.Tests.Services;

// System Configuration gate (MAYD-133, 2026-09-24): the admin screen's own service. Proves the
// dual EntityType+permission gate (same shape as EntityOnboardingServiceTests), the "at most one
// row, computed IsConfigured" model, that the raw SMTP password never comes back out through the
// DTO, and that a blank password on an update keeps the previously-protected one instead of wiping
// it (the frontend can never round-trip a value it was never given).
public class SystemConfigurationServiceTests
{
    private static Role BaytAlUrdonRole(bool withPermission)
    {
        var role = new Role { RoleId = 1, RoleNameEn = "Bayt-AlUrdon", RoleNameAr = "بيت الأردن" };
        var permissionId = withPermission ? 38 : 1;
        var permission = new Permission { PermissionId = permissionId, PermissionNameEn = withPermission ? "Manage System Configuration" : "View Users", Module = "x", IsActive = true };
        role.RolePermissions.Add(new RolePermission { RoleId = 1, Role = role, PermissionId = permissionId, Permission = permission, IsActive = true });
        return role;
    }

    private static User SuperAdmin(bool withPermission = true) => new()
    {
        UserId = 1,
        RoleId = 1,
        EntityType = EntityType.BaytAlUrdon,
        IsActive = true,
        Role = BaytAlUrdonRole(withPermission)
    };

    private static User NonSuperAdmin()
    {
        var role = new Role { RoleId = 2, RoleNameEn = "ASEZA", RoleNameAr = "أسيزا" };
        return new User { UserId = 7, RoleId = 2, EntityType = EntityType.Aseza, IsActive = true, Role = role };
    }

    private static UpdateSystemConfigurationDto ValidDto(string? password = "s3cret") => new()
    {
        SmtpHost = "smtp.example.org",
        SmtpPort = 587,
        SmtpUsername = "no-reply@example.org",
        SmtpPassword = password,
        SenderEmail = "no-reply@example.org",
        SenderDisplayName = "Maydan"
    };

    [Fact]
    public async Task GetAsync_rejects_a_caller_who_is_not_BaytAlUrdon()
    {
        var service = BuildService(NonSuperAdmin(), out _, out _);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetAsync(7));
    }

    [Fact]
    public async Task GetAsync_rejects_a_BaytAlUrdon_caller_missing_the_dedicated_permission()
    {
        var service = BuildService(SuperAdmin(withPermission: false), out _, out _);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetAsync(1));
    }

    [Fact]
    public async Task GetAsync_with_no_row_yet_reports_not_configured_and_no_password()
    {
        var service = BuildService(SuperAdmin(), out _, out _);

        var dto = await service.GetAsync(1);

        Assert.False(dto.IsConfigured);
        Assert.False(dto.HasSmtpPassword);
    }

    [Fact]
    public async Task UpdateAsync_first_save_requires_a_password()
    {
        var service = BuildService(SuperAdmin(), out _, out _);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateAsync(1, ValidDto(password: null)));
    }

    [Fact]
    public async Task UpdateAsync_saves_and_reports_configured_without_ever_returning_the_raw_password()
    {
        var service = BuildService(SuperAdmin(), out var repository, out var protector);

        var dto = await service.UpdateAsync(1, ValidDto(password: "s3cret"));

        Assert.True(dto.IsConfigured);
        Assert.True(dto.HasSmtpPassword);
        Assert.DoesNotContain("s3cret", dto.ToString());

        var stored = await repository.GetAsync();
        Assert.NotNull(stored);
        Assert.Equal(protector.Protect("s3cret"), stored!.SmtpPasswordProtected);
    }

    [Fact]
    public async Task UpdateAsync_with_a_blank_password_keeps_the_previously_protected_one()
    {
        var service = BuildService(SuperAdmin(), out var repository, out var protector);
        await service.UpdateAsync(1, ValidDto(password: "first-real-password"));

        var secondSave = ValidDto(password: null);
        secondSave.SmtpHost = "smtp.new-host.example.org";
        var dto = await service.UpdateAsync(1, secondSave);

        Assert.Equal("smtp.new-host.example.org", dto.SmtpHost);
        Assert.True(dto.HasSmtpPassword);

        var stored = await repository.GetAsync();
        Assert.Equal(protector.Protect("first-real-password"), stored!.SmtpPasswordProtected);
    }

    [Fact]
    public async Task IsConfiguredAsync_is_false_until_every_required_field_is_present()
    {
        var service = BuildService(SuperAdmin(), out var repository, out _);

        Assert.False(await service.IsConfiguredAsync());

        await service.UpdateAsync(1, ValidDto());

        Assert.True(await service.IsConfiguredAsync());
    }

    private static SystemConfigurationService BuildService(User currentUser, out FakeSystemConfigurationRepository repository, out FakeSecretProtector protector)
    {
        repository = new FakeSystemConfigurationRepository();
        protector = new FakeSecretProtector();
        var unitOfWork = new FakeUnitOfWork(new FakeUserRepository(currentUser), repository);
        return new SystemConfigurationService(unitOfWork, protector);
    }

    private sealed class FakeSecretProtector : ISecretProtector
    {
        public string Protect(string plaintext) => $"protected:{plaintext}";
        public string Unprotect(string protectedValue) => protectedValue.Replace("protected:", string.Empty);
    }

    private sealed class FakeSystemConfigurationRepository : ISystemConfigurationRepository
    {
        private SystemConfiguration? _configuration;

        public Task<SystemConfiguration?> GetAsync(CancellationToken cancellationToken = default) => Task.FromResult(_configuration);

        public Task AddAsync(SystemConfiguration configuration, CancellationToken cancellationToken = default)
        {
            _configuration = configuration;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly User _user;

        public FakeUserRepository(User user) => _user = user;

        public Task<User?> GetWithPermissionsAsync(int userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(userId == _user.UserId ? _user : null);

        public Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByEmailWithAccessAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<User>> GetByEntityAsync(EntityType entityType, int entityId, string? search, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<User>> GetDeletedByEntityAsync(EntityType entityType, int entityId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(List<User> Users, int TotalCount)> GetPagedByEntityAsync(EntityType entityType, int entityId, string? search, bool? isActive, int page, int pageSize, CancellationToken cancellationToken = default) => throw new NotSupportedException();
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
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public FakeUnitOfWork(IUserRepository users, ISystemConfigurationRepository systemConfigurations)
        {
            Users = users;
            SystemConfigurations = systemConfigurations;
        }

        public IUserRepository Users { get; }
        public ISystemConfigurationRepository SystemConfigurations { get; }
        public IServiceRepository Services => throw new NotSupportedException();
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



        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
        public Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default) => operation();
    }
}
