using Maydan.Application.DTOs.Common;
using Maydan.Application.DTOs.Projects;
using Maydan.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maydan.API.Controllers;

/// <summary>
/// EN: REST API Controller for managing Project Associations and their operational locations.
/// AR: وحدة التحكم لإدارة جمعيات المشروع ومواقعها الجغرافية.
/// </summary>
[Authorize]
[Route("api/[controller]")]
public class ProjectAssociationsController : ApiControllerBase
{
    private readonly IProjectAssociationService _projectAssociationService;

    public ProjectAssociationsController(IProjectAssociationService projectAssociationService)
    {
        _projectAssociationService = projectAssociationService;
    }

    private ObjectResult UnauthorizedResponse()
    {
        var response = ApiResponse<object?>.FailureResponse("Unauthorized.", "المستخدم غير مصرح له.");
        response.StatusCode = 401;
        return StatusCode(401, response);
    }

    /// <summary>
    /// EN: Retrieves all associations and their locations for a specific project.
    /// AR: جلب كافة الجمعيات ومواقعها المرتبطة بمشروع محدد.
    /// </summary>
    [HttpGet("project/{projectId:int}", Name = "GetProjectAssociationsList")]
    public async Task<ActionResult> GetByProjectId(int projectId, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId)) return UnauthorizedResponse();

        try
        {
            var response = await _projectAssociationService.GetByProjectIdAsync(projectId, currentUserId, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    /// <summary>
    /// EN: Retrieves a single project association record by its ID.
    /// AR: جلب تفاصيل ربط جمعية بمشروع بواسطة المعرف.
    /// </summary>
    [HttpGet("{id:int}", Name = "GetProjectAssociationDetails")]
    public async Task<ActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId)) return UnauthorizedResponse();

        try
        {
            var response = await _projectAssociationService.GetByIdAsync(id, currentUserId, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    /// <summary>
    /// EN: Links an association and its locations to a project (Blocked for Oversight/Asiza role).
    /// AR: إضافة ربط جديد بين جمعية ومشروع مع المواقع (ممنوع للدور الرقابي أسيزا).
    /// </summary>
    [HttpPost(Name = "CreateProjectAssociation")]
    public async Task<ActionResult> Create([FromBody] CreateProjectAssociationDto dto, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId)) return UnauthorizedResponse();

        try
        {
            var response = await _projectAssociationService.CreateAsync(dto, currentUserId, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    /// <summary>
    /// EN: Updates an existing project association and replaces its locations (Blocked for Oversight/Asiza role).
    /// AR: تعديل ربط جمعية بمشروع وتحديث مواقعها (ممنوع للدور الرقابي أسيزا).
    /// </summary>
    [HttpPut("{id:int}", Name = "UpdateProjectAssociation")]
    public async Task<ActionResult> Update(int id, [FromBody] UpdateProjectAssociationDto dto, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId)) return UnauthorizedResponse();

        try
        {
            var response = await _projectAssociationService.UpdateAsync(id, dto, currentUserId, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    /// <summary>
    /// EN: Soft-deletes a project association record and its locations (Blocked for Oversight/Asiza role).
    /// AR: حذف ارتباط جمعية بمشروع ومواقعها منطقياً (ممنوع للدور الرقابي أسيزا).
    /// </summary>
    [HttpDelete("{id:int}", Name = "DeleteProjectAssociation")]
    public async Task<ActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId)) return UnauthorizedResponse();

        try
        {
            var response = await _projectAssociationService.DeleteAsync(id, currentUserId, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }
}