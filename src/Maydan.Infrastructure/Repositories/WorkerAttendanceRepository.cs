using Maydan.Application.Interfaces;
using Maydan.Domain.Entities;
using Maydan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Maydan.Infrastructure.Repositories;

public class WorkerAttendanceRepository : IWorkerAttendanceRepository
{
    private readonly MaydanDbContext _db;
    public WorkerAttendanceRepository(MaydanDbContext db) => _db = db;

    public Task<WorkerAttendance?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _db.WorkerAttendances
           .Include(a => a.WorkerProjectLink)
           .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public Task<List<WorkerAttendance>> ListByLinkAndDateAsync(int workerProjectLinkId, DateTime date, CancellationToken cancellationToken = default) =>
        _db.WorkerAttendances
           .Where(a => a.WorkerProjectLinkId == workerProjectLinkId && a.AttendanceForDate == date)
           .OrderBy(a => a.CheckInNumber)
           .ToListAsync(cancellationToken);

    public Task<List<WorkerAttendance>> ListByServiceRequestAsync(int serviceRequestId, DateTime from, DateTime to, CancellationToken cancellationToken = default) =>
        _db.WorkerAttendances
           .Where(a => a.WorkerProjectLink.ServiceRequestId == serviceRequestId
                    && a.AttendanceForDate >= from && a.AttendanceForDate <= to)
           .OrderBy(a => a.AttendanceForDate).ThenBy(a => a.CheckInNumber)
           .ToListAsync(cancellationToken);

    public Task<WorkerProjectLink?> GetActiveLinkByQrAsync(string qrCode, int serviceRequestId, CancellationToken cancellationToken = default) =>
        _db.WorkerProjectLinks
           .Include(l => l.Worker)
           .FirstOrDefaultAsync(l => l.ServiceRequestId == serviceRequestId
                                  && l.IsWorkerActive
                                  && l.Worker.QrCode == qrCode, cancellationToken);

    public Task<List<WorkerProjectLink>> ListLinksByServiceRequestAsync(int serviceRequestId, CancellationToken cancellationToken = default) =>
        _db.WorkerProjectLinks
           .Where(l => l.ServiceRequestId == serviceRequestId && l.IsWorkerActive)
           .Include(l => l.Worker)
           .ToListAsync(cancellationToken);

    public async Task AddAsync(WorkerAttendance attendance, CancellationToken cancellationToken = default) =>
        await _db.WorkerAttendances.AddAsync(attendance, cancellationToken);

    public Task<AttendanceTimeWindow?> GetWindowAsync(int serviceRequestId, DateTime date, CancellationToken cancellationToken = default) =>
        _db.AttendanceTimeWindows
           .FirstOrDefaultAsync(w => w.ServiceRequestId == serviceRequestId && w.AttendanceDate == date, cancellationToken);

    public Task<List<AttendanceTimeWindow>> ListWindowsAsync(int serviceRequestId, DateTime from, DateTime to, CancellationToken cancellationToken = default) =>
        _db.AttendanceTimeWindows
           .Where(w => w.ServiceRequestId == serviceRequestId && w.AttendanceDate >= from && w.AttendanceDate <= to)
           .OrderBy(w => w.AttendanceDate)
           .ToListAsync(cancellationToken);

    public async Task AddWindowAsync(AttendanceTimeWindow window, CancellationToken cancellationToken = default) =>
        await _db.AttendanceTimeWindows.AddAsync(window, cancellationToken);
}
