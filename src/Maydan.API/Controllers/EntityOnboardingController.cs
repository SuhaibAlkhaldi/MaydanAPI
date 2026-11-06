using Maydan.Application.DTOs.Common;
using Maydan.Application.DTOs.Onboarding;
using Maydan.Application.DTOs.UserManagement;
using Maydan.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maydan.API.Controllers;

[Authorize]
[Route("api/[controller]")]
public class EntityOnboardingController : ApiControllerBase
{
    private readonly IEntityOnboardingService _entityOnboardingService;

    public EntityOnboardingController(IEntityOnboardingService entityOnboardingService)
    {
        _entityOnboardingService = entityOnboardingService;
    }

    [HttpGet("associations-without-admin")]
    public async Task<ActionResult> GetAssociationsWithoutAdmin(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(ApiResponse<List<AssociationWithoutAdminDto>>.FailureResponse("Current user id is required.", "مطلوب معرف المستخدم الحالي."));
        }

        try
        {
            var response = await _entityOnboardingService.GetAssociationsWithoutAdminAsync(currentUserId, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    [HttpPost("associations/{associationId:int}/admin")]
    public async Task<ActionResult> OnboardAssociationAdmin(int associationId, [FromBody] OnboardAssociationAdminDto dto, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(ApiResponse<UserDetailsDto>.FailureResponse("Current user id is required.", "مطلوب معرف المستخدم الحالي."));
        }

        try
        {
            var response = await _entityOnboardingService.OnboardAssociationAdminAsync(currentUserId, associationId, dto, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }
}