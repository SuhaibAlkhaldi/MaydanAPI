using Maydan.Application.DTOs.Common;
using Maydan.Application.DTOs.Locations;

namespace Maydan.Application.Interfaces;

public interface ILocationService
{
    Task<ApiResponse<List<CountryDto>>> GetCountriesAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<CountryDto>> GetCountryByIdAsync(int countryId, CancellationToken cancellationToken = default);
    Task<ApiResponse<CountryDto>> CreateCountryAsync(CreateCountryDto dto, int currentUserId, CancellationToken cancellationToken = default);
    Task<ApiResponse<CountryDto>> UpdateCountryAsync(UpdateCountryDto dto, int currentUserId, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> DeleteCountryAsync(int countryId, int currentUserId, CancellationToken cancellationToken = default);
    Task<ApiResponse<CountryDto>> RestoreCountryAsync(int countryId, int currentUserId, CancellationToken cancellationToken = default);

    Task<ApiResponse<List<CityDto>>> GetCitiesByCountryAsync(int countryId, CancellationToken cancellationToken = default);
    Task<ApiResponse<CityDto>> CreateCityAsync(CreateCityDto dto, int currentUserId, CancellationToken cancellationToken = default);
    Task<ApiResponse<CityDto>> UpdateCityAsync(int cityId, UpdateCityDto dto, int currentUserId, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> DeleteCityAsync(int cityId, int currentUserId, CancellationToken cancellationToken = default);
    Task<ApiResponse<CityDto>> RestoreCityAsync(int cityId, int currentUserId, CancellationToken cancellationToken = default);

    Task<ApiResponse<List<CityLocationDto>>> GetCityLocationsByCityAsync(int cityId, CancellationToken cancellationToken = default);
    Task<ApiResponse<CityLocationDto>> CreateCityLocationAsync(CreateCityLocationDto dto, int currentUserId, CancellationToken cancellationToken = default);
    Task<ApiResponse<CityLocationDto>> UpdateCityLocationAsync(UpdateCityLocationDto dto, int currentUserId, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> DeleteCityLocationAsync(int cityLocationId, int currentUserId, CancellationToken cancellationToken = default);
    Task<ApiResponse<CityLocationDto>> RestoreCityLocationAsync(int cityLocationId, int currentUserId, CancellationToken cancellationToken = default);
}