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
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Project project,
        CancellationToken cancellationToken = default);

    void Remove(Project project);
}
