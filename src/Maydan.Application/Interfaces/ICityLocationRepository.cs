using Maydan.Domain.Entities;

namespace Maydan.Application.Interfaces;

public interface ICityLocationRepository
{
    // Active-only — used to load a row for Update, and (via GetByIdIncludingDeletedAsync below) is
    // not what Restore uses. Same shape as ICityRepository.GetByIdAsync.
    Task<CityLocation?> GetByIdAsync(int cityLocationId, CancellationToken cancellationToken = default);

    // Bypasses the soft-delete filter — needed by RestoreAsync, same pattern as
    // ICityRepository.GetByIdIncludingDeletedAsync.
    Task<CityLocation?> GetByIdIncludingDeletedAsync(int cityLocationId, CancellationToken cancellationToken = default);

    // The admin Locations page (location-management.component.ts, mode: 'cityLocations') shows
    // active AND soft-deleted city locations in the SAME list — a deleted row renders with a
    // "Deleted" pill and a Restore button instead of being hidden, exactly like
    // tiers (see either one's own comment for the full reasoning). CityLocationsService.getByCity()
    // takes no active/deleted toggle at all, so this deliberately bypasses the global soft-delete
    // filter rather than returning active-only rows — active-only would make the Restore button
    // unreachable through the real UI, since a deleted row would never appear in the list to click it.
    Task<List<CityLocation>> GetByCityIdAsync(int cityId, CancellationToken cancellationToken = default);

    Task AddAsync(CityLocation cityLocation, CancellationToken cancellationToken = default);
    void Remove(CityLocation cityLocation);
}
