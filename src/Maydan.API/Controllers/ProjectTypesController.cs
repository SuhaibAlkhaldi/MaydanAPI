using Maydan.Application.DTOs.Common;
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
    //changes the return to action-result to comaptapile with other controller cause he is return (response.StatusCode, response)
    public async Task<ActionResult> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var response = await _projectService.GetProjectTypesAsync(cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }
}