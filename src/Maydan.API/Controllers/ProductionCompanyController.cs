using Maydan.Application.DTOs.Common;
using Maydan.Application.DTOs.ProductionCompanies;
using Maydan.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maydan.API.Controllers;

[Authorize]
[Route("api/[controller]")]
public class ProductionCompanyController : ApiControllerBase
{
    private readonly IProductionCompanyService _productionCompanyService;

    public ProductionCompanyController(IProductionCompanyService productionCompanyService)
    {
        _productionCompanyService = productionCompanyService;
    }

    [HttpGet]
    public async Task<ActionResult> GetAll(
        [FromQuery] string? search, [FromQuery] bool isDeleted, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(new ApiResponse<object?>(false, "المستخدم غير مصرح له.", "Unauthorized.", null, 401));
        }

        try
        {
            var response = await _productionCompanyService.GetAllAsync(currentUserId, search, isDeleted, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception exception)
        {
            return HandleException(exception);
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
            var response = await _productionCompanyService.GetByIdAsync(currentUserId, id, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    [HttpPatch("{id:int}/status/update", Name = "Update Production Company Status")]
    public async Task<ActionResult> UpdateStatus(
        int id, [FromBody] UpdateProductionCompanyStatusDto dto, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(new ApiResponse<object?>(false, "المستخدم غير مصرح له.", "Unauthorized.", null, 401));
        }

        try
        {
            var response = await _productionCompanyService.UpdateStatusAsync(currentUserId, id, dto, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }
}