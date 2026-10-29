using Maydan.Domain.Entities;

namespace Maydan.Application.Interfaces;

public interface IServiceRequestRepository
{
    Task<ServiceRequest?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task AddAsync(ServiceRequest serviceRequest, CancellationToken cancellationToken = default);
    void Remove(ServiceRequest serviceRequest);
    //Task<ServiceRequest?> GetByIdempotencyKeyAsync(string idempotencyKey, int productionCompanyId, CancellationToken cancellationToken = default);
    Task<List<ServiceRequest>> GetByProductionCompanyIdAsync(int productionCompanyId, CancellationToken cancellationToken = default);
    Task<List<ServiceRequest>> GetByAssociationIdAsync(int associationId, CancellationToken cancellationToken = default);
    Task<List<ServiceRequest>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<ServiceRequest>> GetPendingOlderThanAsync(
            DateTime threshold,
            CancellationToken cancellationToken = default);

    Task<bool> HasDifferentServiceAssignmentInProjectAsync(
        int workerId,
        int serviceRequestId,
        int projectId,
        int serviceId,
        CancellationToken cancellationToken = default);

    Task<bool> HasOverlappingServiceRequestAssignmentInProjectAsync(
        int workerId,
        int serviceRequestId,
        int projectId,
        DateTime requestStartDate,
        DateTime requestEndDate,
        CancellationToken cancellationToken = default);

    Task<bool> HasOverlappingProjectAssignmentAsync(
        int workerId,
        int serviceRequestId,
        int projectId,
        DateTime projectStartDate,
        DateTime projectEndDate,
        CancellationToken cancellationToken = default);

    Task AssignToServiceRequestAsync(
        int serviceRequestId,
        int workerId,
        CancellationToken cancellationToken = default); 
}
