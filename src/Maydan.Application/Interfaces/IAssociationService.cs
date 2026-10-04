using Maydan.Application.DTOs.Associations;
using Maydan.Application.DTOs.Common;

namespace Maydan.Application.Interfaces;

// Association Management, Phase 2a (MAYD-4/MAYD-40..54) — see AssociationService's own header
// comment for the full "what already existed vs. what this pass built" story.
//
// Association Admin User / Association Users-Details gaps (2026-09-29): CreateAsync's
// CreateAssociationDto now carries an OPTIONAL Admin payload (still additive — omitted, it's
// exactly today's association-only behavior; EntityOnboardingController's associations/without-admin
// + associations/{id}/admin flow stays the only way to add an admin to an EXISTING association).
// GetDetailsAsync is new: the one place an Association's own fields and its real Users
// (EntityType.Association-scoped) come back together.
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
    Task<ApiResponse<AssociationDto>> UpdateAsync(int currentUserId, UpdateAssociationDto dto, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> DeleteAsync(int currentUserId, int associationId, CancellationToken cancellationToken = default);
    Task<ApiResponse<AssociationDto>> RestoreAsync(int currentUserId, int associationId, CancellationToken cancellationToken = default);
}
