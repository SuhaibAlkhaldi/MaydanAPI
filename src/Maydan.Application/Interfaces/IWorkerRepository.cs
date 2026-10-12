using Maydan.Application.DTOs.Workers;
using Maydan.Domain.Entities;

namespace Maydan.Application.Interfaces;

public interface IWorkerRepository
{
    Task<Worker?> GetByIdAsync(int workerId, CancellationToken cancellationToken = default);
    Task<Worker?> GetByIdWithServicesAsync(int workerId, CancellationToken cancellationToken = default);
    Task<Worker?> GetByIdIncludingDeletedAsync(int workerId, CancellationToken cancellationToken = default);
    Task<Worker?> GetByCivilIdHashAsync(string civilIdHash, CancellationToken cancellationToken = default);
    Task<List<Worker>> GetByAssociationIdAsync(int associationId, CancellationToken cancellationToken = default);
    Task<List<Worker>> GetDeletedByAssociationIdAsync(int associationId, CancellationToken cancellationToken = default);

    Task AddAsync(Worker worker, CancellationToken cancellationToken = default);
    void Remove(Worker worker);

    Task<(List<WorkerSummaryDto> Items, int TotalCount)> GetAllProjectedAsync(
        int? associationId, string? search, int? serviceId, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<WorkerDto?> GetByIdProjectedAsync(int workerId, CancellationToken cancellationToken = default);
}
