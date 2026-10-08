using Maydan.Application.Interfaces;
using Maydan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Maydan.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly MaydanDbContext _context;

    private IUserRepository? _users;
    private IRoleRepository? _roles;
    private IPermissionRepository? _permissions;
    private IGroupRepository? _groups;
    private IProjectTypeRepository? _projectTypes;
    private ICountryRepository? _countries;
    private ICityRepository? _cities;
    private ICityLocationRepository? _cityLocations;
    private IAssociationRepository? _associations;
    private IAssociationProjectSupervisorRepository? _associationProjectSupervisors;
    private IProductionCompanyRepository? _productionCompanies;
    private IProjectRepository? _projects;
    private IWorkerRepository? _workers;
    private IPasswordResetTokenRepository? _passwordResetTokens;
    private IRefreshTokenRepository? _refreshTokens;
    private ISystemConfigurationRepository? _systemConfigurations;
    private IServiceRepository? _services;
    private IServiceRequestRepository? _serviceRequests;

    public UnitOfWork(MaydanDbContext context)
    {
        _context = context;
    }

    public IUserRepository Users => _users ??= new UserRepository(_context);
    public IRoleRepository Roles => _roles ??= new RoleRepository(_context);
    public IPermissionRepository Permissions => _permissions ??= new PermissionRepository(_context);
    public IGroupRepository Groups => _groups ??= new GroupRepository(_context);
    public ICountryRepository Countries => _countries ??= new CountryRepository(_context);
    public ICityRepository Cities => _cities ??= new CityRepository(_context);
    public ICityLocationRepository CityLocations => _cityLocations ??= new CityLocationRepository(_context);
    public IAssociationRepository Associations => _associations ??= new AssociationRepository(_context);
    public IAssociationProjectSupervisorRepository AssociationProjectSupervisors => _associationProjectSupervisors ??= new AssociationProjectSupervisorRepository(_context);
    public IProductionCompanyRepository ProductionCompanies => _productionCompanies ??= new ProductionCompanyRepository(_context);
    public IProjectRepository Projects => _projects ??= new ProjectRepository(_context);
    public IWorkerRepository Workers => _workers ??= new WorkerRepository(_context);
    public IPasswordResetTokenRepository PasswordResetTokens => _passwordResetTokens ??= new PasswordResetTokenRepository(_context);
    public IRefreshTokenRepository RefreshTokens => _refreshTokens ??= new RefreshTokenRepository(_context);
    public ISystemConfigurationRepository SystemConfigurations => _systemConfigurations ??= new SystemConfigurationRepository(_context);
    public IServiceRepository Services => _services ??= new ServiceRepository(_context);
    public IServiceRequestRepository ServiceRequests => _serviceRequests ??= new ServiceRequestRepository(_context);
    public IProjectTypeRepository ProjectTypes =>
    _projectTypes ??= new ProjectTypeRepository(_context);
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);

    public async Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        await operation();
        await transaction.CommitAsync(cancellationToken);
    }
}
