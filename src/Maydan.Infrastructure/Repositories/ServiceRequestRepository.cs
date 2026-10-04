using Maydan.Application.Interfaces;
using Maydan.Domain.Entities;
using Maydan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Maydan.Infrastructure.Repositories;

public class ServiceRequestRepository : IServiceRequestRepository
{
    private readonly MaydanDbContext _context;

    public ServiceRequestRepository(MaydanDbContext context)
    {
        _context = context;
    }

    public Task<ServiceRequest?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.Set<ServiceRequest>()
            .Include(r => r.Service)
            .Include(r => r.Project)
            .Include(r => r.Association)
            .Include(r => r.ProductionCompany)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task AddAsync(ServiceRequest serviceRequest, CancellationToken cancellationToken = default) =>
        await _context.Set<ServiceRequest>().AddAsync(serviceRequest, cancellationToken);

    public void Remove(ServiceRequest serviceRequest) => _context.Set<ServiceRequest>().Remove(serviceRequest);

    public Task<List<ServiceRequest>> GetByProductionCompanyIdAsync(int productionCompanyId, CancellationToken cancellationToken = default) =>
        _context.Set<ServiceRequest>().Where(s => s.ProductionCompanyId == productionCompanyId).ToListAsync(cancellationToken);

    public Task<List<ServiceRequest>> GetByAssociationIdAsync(int associationId, CancellationToken cancellationToken = default) =>
        _context.Set<ServiceRequest>().Where(s => s.AssociationId == associationId).ToListAsync(cancellationToken);

    public Task<List<ServiceRequest>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _context.Set<ServiceRequest>().ToListAsync(cancellationToken);

    public Task<List<ServiceRequest>> GetPendingOlderThanAsync(DateTime threshold, CancellationToken cancellationToken = default) =>
        _context.Set<ServiceRequest>()
            .Where(r => r.Status == Domain.Enums.ServiceRequestStatus.PendingWorkerSelection && !r.ReminderSent && r.CreatedAt <= threshold)
            .Include(r => r.Association)
            .ToListAsync(cancellationToken);
}
