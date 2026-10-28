using Maydan.Domain.Entities;
using Maydan.Domain.Enums;

namespace Maydan.Application.Interfaces;

public interface IWorkerAttendanceRepository
{
    Task<WorkerAttendance?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<List<WorkerAttendance>> ListByLinkAndDateAsync(int workerProjectLinkId, DateTime date, CancellationToken cancellationToken = default);
    Task<List<WorkerAttendance>> ListByServiceRequestAsync(int serviceRequestId, DateTime from, DateTime to, CancellationToken cancellationToken = default);
    //Active link of the worker that owns this QR code on this service request (null if not assigned).
    Task<WorkerProjectLink?> GetActiveLinkByQrAsync(string qrCode, int serviceRequestId, CancellationToken cancellationToken = default);
    Task<List<WorkerProjectLink>> ListLinksByServiceRequestAsync(int serviceRequestId, CancellationToken cancellationToken = default);
    Task AddAsync(WorkerAttendance attendance, CancellationToken cancellationToken = default);

    Task<List<AttendanceTimeWindow>> ListWindowsForDateAsync(int serviceRequestId, DateTime date, CancellationToken cancellationToken = default);
    Task<List<AttendanceTimeWindow>> ListWindowsAsync(int serviceRequestId, DateTime from, DateTime to, CancellationToken cancellationToken = default);
    Task AddWindowAsync(AttendanceTimeWindow window, CancellationToken cancellationToken = default);

    //Overtime
    Task<WorkerOvertime?> GetOvertimeByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<WorkerOvertime?> GetOvertimeAsync(int workerProjectLinkId, DateTime date, CancellationToken cancellationToken = default);
    Task<List<WorkerOvertime>> ListOvertimeByServiceRequestAsync(int serviceRequestId, DateTime from, DateTime to, CancellationToken cancellationToken = default);
    Task<List<WorkerOvertime>> ListOvertimeByStatusAsync(AttendanceApprovalStatus status, CancellationToken cancellationToken = default);
    Task AddOvertimeAsync(WorkerOvertime overtime, CancellationToken cancellationToken = default);
}
