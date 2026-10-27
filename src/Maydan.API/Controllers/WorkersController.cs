using Maydan.API.Models.Workers;
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
    private readonly IFileStorageService _fileStorageService;

    private const string WorkerDocumentsSubfolder = "worker-documents";

    public WorkersController(IWorkerService workerService, IFileStorageService fileStorageService)
    {
        _workerService = workerService;
        _fileStorageService = fileStorageService;
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
    public async Task<ActionResult> Create([FromForm] CreateWorkerRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(new ApiResponse<object?>(false, "المستخدم غير مصرح له.", "Unauthorized.", null, 401));
        }

        try
        {
            var dto = new CreateWorkerDto
            {
                AssociationId = request.AssociationId,
                FirstName = request.FirstName,
                MiddleName = request.MiddleName,
                LastName = request.LastName,
                CivilId = request.CivilId,
                DateOfBirth = request.DateOfBirth,
                Gender = request.Gender,
                MaritalStatus = request.MaritalStatus,
                Nationality = request.Nationality,
                PhoneNumber = request.PhoneNumber,
                ServiceIds = request.ServiceIds,
                YearsOfExperience = request.YearsOfExperience,
                DocumentPaths = await SaveAllAsync(request.Documents, cancellationToken)
            };

            var response = await _workerService.CreateAsync(currentUserId, dto, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> Update(int id, [FromForm] UpdateWorkerRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(new ApiResponse<object?>(false, "المستخدم غير مصرح له.", "Unauthorized.", null, 401));
        }

        try
        {
            var dto = new UpdateWorkerDto
            {
                FirstName = request.FirstName,
                MiddleName = request.MiddleName,
                LastName = request.LastName,
                DateOfBirth = request.DateOfBirth,
                Gender = request.Gender,
                MaritalStatus = request.MaritalStatus,
                Nationality = request.Nationality,
                PhoneNumber = request.PhoneNumber,
                ServiceIds = request.ServiceIds,
                YearsOfExperience = request.YearsOfExperience,
                DocumentPaths = await SaveAllAsync(request.Documents, cancellationToken)
            };

            var response = await _workerService.UpdateAsync(currentUserId, id, dto, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    

   

    private async Task<List<string>> SaveAllAsync(List<IFormFile> files, CancellationToken cancellationToken)
    {
        var paths = new List<string>();

        foreach (var file in files)
        {
            await using var stream = file.OpenReadStream();
            paths.Add(await _fileStorageService.SaveAsync(stream, file.FileName, WorkerDocumentsSubfolder, cancellationToken));
        }

        return paths;
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

    [HttpGet("service-request/{serviceRequestId}/available-workers")]
    public async Task<IActionResult> GetAvailableWorkersForRequest(
     int serviceRequestId, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(new Maydan.Application.DTOs.Common.ApiResponse<object?>(false, "تعذر التحقق من هوية المستخدم", "Current user id is required.", null));
        }

        var response = await _workerService.GetAvailableWorkersForRequestAsync(
            currentUserId,
            serviceRequestId,
           
            cancellationToken);

        return StatusCode(response.StatusCode, response);
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


    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(
        int id,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(
                new ApiResponse<object?>(
                    false,
                    "المستخدم غير مصرح له.",
                    "Unauthorized.",
                    null,
                    401));
        }

        try
        {
            var response = await _workerService.DeleteAsync(
                currentUserId,
                id,
                cancellationToken);

            return StatusCode(response.StatusCode, response);
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }


}


