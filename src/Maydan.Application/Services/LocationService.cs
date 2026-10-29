using Maydan.Application.DTOs.Locations;
using Maydan.Application.Interfaces;
using Maydan.Domain.Entities;

namespace Maydan.Application.Services;

// Associations/Country/City backend gap, Phase 1 (2026-09-25): Country.cs/City.cs,
// CountryConfiguration/CityConfiguration (real FK, Restrict delete behavior), ICountryRepository/
// ICityRepository + their repositories, and the AddAssociationsProductionCompaniesLocations
// migration all predate this class — they existed, wired into IUnitOfWork, with nothing at the
// Application or API layer ever calling them, and zero seed data. That last gap was an active bug,
// not just a missing feature: ProductionCompanyOnboardingService.RegisterAsync already requires a
// real CityId and throws KeyNotFoundException("City was not found.") when it doesn't resolve — with
// an empty Cities table, every real self-registration through that already-live flow was broken.
//
// Same exception-type convention as ProjectService/UserManagementService throughout: KeyNotFoundException
// for "not found", InvalidOperationException for business-rule violations — ApiControllerBase.HandleException
// maps both to the right HTTP status without this class knowing about status codes at all.
public class LocationService : ILocationService
{
    private const int MaxNameLength = 200;

    private readonly IUnitOfWork _unitOfWork;

    public LocationService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<CountryDto>> GetCountriesAsync(CancellationToken cancellationToken = default)
    {
        var countries = await _unitOfWork.Countries.GetAllAsync(cancellationToken);
        return countries
            .DistinctBy(x=>(x.ArabicName,x.EnglishName))
            .Select(MapCountry).ToList();
    }

    public async Task<CountryDto> GetCountryByIdAsync(int countryId, CancellationToken cancellationToken = default)
    {
        var country = await _unitOfWork.Countries.GetByIdAsync(countryId, cancellationToken)
            ?? throw new KeyNotFoundException("Country was not found.");

        return MapCountry(country);
    }

