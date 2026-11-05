using Maydan.Application.DTOs.Common;
using Maydan.Application.DTOs.Locations;
using Maydan.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maydan.API.Controllers;

[Route("api/[controller]")]
public class CityController : ApiControllerBase
{
    private readonly ILocationService _locationService;

    public CityController(ILocationService locationService)
    {
        _locationService = locationService;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var response = await _locationService.GetCitiesAsync(cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult> Create([FromBody] CreateCityDto dto, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(ApiResponse<CityDto>.FailureResponse("Current user id is required.", "مطلوب معرف المستخدم الحالي."));
        }

        try
        {
            var response = await _locationService.CreateCityAsync(dto, currentUserId, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    [Authorize]
    [HttpPut("{cityId:int}")]
    public async Task<ActionResult> Update(int cityId, [FromBody] UpdateCityDto dto, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(ApiResponse<CityDto>.FailureResponse("Current user id is required.", "مطلوب معرف المستخدم الحالي."));
        }

        try
        {
            var response = await _locationService.UpdateCityAsync(cityId, dto, currentUserId, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    [Authorize]
    [HttpDelete("{cityId:int}")]
    public async Task<ActionResult> Delete(int cityId, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(ApiResponse<bool>.FailureResponse("Current user id is required.", "مطلوب معرف المستخدم الحالي."));
        }

        try
        {
            var response = await _locationService.DeleteCityAsync(cityId, currentUserId, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    [Authorize]
    [HttpPatch("restore/{cityId:int}")]
    public async Task<ActionResult> Restore(int cityId, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(ApiResponse<CityDto>.FailureResponse("Current user id is required.", "مطلوب معرف المستخدم الحالي."));
        }

        try
        {
            var response = await _locationService.RestoreCityAsync(cityId, currentUserId, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }
}
