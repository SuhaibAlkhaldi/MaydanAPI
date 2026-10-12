using Maydan.Application.DTOs.Common;
using Maydan.Application.DTOs.Workers;
using Maydan.Domain.Entities;

namespace Maydan.Application.Interfaces;

public interface IWorkerRepository
{
    Task<ApiResponse<Worker?>> GetByIdAsync(int workerId, CancellationToken cancellationToken = default);
    Task<ApiResponse<Worker?>> GetByIdWithServicesAsync(int workerId, CancellationToken cancellationToken = default);
    Task<ApiResponse<Worker?>> GetByIdIncludingDeletedAsync(int workerId, CancellationToken cancellationToken = default);
    Task<ApiResponse<Worker?>> GetByCivilIdHashAsync(string civilIdHash, CancellationToken cancellationToken = default);
    Task<ApiResponse<List<WorkerSummaryDto>>> GetByAssociationIdAsync(int associationId, CancellationToken cancellationToken = default);
    Task<ApiResponse<List<Worker>>> GetDeletedByAssociationIdAsync(int associationId, CancellationToken cancellationToken = default);

    Task<ApiResponse<bool>> AddAsync(Worker worker, CancellationToken cancellationToken = default);

    Task<ApiResponse<List<Worker>>> QueryAsync(
        bool isDeleted,
        int? associationId = null,
        bool? orderByYearsOfExperienceAscending = null,
        bool? orderByNameEnAscending = null,
        bool? orderByNameArAscending = null,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<(List<WorkerSummaryDto> Items, int TotalCount)>> GetAllProjectedAsync(
        int? associationId, string? search, int? serviceId, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<ApiResponse<WorkerDto?>> GetByIdProjectedAsync(int workerId, CancellationToken cancellationToken = default);

    Task<ApiResponse<bool>> Remove(Worker worker, CancellationToken cancellationToken = default);
}