    public async Task<CountryDto> CreateCountryAsync(CreateCountryDto dto, int currentUserId, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(currentUserId, cancellationToken);
        ValidateName(dto.CountryEnglishName, "English country name");
        ValidateName(dto.CountryArabicName, "Arabic country name");

        var country = new Country
        {
            EnglishName = dto.CountryEnglishName.Trim(),
            ArabicName = dto.CountryArabicName.Trim(),
            CreatedBy = currentUser.UserId,
            IsActive = true
        };

        await _unitOfWork.Countries.AddAsync(country, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapCountry(country);
    }

    public async Task<CountryDto> UpdateCountryAsync(UpdateCountryDto dto, int currentUserId, CancellationToken cancellationToken = default)
    {
        await GetCurrentUserAsync(currentUserId, cancellationToken);
        ValidateName(dto.CountryEnglishName, "English country name");
        ValidateName(dto.CountryArabicName, "Arabic country name");

        var country = await _unitOfWork.Countries.GetByIdAsync(dto.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Country was not found.");

        country.EnglishName = dto.CountryEnglishName.Trim();
        country.ArabicName = dto.CountryArabicName.Trim();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapCountry(country);
    }

    public async Task DeleteCountryAsync(int countryId, int currentUserId, CancellationToken cancellationToken = default)
    {
        await GetCurrentUserAsync(currentUserId, cancellationToken);

        var country = await _unitOfWork.Countries.GetByIdAsync(countryId, cancellationToken)
            ?? throw new KeyNotFoundException("Country was not found.");

        // CityConfiguration's real City -> Country FK uses Restrict delete behavior; a soft delete
        // never touches that constraint (see MaydanDbContext.SaveChangesAsync — a Deleted entry is
        // rewritten to Modified/IsDeleted=true, the row itself is never removed), but leaving active
        // Cities under a Country an admin just marked deleted is the same kind of dangling-reference
        // state Restrict exists to prevent, so it's enforced here instead.
        var activeCities = await _unitOfWork.Cities.GetByCountryIdAsync(countryId, cancellationToken);
        if (activeCities.Any(c => !c.IsDeleted))
        {
            throw new InvalidOperationException("Cannot delete a country that still has active cities. Delete or move its cities first.");
        }

        // MaydanDbContext.SaveChangesAsync intercepts EntityState.Deleted for every SharedEntities
        // and converts it into a soft delete (IsDeleted = true, DeletedAt = UtcNow) — this does not
        // hard-delete the row (see ProjectService.DeleteAsync's identical comment).
        _unitOfWork.Countries.Remove(country);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<CountryDto> RestoreCountryAsync(int countryId, int currentUserId, CancellationToken cancellationToken = default)
    {
        await GetCurrentUserAsync(currentUserId, cancellationToken);

        var country = await _unitOfWork.Countries.GetByIdIncludingDeletedAsync(countryId, cancellationToken)
            ?? throw new KeyNotFoundException("Country was not found.");

        if (!country.IsDeleted)
        {
            throw new InvalidOperationException("Country is not deleted.");
        }

        country.IsDeleted = false;
        country.DeletedAt = null;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapCountry(country);
    }

    public async Task<List<CityDto>> GetCitiesByCountryAsync(int countryId, CancellationToken cancellationToken = default)
    {
        var cities = await _unitOfWork.Cities.GetByCountryIdAsync(countryId, cancellationToken);
        return cities.Select(MapCity).ToList();
    }

    public async Task<CityDto> CreateCityAsync(CreateCityDto dto, int currentUserId, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(currentUserId, cancellationToken);
        ValidateName(dto.CityEnglishName, "English city name");
        ValidateName(dto.CityArabicName, "Arabic city name");

        await EnsureCountryExistsAsync(dto.CountryId, cancellationToken);

        var city = new City
        {
            EnglishName = dto.CityEnglishName.Trim(),
            ArabicName = dto.CityArabicName.Trim(),
            CountryId = dto.CountryId,
            CreatedBy = currentUser.UserId,
            IsActive = true
        };

        await _unitOfWork.Cities.AddAsync(city, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapCity(city);
    }

    public async Task<CityDto> UpdateCityAsync(int cityId, UpdateCityDto dto, int currentUserId, CancellationToken cancellationToken = default)
    {
        await GetCurrentUserAsync(currentUserId, cancellationToken);
        ValidateName(dto.CityEnglishName, "English city name");
        ValidateName(dto.CityArabicName, "Arabic city name");

        var city = await _unitOfWork.Cities.GetByIdAsync(cityId, cancellationToken)
            ?? throw new KeyNotFoundException("City was not found.");

        await EnsureCountryExistsAsync(dto.CountryId, cancellationToken);

        city.EnglishName = dto.CityEnglishName.Trim();
        city.ArabicName = dto.CityArabicName.Trim();
        city.CountryId = dto.CountryId;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapCity(city);
    }

    public async Task DeleteCityAsync(int cityId, int currentUserId, CancellationToken cancellationToken = default)
    {
        await GetCurrentUserAsync(currentUserId, cancellationToken);

        var city = await _unitOfWork.Cities.GetByIdAsync(cityId, cancellationToken)
            ?? throw new KeyNotFoundException("City was not found.");

        _unitOfWork.Cities.Remove(city);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<CityDto> RestoreCityAsync(int cityId, int currentUserId, CancellationToken cancellationToken = default)
    {
        await GetCurrentUserAsync(currentUserId, cancellationToken);

        var city = await _unitOfWork.Cities.GetByIdIncludingDeletedAsync(cityId, cancellationToken)
            ?? throw new KeyNotFoundException("City was not found.");

        if (!city.IsDeleted)
        {
            throw new InvalidOperationException("City is not deleted.");
        }

        city.IsDeleted = false;
        city.DeletedAt = null;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapCity(city);
    }

    // MAYD-51 Phase 2d: CityLocations, the third and last tier under Country -> City. Same
    // "list shows active+deleted together" shape as GetCitiesByCountryAsync above — see
    // ICityLocationRepository.GetByCityIdAsync's own comment for why (the admin page's Restore
    // button would be unreachable otherwise, since a deleted row would never appear in the list).
    public async Task<List<CityLocationDto>> GetCityLocationsByCityAsync(int cityId, CancellationToken cancellationToken = default)
    {
        var cityLocations = await _unitOfWork.CityLocations.GetByCityIdAsync(cityId, cancellationToken);
        return cityLocations.Select(MapCityLocation).ToList();
    }

    public async Task<CityLocationDto> CreateCityLocationAsync(CreateCityLocationDto dto, int currentUserId, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(currentUserId, cancellationToken);
        ValidateName(dto.EnglishName, "English city location name");
        ValidateName(dto.ArabicName, "Arabic city location name");

        await EnsureCityExistsAsync(dto.CityId, cancellationToken);

        var cityLocation = new CityLocation
        {
            EnglishName = dto.EnglishName.Trim(),
            ArabicName = dto.ArabicName.Trim(),
            CityId = dto.CityId,
            CreatedBy = currentUser.UserId,
            IsActive = true
        };

        await _unitOfWork.CityLocations.AddAsync(cityLocation, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapCityLocation(cityLocation);
    }

    public async Task<CityLocationDto> UpdateCityLocationAsync(UpdateCityLocationDto dto, int currentUserId, CancellationToken cancellationToken = default)
    {
        await GetCurrentUserAsync(currentUserId, cancellationToken);
        ValidateName(dto.EnglishName, "English city location name");
        ValidateName(dto.ArabicName, "Arabic city location name");

        var cityLocation = await _unitOfWork.CityLocations.GetByIdAsync(dto.Id, cancellationToken)
            ?? throw new KeyNotFoundException("City location was not found.");

        await EnsureCityExistsAsync(dto.CityId, cancellationToken);

        cityLocation.EnglishName = dto.EnglishName.Trim();
        cityLocation.ArabicName = dto.ArabicName.Trim();
        cityLocation.CityId = dto.CityId;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapCityLocation(cityLocation);
    }

    // Plain, unconditional soft-delete — matching DeleteCityAsync above, NOT DeleteCountryAsync's
    // "block if active children exist" pattern. Reason: nothing in this codebase currently persists
    // a real FK to CityLocation.Id — Association deliberately does NOT (AssociationDto's own
    // comment) and no CityLocation-consuming module exists yet — so there is nothing to protect
    // against by blocking. Revisit if a future phase adds a real dependent.
    public async Task DeleteCityLocationAsync(int cityLocationId, int currentUserId, CancellationToken cancellationToken = default)
    {
        await GetCurrentUserAsync(currentUserId, cancellationToken);

        var cityLocation = await _unitOfWork.CityLocations.GetByIdAsync(cityLocationId, cancellationToken)
            ?? throw new KeyNotFoundException("City location was not found.");

        _unitOfWork.CityLocations.Remove(cityLocation);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<CityLocationDto> RestoreCityLocationAsync(int cityLocationId, int currentUserId, CancellationToken cancellationToken = default)
    {
        await GetCurrentUserAsync(currentUserId, cancellationToken);

        var cityLocation = await _unitOfWork.CityLocations.GetByIdIncludingDeletedAsync(cityLocationId, cancellationToken)
            ?? throw new KeyNotFoundException("City location was not found.");

        if (!cityLocation.IsDeleted)
        {
            throw new InvalidOperationException("City location is not deleted.");
        }

        cityLocation.IsDeleted = false;
        cityLocation.DeletedAt = null;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapCityLocation(cityLocation);
    }

    private async Task<User> GetCurrentUserAsync(int currentUserId, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(currentUserId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Current user was not found.");

        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("Current user is inactive.");
        }

        return user;
    }

    private async Task EnsureCountryExistsAsync(int countryId, CancellationToken cancellationToken)
    {
        if (await _unitOfWork.Countries.GetByIdAsync(countryId, cancellationToken) is null)
        {
            throw new KeyNotFoundException("Country was not found.");
        }
    }

    // Same real, active-only City-validation precedent CreateCityAsync's own EnsureCountryExistsAsync
    // establishes for CountryId — ICityRepository.GetByIdAsync is active-only, so a soft-deleted City
    // correctly fails this the same way an unknown one does.
    private async Task EnsureCityExistsAsync(int cityId, CancellationToken cancellationToken)
    {
        if (await _unitOfWork.Cities.GetByIdAsync(cityId, cancellationToken) is null)
        {
            throw new KeyNotFoundException("City was not found.");
        }
    }

    private static void ValidateName(string value, string fieldLabel)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{fieldLabel} is required.");
        }

        if (value.Trim().Length > MaxNameLength)
        {
            throw new InvalidOperationException($"{fieldLabel} cannot exceed {MaxNameLength} characters.");
        }
    }

    private static CountryDto MapCountry(Country country) => new()
    {
        Id = country.Id,
        CountryEnglishName = country.EnglishName,
        CountryArabicName = country.ArabicName,
        IsDeleted = country.IsDeleted
    };

    private static CityDto MapCity(City city) => new()
    {
        Id = city.Id,
        CityEnglishName = city.EnglishName,
        CityArabicName = city.ArabicName,
        CountryId = city.CountryId,
        IsDeleted = city.IsDeleted
    };

    private static CityLocationDto MapCityLocation(CityLocation cityLocation) => new()
    {
        Id = cityLocation.Id,
        EnglishName = cityLocation.EnglishName,
        ArabicName = cityLocation.ArabicName,
        CityId = cityLocation.CityId,
        IsDeleted = cityLocation.IsDeleted
    };
}
