using Maydan.Domain.Entities;

namespace Maydan.Application.Interfaces;

public interface IAssociationProjectSupervisorRepository
{
    // Active-only, with Project and User both loaded (needed for the enriched list response —
    // project name and the supervisor user's own name/email/phone). projectId filters on
    // ProjectId directly; associationId filters on the assigned User's own EntityId (where
    // EntityType == EntityType.Association) — there's no AssociationUser link entity, so this is
    // the only way to scope "supervisors for this association's users." Both null = no filter
    // (return everything) — the "0 means no filter" translation happens one layer up, in the
    // service, so this repository only ever deals in real nullable semantics.
    Task<(List<AssociationProjectSupervisor> Items, int TotalCount)> GetAllAsync(
        int? projectId,
        int? associationId,
        int? productionCompanyId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<AssociationProjectSupervisor?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task AddAsync(AssociationProjectSupervisor supervisor, CancellationToken cancellationToken = default);
    void Remove(AssociationProjectSupervisor supervisor);
}
