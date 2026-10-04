using Maydan.Domain.Entities;

namespace Maydan.Application.Interfaces;

public interface IWorkerRepository
{
    Task<Worker?> GetByIdAsync(int workerId, CancellationToken cancellationToken = default);

    // civilIdHash is the blind index (ICivilIdHasher.ComputeHash). CivilId itself is currently
    // PLAINTEXT (see Worker.CivilId's own comment — encryption via ISecretProtector is planned but
    // not yet implemented); this lookup exists regardless because even once that's wired up,
    // ciphertext still can't be queried/compared directly, so CivilIdHash remains the only way to
    // look a worker up by civil ID.
    Task<Worker?> GetByCivilIdHashAsync(string civilIdHash, CancellationToken cancellationToken = default);
    Task<List<Worker>> GetByAssociationIdAsync(int associationId, CancellationToken cancellationToken = default);

    // MAYD-51 (Association Management, Phase 2c): mirror of IUserRepository.GetDeletedByEntityAsync
    // — SOFT-DELETED rows only, for AssociationService.RestoreAsync's own cascade. See that
    // interface method's own comment for the full reasoning.
    Task<List<Worker>> GetDeletedByAssociationIdAsync(int associationId, CancellationToken cancellationToken = default);

    Task AddAsync(Worker worker, CancellationToken cancellationToken = default);

    Task<List<Worker>> QueryAsync(
    bool isDeleted,
    int? associationId = null,
    bool? orderByYearsOfExperienceAscending = null,
    bool? orderByNameEnAscending = null,
    bool? orderByNameArAscending = null,
    CancellationToken cancellationToken = default);



    Task Remove(Worker worker, CancellationToken cancellationToken = default);
    void Remove(Worker worker);
}
