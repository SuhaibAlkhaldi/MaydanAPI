using Maydan.Domain.Entities;

namespace Maydan.Application.Interfaces;

public interface IProjectRepository
{
    Task<Project?> GetByIdAsync(
        int projectId,
        CancellationToken cancellationToken = default);


    Task<Project?> GetByIdIncludingDeletedAsync(
        int projectId,
        CancellationToken cancellationToken = default);

    Task<List<Project>> GetByProductionCompanyIdAsync(
        int productionCompanyId,
        CancellationToken cancellationToken = default);


    Task<List<Project>> GetAllAsync(
        bool isDeleted,
        string? searchTerm,
        DateTime? startDate,
        DateTime? endDate,
        string? searchByProductionCompanyName,
        int? searchByProductionCompanyId,
        int? cityId,
        int? cityLocationId,
        bool?IsActive,
        CancellationToken cancellationToken = default);

    Task<bool> HasLocationManagerOverlapAsync(
    int locationManagerUserId,
    DateTime projectStartDate,
    DateTime projectEndDate,
    int? excludedProjectId,
    CancellationToken cancellationToken = default);

    Task AddAsync(
        Project project,
        CancellationToken cancellationToken = default);

    void Remove(Project project);


    Task<string?> CheckOverlapConflictsAsync(
    DateTime startDate,
    DateTime endDate,
    int producerId,
    int locationManagerId,
    int cityLocationId,
    int? excludeProjectId = null,
    CancellationToken cancellationToken = default);

    Task<List<int>> GetBusyUserIdsAsync(int productionCompanyId, DateTime startDate, DateTime endDate, int? excludeProjectId = null, CancellationToken cancellationToken = default);
}
