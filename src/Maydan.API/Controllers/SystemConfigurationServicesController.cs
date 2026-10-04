using Maydan.API.Filters;
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
        try
        {
            if (!TryGetCurrentUserId(out var currentUserId))
            {
                return Unauthorized(new Maydan.Application.DTOs.Common.ApiResponse<object?>(false, "المستخدم غير مصرح له.", "Unauthorized.", null));
            }
            var result = await _serviceConfigurationService.GetAllServicesAsync(currentUserId, cancellationToken);
            return Success(result, "تم جلب الخدمات بنجاح", "Services retrieved successfully.");
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
            var result = await _serviceConfigurationService.GetActiveServicesAsync(cancellationToken);
            return Success(result, "تم جلب الخدمات المتاحة بنجاح", "Active services retrieved successfully.");
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
            return Unauthorized(new Maydan.Application.DTOs.Common.ApiResponse<object?>(false, "المستخدم غير مصرح له.", "Unauthorized.", null));
        }
        try
        {
            var result = await _serviceConfigurationService.GetServiceByIdAsync(currentUserId, id, cancellationToken);
            return Success(result, "تم جلب الخدمة بنجاح", "Service retrieved successfully.");
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
            return Unauthorized(new Maydan.Application.DTOs.Common.ApiResponse<object?>(false, "المستخدم غير مصرح له.", "Unauthorized.", null));
        }
        try
        {
            var created = await _serviceConfigurationService.CreateServiceAsync(currentUserId, dto, cancellationToken);
            return CreatedResponse($"/api/system-configuration/services/{created.Id}", created, "تم إنشاء الخدمة بنجاح", "Service created successfully.");
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
            return Unauthorized(new Maydan.Application.DTOs.Common.ApiResponse<object?>(false, "المستخدم غير مصرح له.", "Unauthorized.", null));
        }
        try
        {
            var updated = await _serviceConfigurationService.UpdateServiceAsync(currentUserId, id, dto, cancellationToken);
            return Success(updated, "تم تحديث الخدمة بنجاح", "Service updated successfully.");
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(new Maydan.Application.DTOs.Common.ApiResponse<object?>(false, "المستخدم غير مصرح له.", "Unauthorized.", null));
        }
        try
        {
            await _serviceConfigurationService.DeleteServiceAsync(currentUserId, id, cancellationToken);
            return Success<object>(null, "تم حذف الخدمة بنجاح", "Service deleted successfully.");
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }
}
