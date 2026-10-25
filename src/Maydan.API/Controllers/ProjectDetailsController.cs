using Maydan.Application.DTOs.Common;
using Maydan.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maydan.API.Controllers;


// Admin (baytalurdon) read-only view: shows a project's full details including
// workers assigned, location manager, association supervisor, service requests, and payment info.
// This controller does NOT create or update any data.
[ApiController]
[Authorize]
[Route("api/project-details")]
public class ProjectDetailsController : ApiControllerBase
{
    private readonly IUnitOfWork _uow;
    private readonly IWorkerProjectLinkRepository _linkRepo;
    private readonly ILogger<ProjectDetailsController> _logger;

    public ProjectDetailsController(IUnitOfWork uow, IWorkerProjectLinkRepository linkRepo, ILogger<ProjectDetailsController> logger)
    {
        _uow = uow;
        _linkRepo = linkRepo;
        _logger = logger;

    }

    
    //  GET api/project-details/{projectId}
    //  Returns the full project view: project info, location manager,
    //  association supervisors, assigned workers + wage, service requests.
    
    [HttpGet("{projectId:int}")]
    public async Task<ActionResult> GetProjectFullDetails(int projectId, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out _))
            return Unauthorized(new ApiResponse<object?>(false, "المستخدم غير مصرح له.", "Unauthorized.", null, 401));

        try
        {
            // 1. Load Project with Producer, LocationManager, ProductionCompany
            var project = await _uow.Projects.GetByIdAsync(projectId, cancellationToken);
            if (project == null)
                return NotFound(new ApiResponse<object?>(false, "المشروع غير موجود.", "Project not found.", null, 404));

            // 2. Association Project Supervisors for this project
            var supervisors = await _uow.AssociationProjectSupervisors
    .GetAllAsync(
        projectId: projectId,
        associationId: null,
        cancellationToken: cancellationToken);
            // 3. Workers assigned to this project (with service & wage details)
            var workerLinks = await _linkRepo.ListByProjectAsync(projectId, cancellationToken);

            // 4. Service Requests for this project
            var allServiceRequests = await _uow.ServiceRequests.GetAllAsync(cancellationToken);
            var projectServiceRequests = allServiceRequests
                .Where(sr => sr.ProjectId == projectId)
                .ToList();

            // Build the response DTO 
            var result = new
            {
                // Project Info 
                Project = new
                {
                    project.Id,
                    project.ProjectNameEn,
                    project.ProjectNameAr,
                    project.StartDate,
                    project.EndDate,
                    ProjectType = project.ProjectType?.NameEn,
                    project.WorkPermitImagePath,
                    project.ProductionCompanyId,
                    ProductionCompanyName = project.ProductionCompany?.EnglishName,
                },

                // Producer (from Production Company) 
                Producer = project.Producer != null ? new
                {
                    project.Producer.UserId,
                    FullNameEn = $"{project.Producer.FirstNameEn} {project.Producer.LastNameEn}",
                    FullNameAr = $"{project.Producer.FirstNameAr} {project.Producer.LastNameAr}",
                    project.Producer.Email,
                    project.Producer.PhoneNumber
                } : null,

                // Location Manager (from Production Company) 
                //  Responsible for tracking worker attendance on the project site
                LocationManager = project.LocationManager != null ? new
                {
                    project.LocationManager.UserId,
                    FullNameEn = $"{project.LocationManager.FirstNameEn} {project.LocationManager.LastNameEn}",
                    FullNameAr = $"{project.LocationManager.FirstNameAr} {project.LocationManager.LastNameAr}",
                    project.LocationManager.Email,
                    project.LocationManager.PhoneNumber
                } : null,

                // Association Project Supervisors
                // Responsible for tracking worker attendance on behalf of the association
                AssociationSupervisors = supervisors?.Select(s => new
                {
                    s.Id,
                    s.UserId,
                    FullNameEn = s.User != null ? $"{s.User.FirstNameEn} {s.User.LastNameEn}" : null,
                    FullNameAr = s.User != null ? $"{s.User.FirstNameAr} {s.User.LastNameAr}" : null,
                    Email = s.User?.Email,
                    PhoneNumber = s.User?.PhoneNumber
                }),

                // Workers Assigned to Project 
                Workers = workerLinks.Select(wl => new
                {
                    WorkerProjectLinkId = wl.Id,
                    wl.WorkerId,
                    WorkerFullName = wl.Worker != null ? $"{wl.Worker.FirstName} {wl.Worker.LastName}" : null,
                    wl.Worker?.CivilId,
                    wl.Worker?.PhoneNumber,
                    wl.IsWorkerActive,

                    // Daily wage comes from the service price
                    wl.DailyWageAmount,

                    // Which service request this worker is assigned through
                    wl.ServiceRequestId,
                    ServiceNameEn = wl.ServiceRequest?.Service?.NameEn,
                    ServiceNameAr = wl.ServiceRequest?.Service?.NameAr,
                    ServicePrice = wl.ServiceRequest?.Service?.Price,

                    AssociationName = wl.ServiceRequest?.Association?.EnglishName,

                    wl.CreatedDate,
                }),

                // Service Requests for this Project 
                // Including expected payment details
                ServiceRequests = projectServiceRequests.Select(sr => new
                {
                    sr.Id,
                    sr.ServiceId,
                    ServiceNameEn = sr.Service?.NameEn,
                    ServiceNameAr = sr.Service?.NameAr,
                    sr.AssociationId,
                    AssociationName = sr.Association?.EnglishName,
                    sr.StartDate,
                    sr.EndDate,
                    sr.ShiftsCount,
                    sr.RequestedWorkersCount,
                    sr.SelectedWorkersCount,
                    sr.AttendanceFrequency,
                    sr.UnitPriceSnapshot,
                    sr.ExpectedTotalAmount,
                    Status = sr.Status.ToString(),
                    sr.AdditionalRequirements,
                    sr.RejectionReason,
                    AssignedWorkers = sr.ServiceRequestWorkers?.Select(srw => new
                    {
                        srw.WorkerId,
                        WorkerName = srw.Worker != null ? $"{srw.Worker.FirstName} {srw.Worker.LastName}" : null,
                    })
                })
            };

            return Ok(new ApiResponse<object>(true, "تم جلب تفاصيل المشروع بنجاح.", "Project details fetched successfully.", result));
        }
        catch (Exception ex)
        {
            _logger.LogError(
        ex,
        "Error fetching project details for ProjectId: {ProjectId}",
        projectId);

            return HandleException(ex);
        }
    }

    
    //  GET api/project-details/{projectId}/workers
    //  Focused view: just the workers assigned to a project with their wages.

    [HttpGet("{projectId:int}/workers")]
    public async Task<ActionResult> GetProjectWorkers(int projectId, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out _))
            return Unauthorized(new ApiResponse<object?>(false, "المستخدم غير مصرح له.", "Unauthorized.", null, 401));

        try
        {
            var project = await _uow.Projects.GetByIdAsync(projectId, cancellationToken);
            if (project == null)
                return NotFound(new ApiResponse<object?>(false, "المشروع غير موجود.", "Project not found.", null, 404));

            var workerLinks = await _linkRepo.ListByProjectAsync(projectId, cancellationToken);

            var result = workerLinks.Select(wl => new
            {
                WorkerProjectLinkId = wl.Id,
                wl.WorkerId,
                WorkerFullName = wl.Worker != null ? $"{wl.Worker.FirstName} {wl.Worker.LastName}" : null,
                wl.Worker?.CivilId,
                wl.Worker?.PhoneNumber,
                wl.IsWorkerActive,
                wl.DailyWageAmount,
                wl.ServiceRequestId,
                ServiceNameEn = wl.ServiceRequest?.Service?.NameEn,
                ServicePrice = wl.ServiceRequest?.Service?.Price,
                wl.CreatedDate,
            });

            return Ok(new ApiResponse<object>(true, "تم جلب عمال المشروع بنجاح.", "Project workers fetched successfully.", result));
        }
        catch (Exception ex)
        {
            _logger.LogError(
        ex,
        "Error fetching project details for ProjectId: {ProjectId}",
        projectId);

            return HandleException(ex);
        }
    }
}
