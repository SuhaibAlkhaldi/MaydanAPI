using Maydan.Application.DTOs.Common;
using Maydan.Application.DTOs.Projects;
using Maydan.Application.DTOs.UserManagement;
namespace Maydan.Application.Interfaces;

public interface IProjectService
{
    Task<ApiResponse<ProjectDto>> CreateAsync(
        CreateProjectDto request,
        int currentUserId,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<List<ProjectDto>>> GetAllAsync(
        int currentUserId,
        ProjectQueryDto query,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<ProjectDto>> GetByIdAsync(
        int currentUserId,
        int id,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<ProjectDto>> UpdateAsync(
        int id,
        UpdateProjectDto request,
        int currentUserId,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<object?>> DeleteAsync(
        int id,
        int currentUserId,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<ProjectDto>> RestoreAsync(
        int id,
        int currentUserId,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<List<ProjectTypeDto>>> GetProjectTypesAsync(
        CancellationToken cancellationToken = default);

Task<ApiResponse<List<UserSummaryDto>>> GetAvailableUsersAsync(int currentUserId, DateTime startDate, DateTime endDate, int? currentProjectId = null, CancellationToken cancellationToken = default);
 Task<ApiResponse<ProjectDto>> ApproveAsync(int id, int currentUserId, CancellationToken cancellationToken = default);
}