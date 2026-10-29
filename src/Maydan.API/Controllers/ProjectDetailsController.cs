using Maydan.Application.DTOs.Common;
using Maydan.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maydan.API.Controllers;

// Read-only project details (project info, location manager, association supervisors, workers and wages,
//service requests and payment info). All logic and the access rule live in IProjectDetailsService
//only the Bayt Al Urdon admin and the admin of the project's Production House may see it.
[ApiController]
[Authorize]
[Route("api/project-details")]
public class ProjectDetailsController : ApiControllerBase
{
    private readonly IProjectDetailsService _service;
    private readonly ILogger<ProjectDetailsController> _logger;

    public ProjectDetailsController(IProjectDetailsService service, ILogger<ProjectDetailsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    // GET api/project-details/{projectId}
    [HttpGet("{projectId:int}")]
    public async Task<ActionResult> GetProjectFullDetails(int projectId, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized(new ApiResponse<object?>(false, "المستخدم غير مصرح له.", "Unauthorized.", null, 401));

        try
        {
            var result = await _service.GetProjectFullDetailsAsync(userId, projectId, cancellationToken);
            return StatusCode(result.StatusCode, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching project details for ProjectId: {ProjectId}", projectId);
            return HandleException(ex);
        }
    }

    // GET api/project-details/{projectId}/supervisors
    [HttpGet("{projectId:int}/supervisors")]
    public async Task<ActionResult> GetProjectSupervisors(int projectId, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized(new ApiResponse<object?>(false, "المستخدم غير مصرح له.", "Unauthorized.", null, 401));

        try
        {
            var result = await _service.GetProjectSupervisorsAsync(userId, projectId, cancellationToken);
            return StatusCode(result.StatusCode, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching project supervisors for ProjectId: {ProjectId}", projectId);
            return HandleException(ex);
        }
    }

    // GET api/project-details/{projectId}/workers
    [HttpGet("{projectId:int}/workers")]
    public async Task<ActionResult> GetProjectWorkers(int projectId, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized(new ApiResponse<object?>(false, "المستخدم غير مصرح له.", "Unauthorized.", null, 401));

        try
        {
            var result = await _service.GetProjectWorkersAsync(userId, projectId, cancellationToken);
            return StatusCode(result.StatusCode, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching project workers for ProjectId: {ProjectId}", projectId);
            return HandleException(ex);
        }
    }
}
