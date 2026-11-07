using Maydan.Application.DTOs.Common;
using Maydan.Application.DTOs.Projects;
using Maydan.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maydan.API.Controllers;

/// <summary>
/// EN: REST API Controller for managing Project Workers, filtering, and activation state.
/// AR: وحدة التحكم لإدارة عمال المشروع، الفلترة، وتفعيل أو إيقاف العمال.
/// </summary>
[Authorize]
[Route("api/[controller]")]
public class ProjectWorkersController : ApiControllerBase
{
    private readonly IProjectWorkerService _projectWorkerService;

    public ProjectWorkersController(IProjectWorkerService projectWorkerService)
    {
        _projectWorkerService = projectWorkerService;
    }

    private ObjectResult UnauthorizedResponse()
    {
        var response = ApiResponse<object?>.FailureResponse("Unauthorized.", "المستخدم غير مصرح له.");
        response.StatusCode = 401;
        return StatusCode(401, response);
    }

    /// <summary>
    /// EN: Retrieves all workers for a project with optional filters (Name, Civilian ID, Active status).
    /// AR: جلب كافة عمال المشروع مع دعم الفلترة حسب الاسم، الرقم الوطني، وحالة التفعيل.
    /// </summary>
    [HttpGet("project/{projectId:int}", Name = "GetProjectWorkersList")]
    public async Task<ActionResult> GetByProjectId(
        int projectId,
        [FromQuery] string? searchByWorkerName = null,
        [FromQuery] string? searchByWorkerCivilainId = null,
        [FromQuery] string? searchByWorkerCivilianId = null,
        [FromQuery] bool? isWorkerActive = null,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentUserId(out var currentUserId)) return UnauthorizedResponse();

        try
        {
            var effectiveCivilIdFilter = !string.IsNullOrWhiteSpace(searchByWorkerCivilainId)
                ? searchByWorkerCivilainId
                : searchByWorkerCivilianId;

            var response = await _projectWorkerService.GetByProjectIdAsync(
                projectId,
                searchByWorkerName,
                effectiveCivilIdFilter,
                isWorkerActive,
                currentUserId,
                cancellationToken);

            return StatusCode(response.StatusCode, response);
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    /// <summary>
    /// EN: Retrieves a single project worker assignment by ID.
    /// AR: جلب بيانات تعيين عامل في مشروع بواسطة المعرف.
    /// </summary>
    [HttpGet("{id:int}", Name = "GetProjectWorkerDetails")]
    public async Task<ActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId)) return UnauthorizedResponse();

        try
        {
            var response = await _projectWorkerService.GetByIdAsync(id, currentUserId, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    /// <summary>
    /// EN: Assigns a new worker to a project (Blocked for Oversight/Asiza role).
    /// AR: تعيين عامل جديد في المشروع (ممنوع للدور الرقابي أسيزا).
    /// </summary>
    [HttpPost(Name = "CreateProjectWorker")]
    public async Task<ActionResult> Create([FromBody] CreateProjectWorkerDto dto, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId)) return UnauthorizedResponse();

        try
        {
            var response = await _projectWorkerService.CreateAsync(dto, currentUserId, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    /// <summary>
    /// EN: Updates a project worker's daily wage or assignment details (Blocked for Oversight/Asiza role).
    /// AR: تحديث الأجر اليومي أو بيانات تعيين العامل في المشروع (ممنوع للدور الرقابي أسيزا).
    /// </summary>
    [HttpPut("{id:int}", Name = "UpdateProjectWorkerWage")]
    public async Task<ActionResult> Update(int id, [FromBody] UpdateProjectWorkerDto dto, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId)) return UnauthorizedResponse();

        try
        {
            var response = await _projectWorkerService.UpdateAsync(id, dto, currentUserId, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    /// <summary>
    /// EN: Activates a worker on the project (Supports both PATCH and PUT/POST if needed, blocked for Asiza).
    /// AR: تفعيل العامل في المشروع (ممنوع للدور الرقابي أسيزا).
    /// </summary>
    [HttpPatch("{id:int}/activate", Name = "ActivateProjectWorkerPatch")]
    [HttpPut("{id:int}/activate", Name = "ActivateProjectWorkerPut")]
    public async Task<ActionResult> Activate(int id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId)) return UnauthorizedResponse();

        try
        {
            var response = await _projectWorkerService.ActivateAsync(id, currentUserId, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    /// <summary>
    /// EN: Deactivates a worker on the project (Supports both PATCH and PUT/POST if needed, blocked for Asiza).
    /// AR: إيقاف تفعيل العامل في المشروع (ممنوع للدور الرقابي أسيزا).
    /// </summary>
    [HttpPatch("{id:int}/deactivate", Name = "DeactivateProjectWorkerPatch")]
    [HttpPut("{id:int}/deactivate", Name = "DeactivateProjectWorkerPut")]
    public async Task<ActionResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId)) return UnauthorizedResponse();

        try
        {
            var response = await _projectWorkerService.DeactivateAsync(id, currentUserId, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }
}