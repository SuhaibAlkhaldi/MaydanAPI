using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FluentValidation;
using FluentValidation.Results;
using Maydan.Application.DTOs.ServiceRequests;
using Maydan.Application.Interfaces;
using Maydan.Application.Validators;
using Maydan.Application.DTOs.Common;

namespace Maydan.API.Controllers;

[Authorize]
[Route("api/[controller]")]
public class ServiceRequestsController : ApiControllerBase
{
    private readonly IServiceRequestService _serviceRequestService;
    private readonly CreateServiceRequestDtoValidator _createValidator;

    public ServiceRequestsController(IServiceRequestService serviceRequestService, CreateServiceRequestDtoValidator createValidator)
    {
        _serviceRequestService = serviceRequestService;
        _createValidator = createValidator;
    }

    [HttpGet("resolve-association")]
    public async Task<ActionResult<AssociationLookupDto>> ResolveAssociation([FromQuery] int cityId, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(new Maydan.Application.DTOs.Common.ApiResponse<object?>(false, " تعذر التحقق من هوية المستخدم  ", "Current user id is required.", null));
        }

        try
        {
            var result = await _serviceRequestService.ResolveAssociationByCityIdAsync(currentUserId, cityId, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPost]
    public async Task<ActionResult<ServiceRequestDto>> Create([FromBody] CreateServiceRequestDto dto, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(new Maydan.Application.DTOs.Common.ApiResponse<object?>(false, "  تعذر التحقق من هوية المستخدم ", "Current user id is required.", null));
        }
        // validate input using the concrete validator
        var validation = await _createValidator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = validation.Errors.Select(e => new { field = e.PropertyName, error = e.ErrorMessage }).ToList();
            return BadRequest(new Maydan.Application.DTOs.Common.ApiResponse<object?>(false, "البيانات المدخلة غير صحيحة او غير مطابقة", "Validation failed.", errors));
        }

        try
        {
            var result = await _serviceRequestService.CreateAsync(currentUserId, dto, cancellationToken);
            return CreatedResponse($"/api/ServiceRequests/{result.Id}", result);
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPost("calculate-expected-payment")]
    public async Task<ActionResult<ExpectedPaymentCalculationDto>> Calculate([FromBody] CalculationRequestDto dto, CancellationToken cancellationToken)
    {
        try
        {
            if (!TryGetCurrentUserId(out var currentUserId))
            {
                return Unauthorized(new Maydan.Application.DTOs.Common.ApiResponse<object?>(false, "  تعذر التحقق من هوية المستخدم ", "Current user id is required.", null));
            }

            var calculationResult = await _serviceRequestService.CalculateExpectedPaymentAsync(currentUserId, dto.ServiceId, dto.RequestedWorkers, dto.DurationCount, dto.TimeUnit, cancellationToken);
            return Success(calculationResult);
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpGet]
    public async Task<ActionResult<List<ServiceRequestDto>>> GetAll(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(new Maydan.Application.DTOs.Common.ApiResponse<object?>(false, "تعذر التحقق من هوية المستخدم", "Current user id is required.", null));
        }

        try
        {
            return Success(await _serviceRequestService.GetAllAsync(currentUserId, cancellationToken));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ServiceRequestDetailsDto>> GetById(int id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(new Maydan.Application.DTOs.Common.ApiResponse<object?>(false, "تعذر التحقق من هوية المستخدم", "Current user id is required.", null));
        }

        try
        {
            return Success(await _serviceRequestService.GetByIdAsync(currentUserId, id, cancellationToken));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<ActionResult> Cancel(int id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(new Maydan.Application.DTOs.Common.ApiResponse<object?>(false, "تعذر التحقق من هوية المستخدم", "Current user id is required.", null));

        }

        try
        {
            await _serviceRequestService.CancelAsync(currentUserId, id, cancellationToken);
            return NoContentResponse<ServiceRequestDto>();
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }
}
