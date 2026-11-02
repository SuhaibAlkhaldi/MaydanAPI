using Maydan.Application.DTOs.Common;
using Maydan.Application.DTOs.Locations;
using Maydan.Application.Interfaces;
using Maydan.Domain.Entities;

namespace Maydan.Application.Services;

public class LocationService : ILocationService
{
    private const int MaxNameLength = 200;
    private readonly IUnitOfWork _unitOfWork;

    public LocationService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<List<CountryDto>>> GetCountriesAsync(CancellationToken cancellationToken = default)
    {
        var countries = await _unitOfWork.Countries.GetAllAsync(cancellationToken);
        var dtos = countries
            .DistinctBy(x => (x.ArabicName, x.EnglishName))
            .Select(MapCountry).ToList();

        return ApiResponse<List<CountryDto>>.SuccessResponse(dtos, "Countries retrieved successfully.", "تم استرجاع الدول بنجاح.");
    }

    public async Task<ApiResponse<CountryDto>> GetCountryByIdAsync(int countryId, CancellationToken cancellationToken = default)
    {
        var country = await _unitOfWork.Countries.GetByIdAsync(countryId, cancellationToken);
        if (country == null)
            return ApiResponse<CountryDto>.FailureResponse("Country was not found.", "الدولة غير موجودة.");

        return ApiResponse<CountryDto>.SuccessResponse(MapCountry(country), "Country retrieved successfully.", "تم استرجاع الدولة بنجاح.");
    }

