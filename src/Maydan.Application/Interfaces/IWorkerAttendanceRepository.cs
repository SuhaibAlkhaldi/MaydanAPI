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
    //Task<WorkerOvertime?> GetOvertimeByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<WorkerOvertime?> GetOvertimeAsync(int workerProjectLinkId, DateTime date, CancellationToken cancellationToken = default);
    Task<List<WorkerOvertime>> ListOvertimeByServiceRequestAsync(int serviceRequestId, DateTime from, DateTime to, CancellationToken cancellationToken = default);
    //Task<List<WorkerOvertime>> ListOvertimeByStatusAsync(AttendanceApprovalStatus status, CancellationToken cancellationToken = default);
    //Task<List<WorkerOvertime>> ListOvertimeByStatusAsync(int statusId, CancellationToken cancellationToken = default);
    Task AddOvertimeAsync(WorkerOvertime overtime, CancellationToken cancellationToken = default);
    Task<WorkerAttendance?> GetForReviewAsync(int id, CancellationToken cancellationToken = default);
    Task<WorkerOvertime?> GetOvertimeByIdAsync(int id, int? productionCompanyId = null, CancellationToken cancellationToken = default);
   Task<List<WorkerOvertime>> ListOvertimeByStatusAsync(int statusId, int? productionCompanyId = null, CancellationToken cancellationToken = default);
    //-----------------------------------------------------------------------------

    Task<List<WorkerAttendance>> ListPendingResolutionAsync(CancellationToken cancellationToken = default);
    Task<List<AttendanceTimeWindow>> ListUnprocessedClosedWindowsAsync(DateTime nowUtc, CancellationToken cancellationToken = default);
    Task<List<WorkerAttendance>> ListBySlotAsync(int serviceRequestId, DateTime date, int slotNumber, CancellationToken cancellationToken = default);
    Task<List<WorkerAttendance>> ListRemindableAsync(DateTime reminderBeforeUtc, CancellationToken cancellationToken = default);
    Task<bool> HasUnresolvedAsync(int serviceRequestId, DateTime nowUtc, CancellationToken cancellationToken = default);

    Task<WorkerProjectLink?> GetActiveLinkByIdAsync(int linkId, int serviceRequestId, CancellationToken cancellationToken = default);
    //Pendings for Production Company
    Task<List<WorkerAttendance>> ListPendingApprovalAsync(int productionCompanyId, int? serviceRequestId,
    DateTime? from, DateTime? to, CancellationToken cancellationToken = default);



}
