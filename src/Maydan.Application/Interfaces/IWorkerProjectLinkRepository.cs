using Maydan.Domain.Entities;

namespace Maydan.Application.Interfaces;

public interface IWorkerProjectLinkRepository
{
    Task<WorkerProjectLink?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<WorkerProjectLink?> GetByProjectAndWorkerAsync(int projectId, int workerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkerProjectLink>> ListByProjectAsync(int projectId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkerProjectLink>> ListByWorkerAsync(int workerId, CancellationToken cancellationToken = default);
    Task AddAsync(WorkerProjectLink link, CancellationToken cancellationToken = default);
    Task UpdateAsync(WorkerProjectLink link, CancellationToken cancellationToken = default);

    //Soft-deactivates the link (sets IsWorkerActive = false). Does not delete the row.
    Task DeactivateAsync(WorkerProjectLink link, CancellationToken cancellationToken = default);
}
