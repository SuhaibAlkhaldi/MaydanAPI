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

    Task<List<ProjectDto>> GetAllAsync(
        ProjectQueryDto query,
        CancellationToken cancellationToken = default);

    Task<ProjectDto> GetByIdAsync(

        int id,
        CancellationToken cancellationToken = default);

    Task<ProjectDto> UpdateAsync(
        int id,
        UpdateProjectDto request,

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


    Task DeleteAsync(
        int id,
        int currentUserId,
        CancellationToken cancellationToken = default);

    Task<ProjectDto> RestoreAsync(
        int id,
        int currentUserId,
        CancellationToken cancellationToken = default);


    Task<List<ProjectTypeDto>> GetProjectTypesAsync(
        CancellationToken cancellationToken = default);
}
