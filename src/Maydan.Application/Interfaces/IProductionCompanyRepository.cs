using Maydan.Domain.Entities;

namespace Maydan.Application.Interfaces;

public interface IProductionCompanyRepository
{
    // Production House Management (MAYD-80/81/82), so widening it here is safe; matches
    Task<ProductionCompany?> GetByIdAsync(int productionCompanyId, CancellationToken cancellationToken = default);
    Task<List<ProductionCompany>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<bool> RegistrationNumberExistsAsync(string registrationNumber, CancellationToken cancellationToken = default);

    // MAYD-79 gap fix: CompanyNameEn ("must be unique" per the sign-up ticket's own text) had no
    // uniqueness check at all — only Email and RegistrationNumber did. Mirrors
    // RegistrationNumberExistsAsync's own shape exactly.
    Task<bool> EnglishNameExistsAsync(string englishName, CancellationToken cancellationToken = default);

    // MAYD-80 (List page): the one list endpoint the frontend's production-company.service.ts
    // actually calls — search + isDeleted together, same "one bool bypasses the global soft-delete
    // filter, no separate deleted-view method" shape AssociationRepository.QueryAsync/
    // CityLocationRepository.GetByCityIdAsync already use. isDeleted=true will always return an
    // empty list today (nothing here can currently become soft-deleted — no delete endpoint exists,
    // out of scope for this phase, see the controller's own comment), which is correct, not a bug.
    Task<List<ProductionCompany>> QueryAsync( string? searchTerm = null, bool? isActive = null, CancellationToken cancellationToken = default);

    Task AddAsync(ProductionCompany productionCompany, CancellationToken cancellationToken = default);
    void Remove(ProductionCompany productionCompany);
}
