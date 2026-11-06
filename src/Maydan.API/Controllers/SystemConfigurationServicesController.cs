using Maydan.Application.DTOs.Common;
using Maydan.Application.DTOs.Services;
using Maydan.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maydan.API.Controllers;

[ApiController]
[Route("api/system-configuration/services")]
[Authorize]
public class SystemConfigurationServicesController : ApiControllerBase
{
    private readonly IServiceConfigurationService _serviceConfigurationService;

    public SystemConfigurationServicesController(IServiceConfigurationService serviceConfigurationService)
    {
        _serviceConfigurationService = serviceConfigurationService;
    }

    [HttpGet]
    public async Task<ActionResult> GetAll(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(new ApiResponse<object?>(false, "المستخدم غير مصرح له.", "Unauthorized.", null, 401));
        }

        try
        {
            var response = await _serviceConfigurationService.GetAllServicesAsync(currentUserId, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpGet("active")]
    public async Task<ActionResult> GetActive(CancellationToken cancellationToken)
    {
        try
        {
            var response = await _serviceConfigurationService.GetActiveServicesAsync(cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(new ApiResponse<object?>(false, "المستخدم غير مصرح له.", "Unauthorized.", null, 401));
        }

        try
        {
            var response = await _serviceConfigurationService.GetServiceByIdAsync(currentUserId, id, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] CreateServiceDto dto, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(new ApiResponse<object?>(false, "المستخدم غير مصرح له.", "Unauthorized.", null, 401));
        }

        try
        {
            var response = await _serviceConfigurationService.CreateServiceAsync(currentUserId, dto, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> Update(int id, [FromBody] UpdateServiceDto dto, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(new ApiResponse<object?>(false, "المستخدم غير مصرح له.", "Unauthorized.", null, 401));
        }

        try
        {
            var response = await _serviceConfigurationService.UpdateServiceAsync(currentUserId, id, dto, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(new ApiResponse<object?>(false, "المستخدم غير مصرح له.", "Unauthorized.", null, 401));
        }

        try
        {
            var response = await _serviceConfigurationService.DeleteServiceAsync(currentUserId, id, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }
}
