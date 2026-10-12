using Maydan.Application.DTOs.Common;
using Maydan.Application.DTOs.Workers;
using Maydan.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maydan.API.Controllers;

// No-exceptions convention (product decision, 2026-09-30): IWorkerService returns
// ApiResponse<T> directly with the intended HTTP status code baked in (see its own comment) — this
// controller never needs to interpret a thrown exception for an expected business failure, it just
// mirrors the status code the service already decided. The try/catch below is kept purely as a
// safety net for a genuinely unexpected exception (a bug, a DB outage), not for normal flow.
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
    public async Task<ActionResult> GetAll([FromQuery] string? search, [FromQuery] int? serviceId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(new ApiResponse<object?>(false, "المستخدم غير مصرح له.", "Unauthorized.", null, 401));
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
    public async Task<ActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(new ApiResponse<object?>(false, "المستخدم غير مصرح له.", "Unauthorized.", null, 401));
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

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] CreateWorkerDto dto, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(new ApiResponse<object?>(false, "المستخدم غير مصرح له.", "Unauthorized.", null, 401));
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
    public async Task<ActionResult> Update(int id, [FromBody] UpdateWorkerDto dto, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(new ApiResponse<object?>(false, "المستخدم غير مصرح له.", "Unauthorized.", null, 401));
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
    public async Task<ActionResult> Delete(int id, CancellationToken cancellationToken)
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
    public async Task<ActionResult> Restore(int id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(new ApiResponse<object?>(false, "المستخدم غير مصرح له.", "Unauthorized.", null, 401));
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
