using Maydan.Application.Interfaces;
using Maydan.Domain.Entities;
using Maydan.Domain.Enums;
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
           .Where(l => l.ServiceRequestId == serviceRequestId)
           .Include(l => l.Worker)
           .ToListAsync(cancellationToken);

    public async Task AddAsync(WorkerAttendance attendance, CancellationToken cancellationToken = default) =>
        await _db.WorkerAttendances.AddAsync(attendance, cancellationToken);

    public Task<List<AttendanceTimeWindow>> ListWindowsForDateAsync(int serviceRequestId, DateTime date, CancellationToken cancellationToken = default) =>
        _db.AttendanceTimeWindows
           .Where(w => w.ServiceRequestId == serviceRequestId && w.AttendanceDate == date)
           .OrderBy(w => w.SlotNumber)
           .ToListAsync(cancellationToken);

    public Task<List<AttendanceTimeWindow>> ListWindowsAsync(int serviceRequestId, DateTime from, DateTime to, CancellationToken cancellationToken = default) =>
        _db.AttendanceTimeWindows
           .Where(w => w.ServiceRequestId == serviceRequestId && w.AttendanceDate >= from && w.AttendanceDate <= to)
           .OrderBy(w => w.AttendanceDate).ThenBy(w => w.SlotNumber)
           .ToListAsync(cancellationToken);

    public async Task AddWindowAsync(AttendanceTimeWindow window, CancellationToken cancellationToken = default) =>
        await _db.AttendanceTimeWindows.AddAsync(window, cancellationToken);

    // Overtime 
    public Task<WorkerOvertime?> GetOvertimeByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _db.WorkerOvertimes
           .Include(o => o.WorkerProjectLink).ThenInclude(l => l.Worker)
           .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public Task<WorkerOvertime?> GetOvertimeAsync(int workerProjectLinkId, DateTime date, CancellationToken cancellationToken = default) =>
        _db.WorkerOvertimes
           .FirstOrDefaultAsync(o => o.WorkerProjectLinkId == workerProjectLinkId && o.OvertimeForDate == date, cancellationToken);

    public Task<List<WorkerOvertime>> ListOvertimeByServiceRequestAsync(int serviceRequestId, DateTime from, DateTime to, CancellationToken cancellationToken = default) =>
        _db.WorkerOvertimes
           .Where(o => o.WorkerProjectLink.ServiceRequestId == serviceRequestId
                    && o.OvertimeForDate >= from && o.OvertimeForDate <= to)
           .Include(o => o.WorkerProjectLink).ThenInclude(l => l.Worker)
           .OrderBy(o => o.OvertimeForDate)
           .ToListAsync(cancellationToken);

    public Task<List<WorkerOvertime>> ListOvertimeByStatusAsync(AttendanceApprovalStatus status, CancellationToken cancellationToken = default) =>
        _db.WorkerOvertimes
           .Where(o => o.ApprovalStatus == status)
           .Include(o => o.WorkerProjectLink).ThenInclude(l => l.Worker)
           .OrderBy(o => o.OvertimeForDate)
           .ToListAsync(cancellationToken);

    public async Task AddOvertimeAsync(WorkerOvertime overtime, CancellationToken cancellationToken = default) =>
        await _db.WorkerOvertimes.AddAsync(overtime, cancellationToken);
}