    public async Task<ApiResponse<CountryDto>> CreateCountryAsync(CreateCountryDto dto, int currentUserId, CancellationToken cancellationToken = default)
    {
        var (currentUser, authError) = await GetCurrentUserAsync<CountryDto>(currentUserId, cancellationToken);
        if (authError != null) return authError;

        var valErrorEn = ValidateName(dto.CountryEnglishName, "English country name");
        if (valErrorEn != null) return ApiResponse<CountryDto>.FailureResponse(valErrorEn, "اسم الدولة بالإنجليزية غير صالح.");

        var valErrorAr = ValidateName(dto.CountryArabicName, "Arabic country name");
        if (valErrorAr != null) return ApiResponse<CountryDto>.FailureResponse(valErrorAr, "اسم الدولة بالعربية غير صالح.");

        var country = new Country
        {
            EnglishName = dto.CountryEnglishName.Trim(),
            ArabicName = dto.CountryArabicName.Trim(),
            CreatedBy = currentUser!.UserId,
            IsActive = true
        };

        await _unitOfWork.Countries.AddAsync(country, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<CountryDto>.SuccessResponse(MapCountry(country), "Country created successfully.", "تم إنشاء الدولة بنجاح.", 201);
    }

    public async Task<ApiResponse<CountryDto>> UpdateCountryAsync(UpdateCountryDto dto, int currentUserId, CancellationToken cancellationToken = default)
    {
        var (currentUser, authError) = await GetCurrentUserAsync<CountryDto>(currentUserId, cancellationToken);
        if (authError != null) return authError;

        var valErrorEn = ValidateName(dto.CountryEnglishName, "English country name");
        if (valErrorEn != null) return ApiResponse<CountryDto>.FailureResponse(valErrorEn, "اسم الدولة بالإنجليزية غير صالح.");

        var valErrorAr = ValidateName(dto.CountryArabicName, "Arabic country name");
        if (valErrorAr != null) return ApiResponse<CountryDto>.FailureResponse(valErrorAr, "اسم الدولة بالعربية غير صالح.");

        var country = await _unitOfWork.Countries.GetByIdAsync(dto.Id, cancellationToken);
        if (country == null)
            return ApiResponse<CountryDto>.FailureResponse("Country was not found.", "الدولة غير موجودة.");

        country.EnglishName = dto.CountryEnglishName.Trim();
        country.ArabicName = dto.CountryArabicName.Trim();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<CountryDto>.SuccessResponse(MapCountry(country), "Country updated successfully.", "تم تحديث الدولة بنجاح.");
    }

    public async Task<ApiResponse<bool>> DeleteCountryAsync(int countryId, int currentUserId, CancellationToken cancellationToken = default)
    {
        var (currentUser, authError) = await GetCurrentUserAsync<bool>(currentUserId, cancellationToken);
        if (authError != null) return authError;

        var country = await _unitOfWork.Countries.GetByIdAsync(countryId, cancellationToken);
        if (country == null)
            return ApiResponse<bool>.FailureResponse("Country was not found.", "الدولة غير موجودة.");

        var activeCities = await _unitOfWork.Cities.GetByCountryIdAsync(countryId, cancellationToken);
        if (activeCities.Any(c => !c.IsDeleted))
        {
            return ApiResponse<bool>.FailureResponse("Cannot delete a country that still has active cities.", "لا يمكن حذف دولة تحتوي على مدن نشطة.");
        }

        _unitOfWork.Countries.Remove(country);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.SuccessResponse(true, "Country deleted successfully.", "تم حذف الدولة بنجاح.");
    }

    public async Task<ApiResponse<CountryDto>> RestoreCountryAsync(int countryId, int currentUserId, CancellationToken cancellationToken = default)
    {
        var (currentUser, authError) = await GetCurrentUserAsync<CountryDto>(currentUserId, cancellationToken);
        if (authError != null) return authError;

        var country = await _unitOfWork.Countries.GetByIdIncludingDeletedAsync(countryId, cancellationToken);
        if (country == null)
            return ApiResponse<CountryDto>.FailureResponse("Country was not found.", "الدولة غير موجودة.");

        if (!country.IsDeleted)
        {
            return ApiResponse<CountryDto>.FailureResponse("Country is not deleted.", "الدولة ليست محذوفة.");
        }

        country.IsDeleted = false;
        country.DeletedAt = null;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<CountryDto>.SuccessResponse(MapCountry(country), "Country restored successfully.", "تم استعادة الدولة بنجاح.");
    }

    public async Task<ApiResponse<List<CityDto>>> GetCitiesByCountryAsync(int countryId, CancellationToken cancellationToken = default)
    {
        var cities = await _unitOfWork.Cities.GetByCountryIdAsync(countryId, cancellationToken);
        var dtos = cities.Select(MapCity).ToList();
        return ApiResponse<List<CityDto>>.SuccessResponse(dtos, "Cities retrieved successfully.", "تم استرجاع المدن بنجاح.");
    }

    public async Task<ApiResponse<CityDto>> CreateCityAsync(CreateCityDto dto, int currentUserId, CancellationToken cancellationToken = default)
    {
        var (currentUser, authError) = await GetCurrentUserAsync<CityDto>(currentUserId, cancellationToken);
        if (authError != null) return authError;

        var valErrorEn = ValidateName(dto.CityEnglishName, "English city name");
        if (valErrorEn != null) return ApiResponse<CityDto>.FailureResponse(valErrorEn, "اسم المدينة بالإنجليزية غير صالح.");

        var valErrorAr = ValidateName(dto.CityArabicName, "Arabic city name");
        if (valErrorAr != null) return ApiResponse<CityDto>.FailureResponse(valErrorAr, "اسم المدينة بالعربية غير صالح.");

        if (await _unitOfWork.Countries.GetByIdAsync(dto.CountryId, cancellationToken) == null)
            return ApiResponse<CityDto>.FailureResponse("Country was not found.", "الدولة غير موجودة.");

        var city = new City
        {
            EnglishName = dto.CityEnglishName.Trim(),
            ArabicName = dto.CityArabicName.Trim(),
            CountryId = dto.CountryId,
            CreatedBy = currentUser!.UserId,
            IsActive = true
        };

        await _unitOfWork.Cities.AddAsync(city, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<CityDto>.SuccessResponse(MapCity(city), "City created successfully.", "تم إنشاء المدينة بنجاح.", 201);
    }

    public async Task<ApiResponse<CityDto>> UpdateCityAsync(int cityId, UpdateCityDto dto, int currentUserId, CancellationToken cancellationToken = default)
    {
        var (currentUser, authError) = await GetCurrentUserAsync<CityDto>(currentUserId, cancellationToken);
        if (authError != null) return authError;

        var valErrorEn = ValidateName(dto.CityEnglishName, "English city name");
        if (valErrorEn != null) return ApiResponse<CityDto>.FailureResponse(valErrorEn, "اسم المدينة بالإنجليزية غير صالح.");

        var valErrorAr = ValidateName(dto.CityArabicName, "Arabic city name");
        if (valErrorAr != null) return ApiResponse<CityDto>.FailureResponse(valErrorAr, "اسم المدينة بالعربية غير صالح.");

        var city = await _unitOfWork.Cities.GetByIdAsync(cityId, cancellationToken);
        if (city == null)
            return ApiResponse<CityDto>.FailureResponse("City was not found.", "المدينة غير موجودة.");

        if (await _unitOfWork.Countries.GetByIdAsync(dto.CountryId, cancellationToken) == null)
            return ApiResponse<CityDto>.FailureResponse("Country was not found.", "الدولة غير موجودة.");

        city.EnglishName = dto.CityEnglishName.Trim();
        city.ArabicName = dto.CityArabicName.Trim();
        city.CountryId = dto.CountryId;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<CityDto>.SuccessResponse(MapCity(city), "City updated successfully.", "تم تحديث المدينة بنجاح.");
    }

    public async Task<ApiResponse<bool>> DeleteCityAsync(int cityId, int currentUserId, CancellationToken cancellationToken = default)
    {
        var (currentUser, authError) = await GetCurrentUserAsync<bool>(currentUserId, cancellationToken);
        if (authError != null) return authError;

        var city = await _unitOfWork.Cities.GetByIdAsync(cityId, cancellationToken);
        if (city == null)
            return ApiResponse<bool>.FailureResponse("City was not found.", "المدينة غير موجودة.");

        _unitOfWork.Cities.Remove(city);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.SuccessResponse(true, "City deleted successfully.", "تم حذف المدينة بنجاح.");
    }

    public async Task<ApiResponse<CityDto>> RestoreCityAsync(int cityId, int currentUserId, CancellationToken cancellationToken = default)
    {
        var (currentUser, authError) = await GetCurrentUserAsync<CityDto>(currentUserId, cancellationToken);
        if (authError != null) return authError;

        var city = await _unitOfWork.Cities.GetByIdIncludingDeletedAsync(cityId, cancellationToken);
        if (city == null)
            return ApiResponse<CityDto>.FailureResponse("City was not found.", "المدينة غير موجودة.");

        if (!city.IsDeleted)
        {
            return ApiResponse<CityDto>.FailureResponse("City is not deleted.", "المدينة ليست محذوفة.");
        }

        city.IsDeleted = false;
        city.DeletedAt = null;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<CityDto>.SuccessResponse(MapCity(city), "City restored successfully.", "تم استعادة المدينة بنجاح.");
    }

    public async Task<ApiResponse<List<CityLocationDto>>> GetCityLocationsByCityAsync(int cityId, CancellationToken cancellationToken = default)
    {
        var cityLocations = await _unitOfWork.CityLocations.GetByCityIdAsync(cityId, cancellationToken);
        var dtos = cityLocations.Select(MapCityLocation).ToList();
        return ApiResponse<List<CityLocationDto>>.SuccessResponse(dtos, "City locations retrieved successfully.", "تم استرجاع المواقع بنجاح.");
    }

    public async Task<ApiResponse<CityLocationDto>> CreateCityLocationAsync(CreateCityLocationDto dto, int currentUserId, CancellationToken cancellationToken = default)
    {
        var (currentUser, authError) = await GetCurrentUserAsync<CityLocationDto>(currentUserId, cancellationToken);
        if (authError != null) return authError;

        var valErrorEn = ValidateName(dto.EnglishName, "English city location name");
        if (valErrorEn != null) return ApiResponse<CityLocationDto>.FailureResponse(valErrorEn, "اسم الموقع بالإنجليزية غير صالح.");

        var valErrorAr = ValidateName(dto.ArabicName, "Arabic city location name");
        if (valErrorAr != null) return ApiResponse<CityLocationDto>.FailureResponse(valErrorAr, "اسم الموقع بالعربية غير صالح.");

        if (await _unitOfWork.Cities.GetByIdAsync(dto.CityId, cancellationToken) == null)
            return ApiResponse<CityLocationDto>.FailureResponse("City was not found.", "المدينة غير موجودة.");

        var cityLocation = new CityLocation
        {
            EnglishName = dto.EnglishName.Trim(),
            ArabicName = dto.ArabicName.Trim(),
            CityId = dto.CityId,
            CreatedBy = currentUser!.UserId,
            IsActive = true
        };

        await _unitOfWork.CityLocations.AddAsync(cityLocation, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<CityLocationDto>.SuccessResponse(MapCityLocation(cityLocation), "City location created successfully.", "تم إنشاء الموقع بنجاح.", 201);
    }

    public async Task<ApiResponse<CityLocationDto>> UpdateCityLocationAsync(UpdateCityLocationDto dto, int currentUserId, CancellationToken cancellationToken = default)
    {
        var (currentUser, authError) = await GetCurrentUserAsync<CityLocationDto>(currentUserId, cancellationToken);
        if (authError != null) return authError;

        var valErrorEn = ValidateName(dto.EnglishName, "English city location name");
        if (valErrorEn != null) return ApiResponse<CityLocationDto>.FailureResponse(valErrorEn, "اسم الموقع بالإنجليزية غير صالح.");

        var valErrorAr = ValidateName(dto.ArabicName, "Arabic city location name");
        if (valErrorAr != null) return ApiResponse<CityLocationDto>.FailureResponse(valErrorAr, "اسم الموقع بالعربية غير صالح.");

        var cityLocation = await _unitOfWork.CityLocations.GetByIdAsync(dto.Id, cancellationToken);
        if (cityLocation == null)
            return ApiResponse<CityLocationDto>.FailureResponse("City location was not found.", "الموقع غير موجود.");

        if (await _unitOfWork.Cities.GetByIdAsync(dto.CityId, cancellationToken) == null)
            return ApiResponse<CityLocationDto>.FailureResponse("City was not found.", "المدينة غير موجودة.");

        cityLocation.EnglishName = dto.EnglishName.Trim();
        cityLocation.ArabicName = dto.ArabicName.Trim();
        cityLocation.CityId = dto.CityId;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<CityLocationDto>.SuccessResponse(MapCityLocation(cityLocation), "City location updated successfully.", "تم تحديث الموقع بنجاح.");
    }

    public async Task<ApiResponse<bool>> DeleteCityLocationAsync(int cityLocationId, int currentUserId, CancellationToken cancellationToken = default)
    {
        var (currentUser, authError) = await GetCurrentUserAsync<bool>(currentUserId, cancellationToken);
        if (authError != null) return authError;

        var cityLocation = await _unitOfWork.CityLocations.GetByIdAsync(cityLocationId, cancellationToken);
        if (cityLocation == null)
            return ApiResponse<bool>.FailureResponse("City location was not found.", "الموقع غير موجود.");

        _unitOfWork.CityLocations.Remove(cityLocation);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.SuccessResponse(true, "City location deleted successfully.", "تم حذف الموقع بنجاح.");
    }

    public async Task<ApiResponse<CityLocationDto>> RestoreCityLocationAsync(int cityLocationId, int currentUserId, CancellationToken cancellationToken = default)
    {
        var (currentUser, authError) = await GetCurrentUserAsync<CityLocationDto>(currentUserId, cancellationToken);
        if (authError != null) return authError;

        var cityLocation = await _unitOfWork.CityLocations.GetByIdIncludingDeletedAsync(cityLocationId, cancellationToken);
        if (cityLocation == null)
            return ApiResponse<CityLocationDto>.FailureResponse("City location was not found.", "الموقع غير موجود.");

        if (!cityLocation.IsDeleted)
        {
            return ApiResponse<CityLocationDto>.FailureResponse("City location is not deleted.", "الموقع ليس محذوفاً.");
        }

        cityLocation.IsDeleted = false;
        cityLocation.DeletedAt = null;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<CityLocationDto>.SuccessResponse(MapCityLocation(cityLocation), "City location restored successfully.", "تم استعادة الموقع بنجاح.");
    }

    private async Task<(User? User, ApiResponse<T>? Error)> GetCurrentUserAsync<T>(int currentUserId, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(currentUserId, cancellationToken);
        if (user == null)
            return (null, ApiResponse<T>.FailureResponse("Current user was not found.", "المستخدم الحالي غير موجود."));

        if (!user.IsActive)
            return (null, ApiResponse<T>.FailureResponse("Current user is inactive.", "المستخدم الحالي غير فعّال."));

        return (user, null);
    }

    private static string? ValidateName(string value, string fieldLabel)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return $"{fieldLabel} is required.";
        }

        if (value.Trim().Length > MaxNameLength)
        {
            return $"{fieldLabel} cannot exceed {MaxNameLength} characters.";
        }
        return null;
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