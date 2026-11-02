using Maydan.Application.Interfaces;
using Maydan.Domain.Common;
using Maydan.Domain.Entities;
using Maydan.Domain.Enums;
using Maydan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;

namespace Maydan.Infrastructure.Repositories;

public class WorkerAttendanceRepository : IWorkerAttendanceRepository
{
    private readonly MaydanDbContext _db;
    public WorkerAttendanceRepository(MaydanDbContext db) => _db = db;

    public Task<WorkerAttendance?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _db.WorkerAttendances
           .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    //NEW 
    public Task<WorkerAttendance?> GetForReviewAsync(int id, CancellationToken cancellationToken = default) =>
       _db.WorkerAttendances
          .Include(a => a.WorkerProjectLink)
          .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);



    //    .Include(a => a.WorkerProjectLink)
    //    .Include(a => a.EntryType)
    //.Include(a => a.ApprovalStatus)

    public Task<List<WorkerAttendance>> ListByLinkAndDateAsync(int workerProjectLinkId, DateTime date, CancellationToken cancellationToken = default) =>
         _db.WorkerAttendances
            .Where(a => a.WorkerProjectLinkId == workerProjectLinkId && a.AttendanceForDate == date)
            .OrderBy(a => a.CheckInNumber)
            .ToListAsync(cancellationToken);


    public Task<List<WorkerAttendance>> ListByServiceRequestAsync(int serviceRequestId, DateTime from, DateTime to, CancellationToken cancellationToken = default) =>
         _db.WorkerAttendances.AsNoTracking()
            .Where(a => a.WorkerProjectLink.ServiceRequestId == serviceRequestId
                     && a.AttendanceForDate >= from && a.AttendanceForDate <= to)
            .OrderBy(a => a.AttendanceForDate).ThenBy(a => a.CheckInNumber)
            .ToListAsync(cancellationToken);


    public Task<WorkerProjectLink?> GetActiveLinkByQrAsync(string qrCode, int serviceRequestId, CancellationToken cancellationToken = default) =>
        _db.WorkerProjectLinks.AsNoTracking()
           .Include(l => l.Worker)
           .FirstOrDefaultAsync(l => l.ServiceRequestId == serviceRequestId
                                  && l.IsWorkerActive
                                  && l.Worker.QrCode == qrCode, cancellationToken);

    public Task<List<WorkerProjectLink>> ListLinksByServiceRequestAsync(int serviceRequestId, CancellationToken cancellationToken = default) =>
        _db.WorkerProjectLinks.AsNoTracking()
           .Where(l => l.ServiceRequestId == serviceRequestId)
           .Include(l => l.Worker)
           .ToListAsync(cancellationToken);

    public async Task AddAsync(WorkerAttendance attendance, CancellationToken cancellationToken = default) =>
        await _db.WorkerAttendances.AddAsync(attendance, cancellationToken);

    public Task<List<AttendanceTimeWindow>> ListWindowsForDateAsync(int serviceRequestId, DateTime date, CancellationToken cancellationToken = default) =>
        _db.AttendanceTimeWindows.AsNoTracking()
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
    public Task<WorkerOvertime?> GetOvertimeByIdAsync(int id, int? productionCompanyId = null, CancellationToken cancellationToken = default) =>
         _db.WorkerOvertimes
            .Include(o => o.WorkerProjectLink).ThenInclude(l => l.Worker)
            .Where(o => productionCompanyId == null
                     || o.WorkerProjectLink.ServiceRequest.ProductionCompanyId == productionCompanyId)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public Task<WorkerOvertime?> GetOvertimeAsync(int workerProjectLinkId, DateTime date, CancellationToken cancellationToken = default) =>
        _db.WorkerOvertimes
           .FirstOrDefaultAsync(o => o.WorkerProjectLinkId == workerProjectLinkId && o.OvertimeForDate == date, cancellationToken);


    public Task<List<WorkerOvertime>> ListOvertimeByServiceRequestAsync(int serviceRequestId, DateTime from, DateTime to, CancellationToken cancellationToken = default) =>
        _db.WorkerOvertimes.AsNoTracking()
           .Where(o => o.WorkerProjectLink.ServiceRequestId == serviceRequestId
                    && o.OvertimeForDate >= from && o.OvertimeForDate <= to)
           .Include(o => o.WorkerProjectLink).ThenInclude(l => l.Worker)
           .OrderBy(o => o.OvertimeForDate)
           .ToListAsync(cancellationToken);

    public Task<List<WorkerOvertime>> ListOvertimeByStatusAsync(int statusId, int? productionCompanyId = null, CancellationToken cancellationToken = default) =>
        _db.WorkerOvertimes.AsNoTracking()
           .Where(o => o.ApprovalStatusId == statusId
                    && (productionCompanyId == null
                        || o.WorkerProjectLink.ServiceRequest.ProductionCompanyId == productionCompanyId))
           .Include(o => o.WorkerProjectLink).ThenInclude(l => l.Worker)
           .OrderBy(o => o.OvertimeForDate)
           .ToListAsync(cancellationToken);
    //public Task<List<WorkerOvertime>> ListOvertimeByStatusAsync(int statusId, CancellationToken cancellationToken = default) =>
    //_context.WorkerOvertimes
    //    .Include(o => o.WorkerProjectLink).ThenInclude(l => l.Worker)
    //    .Where(o => o.ApprovalStatusId == statusId)
    //    .ToListAsync(cancellationToken);
    public async Task AddOvertimeAsync(WorkerOvertime overtime, CancellationToken cancellationToken = default) =>
        await _db.WorkerOvertimes.AddAsync(overtime, cancellationToken);


    //--------------------------------------------------------------------- 

    public Task<List<WorkerAttendance>> ListPendingResolutionAsync(CancellationToken cancellationToken = default) =>
    _db.WorkerAttendances.AsNoTracking()
       .Include(a => a.WorkerProjectLink).ThenInclude(l => l.Worker)
       .Where(a => a.EntryTypeId == LookupConstants.AttendanceEntryType.NotRecorded
                && a.ApprovalStatusId == LookupConstants.AttendanceApprovalStatus.Pending)
       .OrderBy(a => a.AttendanceForDate).ThenBy(a => a.CheckInNumber)
       .ToListAsync(cancellationToken);

    // tracked عن قصد: الجوب بيعدّل عليهم
    public Task<List<AttendanceTimeWindow>> ListUnprocessedClosedWindowsAsync(DateTime nowUtc, CancellationToken cancellationToken = default) =>
        _db.AttendanceTimeWindows
           .Where(w => w.ClosesAtUtc <= nowUtc
                    && w.UnrecordedProcessedDate == null
                    && w.ServiceRequest.StatusId != LookupConstants.ServiceRequestStatus.Cancelled
                    && w.ServiceRequest.StatusId != LookupConstants.ServiceRequestStatus.Rejected)
           .OrderBy(w => w.AttendanceDate).ThenBy(w => w.SlotNumber)
           .ToListAsync(cancellationToken);

    public Task<List<WorkerAttendance>> ListBySlotAsync(int serviceRequestId, DateTime date, int slotNumber, CancellationToken cancellationToken = default) =>
        _db.WorkerAttendances
           .Where(a => a.WorkerProjectLink.ServiceRequestId == serviceRequestId
                    && a.AttendanceForDate == date && a.CheckInNumber == slotNumber)
           .ToListAsync(cancellationToken);

    public Task<List<WorkerAttendance>> ListRemindableAsync(DateTime reminderBeforeUtc, CancellationToken cancellationToken = default) =>
        _db.WorkerAttendances
           .Where(a => a.EntryTypeId == LookupConstants.AttendanceEntryType.NotRecorded
                    && a.ApprovalStatusId == LookupConstants.AttendanceApprovalStatus.Pending
                    && (a.LastReminderDate == null || a.LastReminderDate < reminderBeforeUtc))
           .ToListAsync(cancellationToken);

    // للـ guard تبع الأجور: في أي سجل معلق، أو فتحة انتهت ولسا الجوب ما عالجها
    public async Task<bool> HasUnresolvedAsync(int serviceRequestId, DateTime nowUtc, CancellationToken cancellationToken = default) =>
        await _db.WorkerAttendances.AnyAsync(a => a.WorkerProjectLink.ServiceRequestId == serviceRequestId
                && a.ApprovalStatusId == LookupConstants.AttendanceApprovalStatus.Pending, cancellationToken)
        || await _db.AttendanceTimeWindows.AnyAsync(w => w.ServiceRequestId == serviceRequestId
                && w.ClosesAtUtc <= nowUtc && w.UnrecordedProcessedDate == null, cancellationToken);


    public Task<WorkerProjectLink?> GetActiveLinkByIdAsync(int linkId, int serviceRequestId, CancellationToken cancellationToken = default) =>
    _db.WorkerProjectLinks.AsNoTracking()
       .Include(l => l.Worker)
       .FirstOrDefaultAsync(l => l.Id == linkId
                             && l.ServiceRequestId == serviceRequestId
                             && l.IsWorkerActive, cancellationToken);

}
