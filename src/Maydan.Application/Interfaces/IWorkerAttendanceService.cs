using Maydan.Application.DTOs.Attendance;
using Maydan.Application.DTOs.Common;

namespace Maydan.Application.Interfaces;

public interface IWorkerAttendanceService
{
    Task<ApiResponse<List<AttendanceTimeWindowDto>>> SetTimeWindowsAsync(int currentUserId, int serviceRequestId, SetAttendanceWindowDto dto, CancellationToken cancellationToken = default);
    Task<ApiResponse<List<AttendanceTimeWindowDto>>> GetTimeWindowsAsync(int currentUserId, int serviceRequestId, DateTime? from, DateTime? to, CancellationToken cancellationToken = default);

    Task<ApiResponse<WorkerAttendanceDto>> CheckInAsync(int currentUserId, RecordAttendanceDto dto, CancellationToken cancellationToken = default);
    Task<ApiResponse<WorkerAttendanceDto>> CheckOutAsync(int currentUserId, int attendanceId, CancellationToken cancellationToken = default);
    Task<ApiResponse<WorkerAttendanceDto>> ApproveAsync(int currentUserId, int attendanceId, CancellationToken cancellationToken = default);
    Task<ApiResponse<WorkerAttendanceDto>> RejectAsync(int currentUserId, int attendanceId, string reason, CancellationToken cancellationToken = default);

    Task<ApiResponse<DayRosterDto>> GetDayRosterAsync(int currentUserId, int serviceRequestId, DateTime? date, CancellationToken cancellationToken = default);
    Task<ApiResponse<AttendanceSummaryDto>> GetSummaryAsync(int currentUserId, int serviceRequestId, DateTime from, DateTime to, CancellationToken cancellationToken = default);
}
