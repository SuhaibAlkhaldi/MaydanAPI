using Maydan.Application.DTOs.Attendance;
using Maydan.Application.DTOs.Common;
using Maydan.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maydan.API.Controllers;

[Authorize]
[Route("api/attendance")]
public class AttendanceController : ApiControllerBase
{
    private readonly IWorkerAttendanceService _service;
    public AttendanceController(IWorkerAttendanceService service) => _service = service;

    private ActionResult NoUser() =>
        Unauthorized(new ApiResponse<object?>(false, "تعذر التحقق من هوية المستخدم", "Current user id is required.", null, 401));

    // Time windows (Production House) 
    [HttpPut("service-requests/{serviceRequestId:int}/time-windows")]
    public async Task<IActionResult> SetTimeWindows(int serviceRequestId, [FromBody] SetAttendanceWindowDto dto, CancellationToken ct)
    {
        if (!TryGetCurrentUserId(out var uid)) return NoUser();
        var r = await _service.SetTimeWindowsAsync(uid, serviceRequestId, dto, ct);
        return StatusCode(r.StatusCode, r);
    }

    [HttpGet("service-requests/{serviceRequestId:int}/time-windows")]
    public async Task<IActionResult> GetTimeWindows(int serviceRequestId, [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
    {
        if (!TryGetCurrentUserId(out var uid)) return NoUser();
        var r = await _service.GetTimeWindowsAsync(uid, serviceRequestId, from, to, ct);
        return StatusCode(r.StatusCode, r);
    }

    // Recording
    [HttpPost("check-in")]
    public async Task<IActionResult> CheckIn([FromBody] RecordAttendanceDto dto, CancellationToken ct)
    {
        if (!TryGetCurrentUserId(out var uid)) return NoUser();
        var r = await _service.CheckInAsync(uid, dto, ct);
        return StatusCode(r.StatusCode, r);
    }

    [HttpPost("{attendanceId:int}/check-out")]
    public async Task<IActionResult> CheckOut(int attendanceId, CancellationToken ct)
    {
        if (!TryGetCurrentUserId(out var uid)) return NoUser();
        var r = await _service.CheckOutAsync(uid, attendanceId, ct);
        return StatusCode(r.StatusCode, r);
    }

    // Approval (one record at a time)
    [HttpPost("{attendanceId:int}/approve")]
    public async Task<IActionResult> Approve(int attendanceId, CancellationToken ct)
    {
        if (!TryGetCurrentUserId(out var uid)) return NoUser();
        var r = await _service.ApproveAsync(uid, attendanceId, ct);
        return StatusCode(r.StatusCode, r);
    }

    [HttpPost("{attendanceId:int}/reject")]
    public async Task<IActionResult> Reject(int attendanceId, [FromBody] RejectAttendanceDto dto, CancellationToken ct)
    {
        if (!TryGetCurrentUserId(out var uid)) return NoUser();
        var r = await _service.RejectAsync(uid, attendanceId, dto.Reason, ct);
        return StatusCode(r.StatusCode, r);
    }

    // Reads 
    [HttpGet("service-requests/{serviceRequestId:int}/day")]
    public async Task<IActionResult> GetDay(int serviceRequestId, [FromQuery] DateTime? date, CancellationToken ct)
    {
        if (!TryGetCurrentUserId(out var uid)) return NoUser();
        var r = await _service.GetDayRosterAsync(uid, serviceRequestId, date, ct);
        return StatusCode(r.StatusCode, r);
    }

    [HttpGet("service-requests/{serviceRequestId:int}/summary")]
    public async Task<IActionResult> GetSummary(int serviceRequestId, [FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct)
    {
        if (!TryGetCurrentUserId(out var uid)) return NoUser();
        var r = await _service.GetSummaryAsync(uid, serviceRequestId, from, to, ct);
        return StatusCode(r.StatusCode, r);
    }
}
