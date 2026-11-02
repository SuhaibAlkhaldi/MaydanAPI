using Maydan.Application.DTOs.ServiceRequests;
using Maydan.Domain.Entities;

namespace Maydan.Application.Interfaces;

public interface IServiceRequestRepository
{
  
    Task AddAsync(ServiceRequest serviceRequest, CancellationToken cancellationToken = default);
    void Remove(ServiceRequest serviceRequest);
    Task<List<ServiceRequest>> GetByProductionCompanyIdAsync(int productionCompanyId, CancellationToken cancellationToken = default);
    Task<List<ServiceRequest>> GetByAssociationIdAsync(int associationId, CancellationToken cancellationToken = default);
    Task<List<ServiceRequest>> GetPendingOlderThanAsync(DateTime threshold, CancellationToken cancellationToken = default);
    Task AssignToServiceRequestAsync(int serviceRequestId,int workerId,CancellationToken cancellationToken = default);
    //NEW
    Task<ServiceRequest?> GetForUpdateAsync(int id, CancellationToken ct = default);
    Task<ServiceRequest?> GetForAssignmentAsync(int id, CancellationToken ct = default); // + Service
    Task<ServiceRequestAttendanceInfo?> GetAttendanceInfoAsync(int id, CancellationToken ct = default);
    Task<ServiceRequestDetailsDto?> GetDetailsAsync(int id, CancellationToken ct = default);
    Task<List<ServiceRequestDto>> ListSummariesByProductionCompanyAsync(int id, CancellationToken ct = default);
    Task<List<ServiceRequestDto>> ListSummariesByAssociationAsync(int id, CancellationToken ct = default);
    Task<List<ServiceRequest>> ListByProjectForDetailsAsync(int projectId, CancellationToken ct = default);

    //------
    //Task<List<ServiceRequest>> GetAllAsync(CancellationToken cancellationToken = default);
    //Task<List<ServiceRequest>> GetByProjectIdAsync(int projectId, CancellationToken cancellationToken = default);
    //Task<ServiceRequest?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

}
