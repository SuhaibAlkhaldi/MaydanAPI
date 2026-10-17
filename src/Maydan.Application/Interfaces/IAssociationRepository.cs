using Maydan.Domain.Entities;

namespace Maydan.Application.Interfaces;

public interface IAssociationRepository
{
    // Unchanged signatures — EntityOnboardingService (the real, already-built associations/
    // without-admin + associations/{id}/admin flow) already depends on these exact shapes; Phase 2a
    // only adds to this interface, never changes what's already here.
    Task<Association?> GetByIdAsync(int associationId, CancellationToken cancellationToken = default);
    Task<List<Association>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Association?> GetByCityIdAsync(int cityId, CancellationToken cancellationToken = default);
    Task AddAsync(Association association, CancellationToken cancellationToken = default);
    void Remove(Association association);

    // Bypasses the soft-delete filter — needed by RestoreAsync, same pattern as
    // ICountryRepository.GetByIdIncludingDeletedAsync (Phase 1's own precedent).
    Task<Association?> GetByIdIncludingDeletedAsync(int associationId, CancellationToken cancellationToken = default);

    // MAYD-51 (Association Management, Phase 2c): DeleteAsync's own cascade-delete needs the real
    // Workers collection actually LOADED (tracked), not just separately queried — Worker.Association
    // is a required (non-nullable FK), DeleteBehavior.Restrict navigation (WorkerConfiguration.cs).
    // Confirmed by hitting it live: if both the Association and separately-tracked Worker rows are
    // marked EntityState.Deleted in the same SaveChanges call while Association.Workers isn't
    // Included, EF Core's own relationship fixup can't reconcile the two and throws ("the
    // association between entity types 'Association' and 'Worker' has been severed..."). Loading the
    // collection here (global soft-delete filter still applies to it automatically, same as any
    // other Include) avoids that entirely — GetByIdAsync itself is left untouched since its other
    // real callers (GetAllAsync's own reuse aside) have no use for the Workers collection.
    Task<Association?> GetByIdWithWorkersAsync(int associationId, CancellationToken cancellationToken = default);

    // GET /api/Associations/{id} needs WorkersCount alongside the entity itself — a computed
    // COUNT(Workers WHERE AssociationId = Id) (Association.cs's own comment), not a stored column,
    // so it's carried out of the repository as a tuple rather than bolted onto the entity.
    Task<(Association Association, int WorkersCount)?> GetByIdWithWorkersCountAsync(int associationId, CancellationToken cancellationToken = default);

    // One flexible query method covering every list-shaped endpoint this module needs (GetAll,
    // by-name search, order-by-worker-count asc/desc, the deleted-only list, and deleted+search
    // combined) — same "one method, optional params" shape ProjectRepository.GetAllAsync already
    // established, rather than five near-duplicate methods. isDeleted=true bypasses the soft-delete
    // filter and returns ONLY deleted rows (mirrors ProjectRepository's own isDeleted-toggle
    // comment); searchTerm matches EnglishName OR ArabicName (same Contains-on-both-fields
    // convention as UserRepository's own name search);
    // orderByWorkersCountAscending null falls back to EnglishName ordering.
    Task<List<(Association Association, int WorkersCount)>> QueryAsync(
        bool isDeleted,
        string? searchTerm = null,
        bool? orderByWorkersCountAscending = null,
        CancellationToken cancellationToken = default);
}
