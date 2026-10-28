using Maydan.Application.DTOs.Common;
using Maydan.Application.DTOs.ProjectDetails;

namespace Maydan.Application.Interfaces;

// Read-only project details. Visible only to the Bayt Al Urdon admin and to the admin of the
//Production House that owns the project.
public interface IProjectDetailsService
{
    Task<ApiResponse<ProjectDetailsDto>> GetProjectFullDetailsAsync(int currentUserId, int projectId, CancellationToken cancellationToken = default);
    Task<ApiResponse<List<ProjectDetailsWorkerDto>>> GetProjectWorkersAsync(int currentUserId, int projectId, CancellationToken cancellationToken = default);
}
