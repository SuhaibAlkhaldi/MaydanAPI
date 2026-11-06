using Maydan.Domain.Entities;

namespace Maydan.Application.Interfaces;

public interface ICityRepository
{
    // Active-only — this is the exact lookup ProductionCompanyOnboardingService.RegisterAsync
    // already relies on to validate CityId; a soft-deleted city must keep failing that check, so
    // this method's filtered behavior must not change.
    Task<City?> GetByIdAsync(int cityId, CancellationToken cancellationToken = default);

    // Bypasses the soft-delete filter — needed by RestoreAsync. See
    Task<City?> GetByIdIncludingDeletedAsync(int cityId, CancellationToken cancellationToken = default);

    // Same "active + deleted together, one list, no toggle" shape as
    // endpoint, called by BOTH the admin Locations page (needs deleted rows for its Restore-button
    // UI) and the public ProductionHouseSignupComponent dropdown (does not). A soft-deleted city
    // could theoretically appear in the public signup dropdown as a result; accepted rather than
    // splitting this into two endpoints, because RegisterAsync's own City lookup (GetByIdAsync,
    // active-only, unchanged above) would still correctly reject it, and
    // ProductionHouseSignupComponent.applyServerError already has a dedicated
    // /city/i + /not found/i branch that maps exactly that failure onto the CityId field instead of
    // a generic error banner — this edge case was already anticipated on the frontend.
    Task<List<City>> GetAllAsync(CancellationToken cancellationToken = default);

    Task AddAsync(City city, CancellationToken cancellationToken = default);
    void Remove(City city);
}
