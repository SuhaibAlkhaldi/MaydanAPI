using Maydan.Application.DTOs.Common;
using Maydan.Application.DTOs.Locations;
using Maydan.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maydan.API.Controllers;

[Authorize]
[Route("api/[controller]")]
public class CityLocationsController : ApiControllerBase
{
    private readonly ILocationService _locationService;

    public CityLocationsController(ILocationService locationService)
    {
        _locationService = locationService;
    }

    [HttpGet("city/{cityId:int}")]
    public async Task<ActionResult> GetByCity(int cityId, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _locationService.GetCityLocationsByCityAsync(cityId, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] CreateCityLocationDto dto, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(ApiResponse<CityLocationDto>.FailureResponse("Current user id is required.", "مطلوب معرف المستخدم الحالي."));
        }

        try
        {
            var response = await _locationService.CreateCityLocationAsync(dto, currentUserId, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    [HttpPut]
    public async Task<ActionResult> Update([FromBody] UpdateCityLocationDto dto, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(ApiResponse<CityLocationDto>.FailureResponse("Current user id is required.", "مطلوب معرف المستخدم الحالي."));
        }

        try
        {
            var response = await _locationService.UpdateCityLocationAsync(dto, currentUserId, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    [HttpDelete("{cityLocationId:int}")]
    public async Task<ActionResult> Delete(int cityLocationId, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(ApiResponse<bool>.FailureResponse("Current user id is required.", "مطلوب معرف المستخدم الحالي."));
        }

        try
        {
            var response = await _locationService.DeleteCityLocationAsync(cityLocationId, currentUserId, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    [HttpPatch("restore/{cityLocationId:int}")]
    public async Task<ActionResult> Restore(int cityLocationId, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(ApiResponse<CityLocationDto>.FailureResponse("Current user id is required.", "مطلوب معرف المستخدم الحالي."));
        }

        try
        {
            var response = await _locationService.RestoreCityLocationAsync(cityLocationId, currentUserId, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }
}