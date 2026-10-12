using Maydan.Application.DTOs.Associations;
using Maydan.Application.DTOs.Common;

namespace Maydan.Application.Interfaces;

public interface IAssociationServiceV2
{
    Task<ApiResponse<List<AssociationDtoV2>>> GetAllAsync(int currentUserId, CancellationToken cancellationToken = default);
    Task<ApiResponse<AssociationDtoV2>> GetByIdAsync(int currentUserId, int associationIdV2, CancellationToken cancellationToken = default);
    Task<ApiResponse<AssociationDetailsDtoV2>> GetDetailsAsync(int currentUserId, int associationIdV2, CancellationToken cancellationToken = default);
    Task<ApiResponse<List<AssociationDtoV2>>> SearchByNameAsync(int currentUserId, string name, CancellationToken cancellationToken = default);
    Task<ApiResponse<List<AssociationDtoV2>>> GetOrderedByWorkersCountAsync(int currentUserId, bool ascending, CancellationToken cancellationToken = default);
    Task<ApiResponse<List<AssociationDtoV2>>> GetDeletedAsync(int currentUserId, CancellationToken cancellationToken = default);
    Task<ApiResponse<List<AssociationDtoV2>>> SearchDeletedByNameAsync(int currentUserId, string name, CancellationToken cancellationToken = default);

    Task<ApiResponse<AssociationDtoV2>> CreateAsync(int currentUserId, CreateAssociationDtoV2 dto, CancellationToken cancellationToken = default);
    Task<ApiResponse<AssociationDtoV2>> UpdateAsync(int currentUserId, UpdateAssociationDtoV2 dto, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> DeleteAsync(int currentUserId, int associationIdV2, CancellationToken cancellationToken = default);
    Task<ApiResponse<AssociationDtoV2>> RestoreAsync(int currentUserId, int associationIdV2, CancellationToken cancellationToken = default);
}