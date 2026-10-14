using Maydan.Application.DTOs.Common;
using Maydan.Application.DTOs.Workers;

namespace Maydan.Application.Interfaces;

public interface IWorkerService
{
    Task<ApiResponse<PagedResult<WorkerSummaryDto>>> GetAllAsync(int currentUserId, string? search, int? serviceId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<ApiResponse<WorkerDto>> GetByIdAsync(int currentUserId, int workerId, CancellationToken cancellationToken = default);
    Task<ApiResponse<WorkerDto>> CreateAsync(int currentUserId, CreateWorkerDto dto, CancellationToken cancellationToken = default);
    Task<ApiResponse<WorkerDto>> UpdateAsync(int currentUserId, int workerId, UpdateWorkerDto dto, CancellationToken cancellationToken = default);
    Task<ApiResponse<object?>> DeleteAsync(int currentUserId, int workerId, CancellationToken cancellationToken = default);
    Task<ApiResponse<WorkerDto>> RestoreAsync(int currentUserId, int workerId, CancellationToken cancellationToken = default);
}
