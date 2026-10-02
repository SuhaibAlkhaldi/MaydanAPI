using Maydan.API.Models.Projects;
using Maydan.Application.DTOs.Projects;
using Maydan.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maydan.API.Controllers;


[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProjectsController : ApiControllerBase
{
    private readonly IProjectService _projectService;
    private readonly IFileStorageService _fileStorageService;

    private const string WorkPermitsSubfolder = "work-permits";

    public ProjectsController(IProjectService projectService, IFileStorageService fileStorageService)
    {
        _projectService = projectService;
        _fileStorageService = fileStorageService;
    }

    [HttpGet]
    public async Task<ActionResult<List<ProjectDto>>> GetAll(
        [FromQuery] ProjectQueryDto query,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _projectService.GetAllAsync(query, cancellationToken));
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProjectDto>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _projectService.GetByIdAsync(id, cancellationToken));
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    [HttpPost]
    public async Task<ActionResult<ProjectDto>> Create(
        [FromForm] CreateProjectRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(new { message = "Current user id is required." });
        }

        try
        {

            return Ok(await _projectService.GetAllAsync(currentUserId, query, cancellationToken));

            string workPermitImagePath;
            await using (var stream = request.WorkPermitImage.OpenReadStream())
            {
                workPermitImagePath = await _fileStorageService.SaveAsync(
                    stream,
                    request.WorkPermitImage.FileName,
                    WorkPermitsSubfolder,
                    cancellationToken);
            }

            var dto = new CreateProjectDto
            {
                ProjectNameEn = request.ProjectNameEn,
                ProjectNameAr = request.ProjectNameAr,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                ProjectTypeId = request.ProjectTypeId,
                ProducerUserId = request.ProducerUserId,
                LocationManagerUserId = request.LocationManagerUserId,
                WorkPermitImagePath = workPermitImagePath
            };

            var result = await _projectService.CreateAsync(dto, currentUserId, cancellationToken);
            return Created($"/api/Projects/{result.Id}", result);
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProjectDto>> GetById(
        int id,

    [HttpPut]
    public async Task<ActionResult<ProjectDto>> Update(
        [FromForm] UpdateProjectRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(new { message = "Current user id is required." });
        }

        try
        {

            return Ok(await _projectService.GetByIdAsync(currentUserId, id, cancellationToken));
            string? workPermitImagePath = null;
            if (request.WorkPermitImage is not null)
            {
                await using var stream = request.WorkPermitImage.OpenReadStream();
                workPermitImagePath = await _fileStorageService.SaveAsync(
                    stream,
                    request.WorkPermitImage.FileName,
                    WorkPermitsSubfolder,
                    cancellationToken);
            }

            var dto = new UpdateProjectDto
            {
                ProjectNameEn = request.ProjectNameEn,
                ProjectNameAr = request.ProjectNameAr,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                ProjectTypeId = request.ProjectTypeId,
                ProducerUserId = request.ProducerUserId,
                LocationManagerUserId = request.LocationManagerUserId,
                WorkPermitImagePath = workPermitImagePath
            };

            var result = await _projectService.UpdateAsync(request.Id, dto, currentUserId, cancellationToken);
            return Ok(result);
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    [HttpPost]
    public async Task<ActionResult<ProjectDto>> Create(
        [FromForm] CreateProjectRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(new { message = "Current user id is required." });
        }

        try
        {
            string workPermitImagePath;
            await using (var stream = request.WorkPermitImage.OpenReadStream())
            {
                workPermitImagePath = await _fileStorageService.SaveAsync(
                    stream,
                    request.WorkPermitImage.FileName,
                    WorkPermitsSubfolder,
                    cancellationToken);
            }

            var dto = new CreateProjectDto
            {
                ProjectNameEn = request.ProjectNameEn,
                ProjectNameAr = request.ProjectNameAr,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                ProjectTypeId = request.ProjectTypeId,
                ProducerUserId = request.ProducerUserId,
                LocationManagerUserId = request.LocationManagerUserId,
                WorkPermitImagePath = workPermitImagePath
            };

            var result = await _projectService.CreateAsync(dto, currentUserId, cancellationToken);
            return Created($"/api/Projects/{result.Id}", result);
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    [HttpPut]
    public async Task<ActionResult<ProjectDto>> Update(
        [FromForm] UpdateProjectRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(new { message = "Current user id is required." });
        }

        try
        {
            string? workPermitImagePath = null;
            if (request.WorkPermitImage is not null)
            {
                await using var stream = request.WorkPermitImage.OpenReadStream();
                workPermitImagePath = await _fileStorageService.SaveAsync(
                    stream,
                    request.WorkPermitImage.FileName,
                    WorkPermitsSubfolder,
                    cancellationToken);
            }

            var dto = new UpdateProjectDto
            {
                ProjectNameEn = request.ProjectNameEn,
                ProjectNameAr = request.ProjectNameAr,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                ProjectTypeId = request.ProjectTypeId,
                ProducerUserId = request.ProducerUserId,
                LocationManagerUserId = request.LocationManagerUserId,
                WorkPermitImagePath = workPermitImagePath
            };

            var result = await _projectService.UpdateAsync(request.Id, dto, currentUserId, cancellationToken);
            return Ok(result);
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    [HttpDelete]
    public async Task<IActionResult> Delete(
        [FromQuery] int id,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(new { message = "Current user id is required." });
        }

        try
        {
            await _projectService.DeleteAsync(id, currentUserId, cancellationToken);
            return NoContent();
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    [HttpPatch("restore")]
    public async Task<ActionResult<ProjectDto>> Restore(
        [FromQuery] int projectId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(new { message = "Current user id is required." });
        }

        try
        {
            return Ok(await _projectService.RestoreAsync(projectId, currentUserId, cancellationToken));
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }
}
