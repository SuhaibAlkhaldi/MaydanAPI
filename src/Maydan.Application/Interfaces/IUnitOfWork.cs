namespace Maydan.Application.Interfaces;

public interface IUnitOfWork
{
    IUserRepository Users { get; }
    IRoleRepository Roles { get; }
    IPermissionRepository Permissions { get; }
    IGroupRepository Groups { get; }
    IProjectTypeRepository ProjectTypes { get; }
    ICountryRepository Countries { get; }
    ICityRepository Cities { get; }
    ICityLocationRepository CityLocations { get; }
    IAssociationRepository Associations { get; }
    IAssociationProjectSupervisorRepository AssociationProjectSupervisors { get; }
    IProductionCompanyRepository ProductionCompanies { get; }
    IProjectRepository Projects { get; }
    IWorkerRepository Workers { get; }
    IPasswordResetTokenRepository PasswordResetTokens { get; }
    IRefreshTokenRepository RefreshTokens { get; }
    ISystemConfigurationRepository SystemConfigurations { get; }
    IServiceRepository Services { get; }
    // Optional default implementation so existing test fakes don't need to implement it.
    IServiceRequestRepository ServiceRequests => throw new NotImplementedException();

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    // Entity onboarding Stage 1 (2026-09-22): User.EntityId is a soft FK (no EF navigation, see
    // User.cs) — EF can't auto-populate it the way it does a real FK, so creating a brand-new
    // entity plus its first user needs two SaveChangesAsync calls (one to obtain the entity's
    // generated Id before the user row referencing it can be built). Wrapped here so that pair
    // stays atomic without leaking EF's transaction type into the Application layer.
    Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default);
}
