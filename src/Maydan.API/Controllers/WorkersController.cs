using Maydan.Application.DTOs.Common;
using Maydan.Application.DTOs.Workers;
using Maydan.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maydan.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class WorkersController : ApiControllerBase
{
    private readonly IWorkerService _workerService;

    public WorkersController(IWorkerService workerService)
    {
        _workerService = workerService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<WorkerSummaryDto>>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] int? serviceId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(new ApiResponse<PagedResult<WorkerSummaryDto>>(false, "المستخدم غير مصرح له.", "Unauthorized.", null, 401));
        }

        try
        {
            var response = await _workerService.GetAllAsync(currentUserId, search, serviceId, page, pageSize, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<WorkerDto>>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(new ApiResponse<WorkerDto>(false, "المستخدم غير مصرح له.", "Unauthorized.", null, 401));
        }

        try
        {
            var response = await _workerService.GetByIdAsync(currentUserId, id, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpGet("civil-id/{civilId}")]
    public async Task<ActionResult<ApiResponse<WorkerDto>>> GetByCivilId(
        string civilId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(new ApiResponse<WorkerDto>(false, "المستخدم غير مصرح له.", "Unauthorized.", null, 401));
        }

        try
        {
            var response = await _workerService.GetByCivilIdAsync(currentUserId, civilId, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpGet("association")]
    public async Task<ActionResult<ApiResponse<List<WorkerSummaryDto>>>> GetByAssociationId(
        [FromQuery] int? associationId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(new ApiResponse<List<WorkerSummaryDto>>(false, "المستخدم غير مصرح له.", "Unauthorized.", null, 401));
        }

        try
        {
            var response = await _workerService.GetByAssociationIdAsync(currentUserId, associationId, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpGet("association/deleted")]
    public async Task<ActionResult<ApiResponse<List<WorkerSummaryDto>>>> GetDeletedByAssociationId(
        [FromQuery] int? associationId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(new ApiResponse<List<WorkerSummaryDto>>(false, "المستخدم غير مصرح له.", "Unauthorized.", null, 401));
        }

        try
        {
            var response = await _workerService.GetDeletedByAssociationIdAsync(currentUserId, associationId, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpGet("query")]
    public async Task<ActionResult<ApiResponse<List<WorkerSummaryDto>>>> Query(
        [FromQuery] bool isDeleted = false,
        [FromQuery] int? associationId = null,
        [FromQuery] bool? orderByYearsOfExperienceAscending = null,
        [FromQuery] bool? orderByNameEnAscending = null,
        [FromQuery] bool? orderByNameArAscending = null,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(new ApiResponse<List<WorkerSummaryDto>>(false, "المستخدم غير مصرح له.", "Unauthorized.", null, 401));
        }

        try
        {
            var response = await _workerService.QueryAsync(
                currentUserId,
                isDeleted,
                associationId,
                orderByYearsOfExperienceAscending,
                orderByNameEnAscending,
                orderByNameArAscending,
                cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<WorkerDto>>> Create(
        [FromBody] CreateWorkerDto dto,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(new ApiResponse<WorkerDto>(false, "المستخدم غير مصرح له.", "Unauthorized.", null, 401));
        }

        try
        {
            var response = await _workerService.CreateAsync(currentUserId, dto, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<WorkerDto>>> Update(
        int id,
        [FromBody] UpdateWorkerDto dto,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(new ApiResponse<WorkerDto>(false, "المستخدم غير مصرح له.", "Unauthorized.", null, 401));
        }

        try
        {
            var response = await _workerService.UpdateAsync(currentUserId, id, dto, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(
        int id,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(new ApiResponse<object?>(false, "المستخدم غير مصرح له.", "Unauthorized.", null, 401));
        }

        try
        {
            var response = await _workerService.DeleteAsync(currentUserId, id, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPatch("restore/{id:int}")]
    public async Task<ActionResult<ApiResponse<WorkerDto>>> Restore(
        int id,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(new ApiResponse<WorkerDto>(false, "المستخدم غير مصرح له.", "Unauthorized.", null, 401));
        }

        try
        {
            var response = await _workerService.RestoreAsync(currentUserId, id, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }
}