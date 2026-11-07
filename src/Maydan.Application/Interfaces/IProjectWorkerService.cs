using Maydan.Application.DTOs.Common;
using Maydan.Application.DTOs.Projects;

namespace Maydan.Application.Interfaces;

/// <summary>
/// EN: Application service contract for managing Project Workers, filtering, and activation state.
/// AR: واجهة خدمة التطبيق لإدارة عمال المشروع، الفلترة، وتغيير حالة التفعيل.
/// </summary>
public interface IProjectWorkerService
{
    Task<ApiResponse<List<ProjectWorkerDto>>> GetByProjectIdAsync(
        int projectId,
        string? searchByWorkerName,
        string? searchByWorkerCivilianId,
        bool? isWorkerActive,
        int currentUserId,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<ProjectWorkerDto>> GetByIdAsync(int id, int currentUserId, CancellationToken cancellationToken = default);
    Task<ApiResponse<ProjectWorkerDto>> CreateAsync(CreateProjectWorkerDto dto, int currentUserId, CancellationToken cancellationToken = default);
    Task<ApiResponse<ProjectWorkerDto>> UpdateAsync(int id, UpdateProjectWorkerDto dto, int currentUserId, CancellationToken cancellationToken = default);
    Task<ApiResponse<ProjectWorkerDto>> ActivateAsync(int id, int currentUserId, CancellationToken cancellationToken = default);
    Task<ApiResponse<ProjectWorkerDto>> DeactivateAsync(int id, int currentUserId, CancellationToken cancellationToken = default);
}