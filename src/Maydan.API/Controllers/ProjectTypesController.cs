using Maydan.Application.DTOs.Projects;
using Maydan.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maydan.API.Controllers;


[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProjectTypesController : ApiControllerBase
{
    private readonly IProjectService _projectService;

    public ProjectTypesController(IProjectService projectService)
    {
        _projectService = projectService;
    }

    [HttpGet]
    public async Task<ActionResult<List<ProjectTypeDto>>> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _projectService.GetProjectTypesAsync(cancellationToken));
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }
}
