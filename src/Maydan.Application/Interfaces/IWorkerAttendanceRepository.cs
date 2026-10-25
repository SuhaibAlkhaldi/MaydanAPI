using Maydan.Domain.Entities;

namespace Maydan.Application.Interfaces;

public interface IWorkerAttendanceRepository
{
    Task<WorkerAttendance?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<List<WorkerAttendance>> ListByLinkAndDateAsync(int workerProjectLinkId, DateTime date, CancellationToken cancellationToken = default);
    Task<List<WorkerAttendance>> ListByServiceRequestAsync(int serviceRequestId, DateTime from, DateTime to, CancellationToken cancellationToken = default);
    Task<List<WorkerProjectLink>> ListLinksByServiceRequestAsync(int serviceRequestId, CancellationToken cancellationToken = default);
    Task AddAsync(WorkerAttendance attendance, CancellationToken cancellationToken = default);

    Task<AttendanceTimeWindow?> GetWindowAsync(int serviceRequestId, DateTime date, CancellationToken cancellationToken = default);
    Task<List<AttendanceTimeWindow>> ListWindowsAsync(int serviceRequestId, DateTime from, DateTime to, CancellationToken cancellationToken = default);
    Task AddWindowAsync(AttendanceTimeWindow window, CancellationToken cancellationToken = default);
}
