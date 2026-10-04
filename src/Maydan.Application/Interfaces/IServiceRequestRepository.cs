using Maydan.Domain.Entities;

namespace Maydan.Application.Interfaces;

public interface IServiceRequestRepository
{
    Task<ServiceRequest?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task AddAsync(ServiceRequest serviceRequest, CancellationToken cancellationToken = default);
    void Remove(ServiceRequest serviceRequest);
    Task<ServiceRequest?> GetByIdempotencyKeyAsync(string idempotencyKey, int productionCompanyId, CancellationToken cancellationToken = default);
    Task<List<ServiceRequest>> GetByProductionCompanyIdAsync(int productionCompanyId, CancellationToken cancellationToken = default);
    Task<List<ServiceRequest>> GetByAssociationIdAsync(int associationId, CancellationToken cancellationToken = default);
    Task<List<ServiceRequest>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<ServiceRequest>> GetPendingOlderThanAsync(DateTime threshold, CancellationToken cancellationToken = default);
}
