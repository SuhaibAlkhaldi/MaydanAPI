using Maydan.Application.DTOs.ServiceRequests;
using Maydan.Domain.Enums;

namespace Maydan.Application.Interfaces;

public interface IServiceRequestService
{
    Task<ServiceRequestDto> CreateAsync(int currentUserId, CreateServiceRequestDto dto, CancellationToken cancellationToken = default);
    Task<ExpectedPaymentCalculationDto> CalculateExpectedPaymentAsync(int currentUserId, int serviceId, int requestedWorkers, int durationCount, ServiceTimeUnit timeUnit, CancellationToken cancellationToken = default);
    Task<List<ServiceRequestDto>> GetAllAsync(int currentUserId, CancellationToken cancellationToken = default);
    Task<ServiceRequestDetailsDto> GetByIdAsync(int currentUserId, int id, CancellationToken cancellationToken = default);
    Task CancelAsync(int currentUserId, int id, CancellationToken cancellationToken = default);
    Task<AssociationLookupDto> ResolveAssociationByCityIdAsync(int currentUserId, int cityId, CancellationToken cancellationToken = default);
}
