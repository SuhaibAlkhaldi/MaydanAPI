using Maydan.Application.DTOs.Associations;
using Maydan.Application.DTOs.Common;

namespace Maydan.Application.Interfaces;

public interface IAssociationService
{
    Task<ApiResponse<List<AssociationDto>>> GetAllAsync(int currentUserId, CancellationToken cancellationToken = default);
    Task<ApiResponse<AssociationDto>> GetByIdAsync(int currentUserId, int associationId, CancellationToken cancellationToken = default);
    Task<ApiResponse<AssociationDetailsDto>> GetDetailsAsync(int currentUserId, int associationId, CancellationToken cancellationToken = default);
    Task<ApiResponse<List<AssociationDto>>> SearchByNameAsync(int currentUserId, string name, CancellationToken cancellationToken = default);
    Task<ApiResponse<List<AssociationDto>>> GetOrderedByWorkersCountAsync(int currentUserId, bool ascending, CancellationToken cancellationToken = default);
    Task<ApiResponse<List<AssociationDto>>> GetDeletedAsync(int currentUserId, CancellationToken cancellationToken = default);
    Task<ApiResponse<List<AssociationDto>>> SearchDeletedByNameAsync(int currentUserId, string name, CancellationToken cancellationToken = default);

    Task<ApiResponse<AssociationDto>> CreateAsync(int currentUserId, CreateAssociationDto dto, CancellationToken cancellationToken = default);
    Task<ApiResponse<AssociationUpdateResponseDto>> UpdateAsync(int currentUserId,UpdateAssociationDto dto,CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> DeleteAsync(int currentUserId, int associationId, CancellationToken cancellationToken = default);
    Task<ApiResponse<AssociationDto>> RestoreAsync(int currentUserId, int associationId, CancellationToken cancellationToken = default);
}