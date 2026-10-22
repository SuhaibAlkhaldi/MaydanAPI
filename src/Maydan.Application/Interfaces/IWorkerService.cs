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
    //Task<ApiResponse<List<WorkerSummaryDto>>> GetAvailableWorkersForRequestAsync(int currentUserId, int serviceRequestId, CancellationToken cancellationToken = default);

    Task<ApiResponse<WorkerDto>> GetByIdAsync(
        int currentUserId,
        int workerId,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<WorkerDto>> GetByCivilIdAsync(
        int currentUserId,
        string civilId,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<List<WorkerSummaryDto>>> GetByAssociationIdAsync(
        int currentUserId,
        int? associationId = null,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<List<WorkerSummaryDto>>> GetDeletedByAssociationIdAsync(
        int currentUserId,
        int? associationId = null,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<List<WorkerSummaryDto>>> QueryAsync(
        int currentUserId,
        bool isDeleted = false,
        int? associationId = null,
        bool? orderByYearsOfExperienceAscending = null,
        bool? orderByNameEnAscending = null,
        bool? orderByNameArAscending = null,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<WorkerDto>> CreateAsync(
        int currentUserId,
        CreateWorkerDto dto,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<WorkerDto>> UpdateAsync(
        int currentUserId,
        int workerId,
        UpdateWorkerDto dto,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<object?>> DeleteAsync(
        int currentUserId,
        int workerId,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<WorkerDto>> RestoreAsync(
        int currentUserId,
        int workerId,
        CancellationToken cancellationToken = default);


    Task<ApiResponse<List<WorkerDto>>> GetAvailableWorkersForRequestAsync(
             int currentUserId,
             int serviceRequestId,
             CancellationToken cancellationToken = default);
}

