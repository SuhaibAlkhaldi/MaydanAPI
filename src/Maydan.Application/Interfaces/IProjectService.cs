using Maydan.Application.DTOs.Projects;

namespace Maydan.Application.Interfaces;

public interface IProjectService
{
    Task<ProjectDto> CreateAsync(
        CreateProjectDto request,
        int currentUserId,
        CancellationToken cancellationToken = default);

    Task<List<ProjectDto>> GetAllAsync(
        int currentUserId,
        ProjectQueryDto query,
        CancellationToken cancellationToken = default);

    Task<ProjectDto> GetByIdAsync(
        int currentUserId,
        int id,
        CancellationToken cancellationToken = default);

    Task<ProjectDto> UpdateAsync(
        int id,
        UpdateProjectDto request,
        int currentUserId,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        int id,
        int currentUserId,
        CancellationToken cancellationToken = default);

    Task<ProjectDto> RestoreAsync(
        int id,
        int currentUserId,
        CancellationToken cancellationToken = default);

    // Backs GET /api/ProjectTypes. Lives on IProjectService rather than a dedicated service —
    // ProjectTypes has exactly one read method and is tightly coupled to Projects (it's the FK
    // catalog CreateAsync/UpdateAsync validate ProjectTypeId against), so a whole new service
    // interface for one method would be pure ceremony. Exposed through its own
    // ProjectTypesController, not nested under /api/Projects.
    Task<List<ProjectTypeDto>> GetProjectTypesAsync(
        CancellationToken cancellationToken = default);
}
