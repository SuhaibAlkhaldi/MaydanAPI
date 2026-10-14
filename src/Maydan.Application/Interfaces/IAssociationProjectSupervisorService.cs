using Maydan.Application.DTOs;
using Maydan.Application.DTOs.Associations;
using Maydan.Application.DTOs.Common;

namespace Maydan.Application.Interfaces;

public interface IAssociationProjectSupervisorService
{
    Task<ApiResponse<List<AssociationProjectSupervisorDto>>> GetAllAsync(int projectId, int associationId, CancellationToken cancellationToken = default);

    Task<ApiResponse<AssociationProjectSupervisorDto>> CreateAsync(CreateAssociationProjectSupervisorDto dto, int currentUserId, CancellationToken cancellationToken = default);

    Task<ApiResponse<bool>> DeleteAsync(int id, int currentUserId, CancellationToken cancellationToken = default);
}