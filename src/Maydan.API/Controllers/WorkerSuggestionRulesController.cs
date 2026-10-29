using Microsoft.AspNetCore.Http;

using Maydan.Application.DTOs.Common;
using Maydan.Application.DTOs.WorkerSuggestions;
using Maydan.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maydan.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class WorkerSuggestionRulesController : ApiControllerBase
{
    private readonly IWorkerSuggestionRuleService _ruleService;

    public WorkerSuggestionRulesController(
        IWorkerSuggestionRuleService ruleService)
    {
        _ruleService = ruleService;
    }

    // GET: api/WorkerSuggestionRules
    [HttpGet]
    public async Task<ActionResult> GetAll(
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(new ApiResponse<object?>(
                false, "المستخدم غير مصرح له.",
                "Unauthorized.", null, 401));
        }

        try
        {
            var response = await _ruleService.GetAllAsync(
                currentUserId, cancellationToken);

            return StatusCode(response.StatusCode, response);
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    // GET: api/WorkerSuggestionRules/deleted
    [HttpGet("deleted")]
    public async Task<ActionResult> GetDeleted(
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(new ApiResponse<object?>(
                false, "المستخدم غير مصرح له.",
                "Unauthorized.", null, 401));
        }

        try
        {
            var response = await _ruleService.GetDeletedAsync(
                currentUserId, cancellationToken);

            return StatusCode(response.StatusCode, response);
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    // GET: api/WorkerSuggestionRules/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(new ApiResponse<object?>(
                false, "المستخدم غير مصرح له.",
                "Unauthorized.", null, 401));
        }

        try
        {
            var response = await _ruleService.GetByIdAsync(
                currentUserId, id, cancellationToken);

            return StatusCode(response.StatusCode, response);
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    // POST: api/WorkerSuggestionRules
    [HttpPost]
    public async Task<ActionResult> Create(
        [FromBody] CreateWorkerSuggestionRuleRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(new ApiResponse<object?>(
                false, "المستخدم غير مصرح له.",
                "Unauthorized.", null, 401));
        }

        try
        {
            var response = await _ruleService.CreateAsync(
                currentUserId, request, cancellationToken);

            return StatusCode(response.StatusCode, response);
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    // PUT: api/WorkerSuggestionRules/5

    [HttpPut("{id:int}")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult> Update(
        int id,
        [FromForm] UpdateWorkerSuggestionRuleRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(new ApiResponse<object?>(
                false,
                "المستخدم غير مصرح له.",
                "Unauthorized.",
                null,
                401));
        }

        try
        {
            var response = await _ruleService.UpdateAsync(
                currentUserId,
                id,
                request,
                cancellationToken);

            return StatusCode(response.StatusCode, response);
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }


    // DELETE: api/WorkerSuggestionRules/5
    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(
        int id,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(new ApiResponse<object?>(
                false, "المستخدم غير مصرح له.",
                "Unauthorized.", null, 401));
        }

        try
        {
            var response = await _ruleService.DeleteAsync(
                currentUserId, id, cancellationToken);

            return StatusCode(response.StatusCode, response);
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    // PATCH: api/WorkerSuggestionRules/5/restore
    [HttpPatch("{id:int}/restore")]
    public async Task<ActionResult> Restore(
        int id,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(new ApiResponse<object?>(
                false, "المستخدم غير مصرح له.",
                "Unauthorized.", null, 401));
        }

        try
        {
            var response = await _ruleService.RestoreAsync(
                currentUserId, id, cancellationToken);

            return StatusCode(response.StatusCode, response);
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    // PUT: api/WorkerSuggestionRules/reorder
    [HttpPut("reorder")]
    public async Task<ActionResult> Reorder(
        [FromBody] ReorderWorkerSuggestionRulesRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(new ApiResponse<object?>(
                false, "المستخدم غير مصرح له.",
                "Unauthorized.", null, 401));
        }

        try
        {
            var response = await _ruleService.ReorderAsync(
                currentUserId, request, cancellationToken);

            return StatusCode(response.StatusCode, response);
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }
}

