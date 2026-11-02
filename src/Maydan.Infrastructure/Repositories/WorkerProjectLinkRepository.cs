using Maydan.Application.DTOs.Projects;
using Maydan.Application.Interfaces;
using Maydan.Domain.Entities;
using Maydan.Domain.Enums;
using Maydan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Maydan.Infrastructure.Repositories;

public class WorkerProjectLinkRepository : IWorkerProjectLinkRepository
{
    private readonly MaydanDbContext _db;
    // Jordan is UTC+3
    private static DateTime Today => DateTime.UtcNow.AddHours(3).Date;

    public WorkerProjectLinkRepository(MaydanDbContext db) => _db = db;

    //public Task<WorkerProjectLink?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
    //    _db.WorkerProjectLinks
    //       .Include(x => x.Worker)
    //       .Include(x => x.Project)
    //       .Include(x => x.ServiceRequest)
    //           .ThenInclude(sr => sr.Service)
    //       .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<WorkerProjectLink?> GetByProjectAndWorkerAsync(int projectId, int workerId, CancellationToken cancellationToken = default) =>
        _db.WorkerProjectLinks.FirstOrDefaultAsync(x => x.ProjectId == projectId && x.WorkerId == workerId && x.IsWorkerActive, cancellationToken);
    //public async Task<IReadOnlyList<WorkerProjectLink>> ListByProjectAsync(int projectId, CancellationToken cancellationToken = default) =>
    //    await _db.WorkerProjectLinks
    //        .AsNoTracking()
    //        .Where(x => x.ProjectId == projectId)
    //        .Include(x => x.Worker)
    //        .Include(x => x.ServiceRequest).ThenInclude(sr => sr.Service)
    //        .Include(x => x.ServiceRequest).ThenInclude(sr => sr.Association)
    //        .OrderByDescending(x => x.CreatedDate)
    //        .ToListAsync(cancellationToken);
    public async Task<IReadOnlyList<WorkerProjectLink>> ListByWorkerAsync(int workerId, CancellationToken cancellationToken = default) =>
        await _db.WorkerProjectLinks
            .AsNoTracking()
            .Where(x => x.WorkerId == workerId)
            .OrderByDescending(x => x.CreatedDate)
            .ToListAsync(cancellationToken); public Task<WorkerProjectLink?> GetActiveByRequestAndWorkerAsync(int serviceRequestId, int workerId, CancellationToken cancellationToken = default) =>
    _db.WorkerProjectLinks.FirstOrDefaultAsync(
        x => x.ServiceRequestId == serviceRequestId && x.WorkerId == workerId && x.IsWorkerActive, cancellationToken);

    public Task<List<WorkerProjectLink>> ListActiveByServiceRequestAsync(int serviceRequestId, CancellationToken cancellationToken = default) =>
        _db.WorkerProjectLinks
           .Where(x => x.ServiceRequestId == serviceRequestId && x.IsWorkerActive)
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
    public async Task<HashSet<int>> ListActiveWorkerIdsAsync(int serviceRequestId, IReadOnlyCollection<int> workerIds, CancellationToken ct = default) =>
   (await _db.WorkerProjectLinks.AsNoTracking()
       .Where(x => x.ServiceRequestId == serviceRequestId && x.IsWorkerActive && workerIds.Contains(x.WorkerId))
       .Select(x => x.WorkerId).ToListAsync(ct)).ToHashSet();


    public async Task<IReadOnlyList<ProjectWorkerRow>> ListWorkerRowsByProjectAsync(int projectId, CancellationToken cancellationToken = default) =>
    await _db.WorkerProjectLinks
        .AsNoTracking()
        .Where(x => x.ProjectId == projectId)
        .OrderByDescending(x => x.CreatedDate)
        .Select(x => new ProjectWorkerRow(
            x.Id,
            x.WorkerId,
            x.Worker.FirstName,
            x.Worker.LastName,
            x.Worker.CivilId,
            x.Worker.PhoneNumber,
            x.IsWorkerActive,
            x.DailyWageAmount,
            x.ServiceRequestId,
            x.ServiceRequest.Service.NameEn,
            x.ServiceRequest.Service.NameAr,
            x.ServiceRequest.Service.Price,
            x.ServiceRequest.Association.EnglishName,
            x.CreatedDate,
            x.ServiceRequest.StartDate,
            x.ServiceRequest.EndDate,
            x.ServiceRequest.StatusId))
        .ToListAsync(cancellationToken);
}
