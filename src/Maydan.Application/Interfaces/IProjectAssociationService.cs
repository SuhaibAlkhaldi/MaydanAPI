using Maydan.Application.DTOs.Common;
using Maydan.Application.DTOs.Projects;

namespace Maydan.Application.Interfaces;

/// <summary>
/// EN: Application service contract for managing Project Associations with strict role/tenant security.
/// AR: واجهة خدمة التطبيق لإدارة جمعيات المشروع ومواقعها مع تطبيق صارم لصلاحيات المستخدمين.
/// </summary>
public interface IProjectAssociationService
{
    Task<ApiResponse<List<ProjectAssociationDto>>> GetByProjectIdAsync(int projectId, int currentUserId, CancellationToken cancellationToken = default);
    Task<ApiResponse<ProjectAssociationDto>> GetByIdAsync(int id, int currentUserId, CancellationToken cancellationToken = default);
    Task<ApiResponse<ProjectAssociationDto>> CreateAsync(CreateProjectAssociationDto dto, int currentUserId, CancellationToken cancellationToken = default);
    Task<ApiResponse<ProjectAssociationDto>> UpdateAsync(int id, UpdateProjectAssociationDto dto, int currentUserId, CancellationToken cancellationToken = default);
    Task<ApiResponse<object?>> DeleteAsync(int id, int currentUserId, CancellationToken cancellationToken = default);
}