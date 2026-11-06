namespace Maydan.Application.Interfaces;

public interface IUnitOfWork
{
    IUserRepository Users { get; }
    IRoleRepository Roles { get; }
    IPermissionRepository Permissions { get; }
    IGroupRepository Groups { get; }
    IProjectTypeRepository ProjectTypes { get; }
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
    IServiceRequestRepository ServiceRequests => throw new NotImplementedException();
    ILookupRepository Lookups => throw new NotImplementedException();
    IWorkerProjectLinkRepository WorkerProjectLinks => throw new NotImplementedException();
    IWorkerSuggestionRuleRepository WorkerSuggestionRules => throw new NotImplementedException();

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default);
}
