using Maydan.Application.DTOs.Associations;
using Maydan.Application.DTOs.Common;

namespace Maydan.Application.Interfaces;

// Association Management, Phase 2e: a new dedicated service, not an extension of an existing one —
// a supervisor assignment spans three aggregates (Project/User/Association) and doesn't naturally
// belong to any single one of their own services. LocationService's "one cohesive service per
// feature area" reasoning (City is FK'd to Country, one frontend page drives both) doesn't apply
// here: Project and the supervising User are peers linked by a new junction entity, not a
// parent/child pair under one page.
public interface IAssociationProjectSupervisorService
{
    // projectId/associationId of 0 mean "no filter" — matches workforcment's
    // association-supervisors.service.ts, which always sends both as literal numeric strings
    // (never omitted) and relies on 0 meaning "not selected yet." No real row will ever have
    // ProjectId == 0 or resolve to AssociationId == 0, so treating 0 as null is safe.
    Task<ApiResponse<PagedAssociationProjectSupervisorDto>> GetAllAsync(int currentUserId, int projectId, int associationId, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<ApiResponse<AssociationProjectSupervisorDto>> CreateAsync(CreateAssociationProjectSupervisorDto dto, int currentUserId, CancellationToken cancellationToken = default);
    Task<ApiResponse<object?>> DeleteAsync(int id, int currentUserId, CancellationToken cancellationToken = default);

    // Plain, unconditional soft-delete of the assignment row itself — it's the dependent side of
    // both relationships (Restrict on both FKs), nothing cascades from removing a supervisor
    // assignment. No RestoreAsync: association-supervisors.service.ts has no restore call at all.
}
