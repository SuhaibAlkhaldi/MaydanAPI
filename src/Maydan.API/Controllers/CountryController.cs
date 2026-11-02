using Maydan.Application.DTOs.Common;
using Maydan.Application.DTOs.Locations;
using Maydan.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maydan.API.Controllers;

[Route("api/[controller]")]
public class CountryController : ApiControllerBase
{
    private readonly ILocationService _locationService;

    public CountryController(ILocationService locationService)
    {
        _locationService = locationService;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var response = await _locationService.GetCountriesAsync(cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    [AllowAnonymous]
    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _locationService.GetCountryByIdAsync(id, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult> Create([FromBody] CreateCountryDto dto, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(ApiResponse<CountryDto>.FailureResponse("Current user id is required.", "مطلوب معرف المستخدم الحالي."));
        }

        try
        {
            var response = await _locationService.CreateCountryAsync(dto, currentUserId, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    [Authorize]
    [HttpPut]
    public async Task<ActionResult> Update([FromBody] UpdateCountryDto dto, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(ApiResponse<CountryDto>.FailureResponse("Current user id is required.", "مطلوب معرف المستخدم الحالي."));
        }

        try
        {
            var response = await _locationService.UpdateCountryAsync(dto, currentUserId, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    [Authorize]
    [HttpDelete("{countryId:int}")]
    public async Task<ActionResult> Delete(int countryId, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(ApiResponse<bool>.FailureResponse("Current user id is required.", "مطلوب معرف المستخدم الحالي."));
        }

        try
        {
            var response = await _locationService.DeleteCountryAsync(countryId, currentUserId, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    [Authorize]
    [HttpPatch("restore/{countryId:int}")]
    public async Task<ActionResult> Restore(int countryId, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(ApiResponse<CountryDto>.FailureResponse("Current user id is required.", "مطلوب معرف المستخدم الحالي."));
        }

        try
        {
            var response = await _locationService.RestoreCountryAsync(countryId, currentUserId, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }
}