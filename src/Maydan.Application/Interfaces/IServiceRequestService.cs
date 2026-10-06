using Maydan.Application.DTOs.Common;
using Maydan.Application.DTOs.ServiceRequests;
using Maydan.Domain.Enums;

namespace Maydan.Application.Interfaces;

public interface IServiceRequestService
{
    Task<ApiResponse<ServiceRequestDto>> CreateAsync(int currentUserId, CreateServiceRequestDto dto, CancellationToken cancellationToken = default);
    Task<ApiResponse<ExpectedPaymentCalculationDto>> CalculateExpectedPaymentAsync(int currentUserId, int serviceId, int requestedWorkers, int durationCount, ServiceTimeUnit timeUnit, CancellationToken cancellationToken = default);
    Task<ApiResponse<List<ServiceRequestDto>>> GetAllAsync(int currentUserId, CancellationToken cancellationToken = default);
    Task<ApiResponse<ServiceRequestDetailsDto>> GetByIdAsync(int currentUserId, int id, CancellationToken cancellationToken = default);
    Task<ApiResponse<object?>> CancelAsync(int currentUserId, int id, CancellationToken cancellationToken = default);
    Task<ApiResponse<AssociationLookupDto>> ResolveAssociationByCityIdAsync(int currentUserId, int cityId, CancellationToken cancellationToken = default);
    Task<ApiResponse<object?>> RejectAsync(int currentUserId, int id, string? reason, CancellationToken cancellationToken = default);
    Task<ApiResponse<object?>> ApproveAsync(int currentUserId, int id, CancellationToken cancellationToken = default); //future steps to add Worker IDs here later if needed
    //Task<ApiResponse<object?>> CompletedAsync(int currentUserId , int id, CancellationToken cancellationToken = default);
}
