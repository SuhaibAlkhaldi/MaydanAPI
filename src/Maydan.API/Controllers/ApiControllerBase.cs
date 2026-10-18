using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Maydan.Application.DTOs.Common;

// Backward-compatible helpers for new
//
//
//
//
//
// <T> envelope.

namespace Maydan.API.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected bool TryGetCurrentUserId(out int currentUserId)
    {
        var claimValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        if (int.TryParse(claimValue, out currentUserId))
        {
            return true;
        }

        if (Request.Headers.TryGetValue("X-Current-User-Id", out var values) &&
            int.TryParse(values.FirstOrDefault(), out currentUserId))
        {
            return true;
        }

        currentUserId = 0;
        return false;
    }

    protected ActionResult HandleException(Exception exception) =>
        exception switch
        {
            Maydan.Application.Exceptions.BilingualNotFoundException bnf =>
                NotFound(new Maydan.Application.DTOs.Common.ApiResponse<object?>(false, bnf.MessageAr, bnf.MessageEn, null)),
            Maydan.Application.Exceptions.BilingualException be =>
                BadRequest(new Maydan.Application.DTOs.Common.ApiResponse<object?>(false, be.MessageAr, be.MessageEn, null)),
            UnauthorizedAccessException =>
                new ObjectResult(new Maydan.Application.DTOs.Common.ApiResponse<object?>(false, "غير مصرح", "Forbidden", null)) { StatusCode = StatusCodes.Status403Forbidden },
            KeyNotFoundException => NotFound(new { message = exception.Message }),
            InvalidOperationException => BadRequest(new { message = exception.Message }),
            ArgumentException => BadRequest(new { message = exception.Message }),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new { message = "Unexpected server error." })
        };

    // New helper to produce an ApiResponse<T> for successes.
    protected ActionResult Success<T>(T? data, string messageAr = "", string messageEn = "") where T : class
    {
        var payload = new ApiResponse<T>(true, messageAr, messageEn, data);
        return Ok(payload);
    }

    protected ActionResult CreatedResponse<T>(string location, T? data, string messageAr = "", string messageEn = "") where T : class
    {
        var payload = new ApiResponse<T>(true, messageAr, messageEn, data);
        return Created(location, payload);
    }

    protected ActionResult NoContentResponse<T>(string messageAr = "", string messageEn = "") where T : class
    {
        var payload = new ApiResponse<T>(true, messageAr, messageEn, default);
        return new ObjectResult(payload) { StatusCode = StatusCodes.Status204NoContent };
    }
}
