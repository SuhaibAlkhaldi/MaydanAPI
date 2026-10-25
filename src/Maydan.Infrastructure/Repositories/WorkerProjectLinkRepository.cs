using Maydan.Application.Interfaces;
using Maydan.Domain.Entities;
using Maydan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Maydan.Infrastructure.Repositories;

public class WorkerProjectLinkRepository : IWorkerProjectLinkRepository
{
    private readonly MaydanDbContext _db;

    public WorkerProjectLinkRepository(MaydanDbContext db) => _db = db;

    public Task<WorkerProjectLink?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _db.WorkerProjectLinks
           .Include(x => x.Worker)
           .Include(x => x.Project)
           .Include(x => x.ServiceRequest)
               .ThenInclude(sr => sr.Service)
           .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<WorkerProjectLink?> GetByProjectAndWorkerAsync(int projectId, int workerId, CancellationToken cancellationToken = default) =>
        _db.WorkerProjectLinks.FirstOrDefaultAsync(x => x.ProjectId == projectId && x.WorkerId == workerId && x.IsWorkerActive, cancellationToken);
    public async Task<IReadOnlyList<WorkerProjectLink>> ListByProjectAsync(int projectId, CancellationToken cancellationToken = default) =>
        await _db.WorkerProjectLinks
                 .Where(x => x.ProjectId == projectId && x.IsWorkerActive)
                 .Include(x => x.Worker)
                 .Include(x => x.ServiceRequest)
                     .ThenInclude(sr => sr.Service)
                 .Include(x => x.ServiceRequest)
                     .ThenInclude(sr => sr.Association)
                 .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<WorkerProjectLink>> ListByWorkerAsync(int workerId, CancellationToken cancellationToken = default) =>
        await _db.WorkerProjectLinks
                 .Where(x => x.WorkerId == workerId && x.IsWorkerActive)
                 .Include(x => x.Project)
                 .Include(x => x.ServiceRequest)
                     .ThenInclude(sr => sr.Service)
                 .ToListAsync(cancellationToken);

    public async Task AddAsync(WorkerProjectLink link, CancellationToken cancellationToken = default) =>
        await _db.WorkerProjectLinks.AddAsync(link, cancellationToken);

    public Task UpdateAsync(WorkerProjectLink link, CancellationToken cancellationToken = default)
    {
        _db.WorkerProjectLinks.Update(link);
        return Task.CompletedTask;
    }

    public Task DeactivateAsync(WorkerProjectLink link, CancellationToken cancellationToken = default)
    {
        link.IsWorkerActive = false;
        link.UpdatedDate = DateTime.UtcNow;
        _db.WorkerProjectLinks.Update(link);
        return Task.CompletedTask;
    }
}
