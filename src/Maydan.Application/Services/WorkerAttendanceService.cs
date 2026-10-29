using Maydan.Application.DTOs.Attendance;
using Maydan.Application.DTOs.Common;
using Maydan.Application.Interfaces;
using Maydan.Domain.Common;
using Maydan.Domain.Entities;
using Maydan.Domain.Enums;

namespace Maydan.Application.Services;

public partial class WorkerAttendanceService : IWorkerAttendanceService
{
    private const string ViewPermission = "View Attendance";
    private const string ManagePermission = "Manage Attendance";
    private const int MaxRangeDays = 366;

    // Jordan is permanently UTC+3 (no daylight saving since 2022).
    // Windows are entered in local time and stored in UTC.
    private static readonly TimeSpan LocalOffset = TimeSpan.FromHours(3);

    private readonly IUnitOfWork _uow;
    private readonly IWorkerAttendanceRepository _repo;

    public WorkerAttendanceService(IUnitOfWork uow, IWorkerAttendanceRepository repo)
    {
        _uow = uow;
        _repo = repo;
    }

    private enum Side { None, Association, ProductionCompany }

    private sealed record Gate(Side Side, string? En = null, string? Ar = null, int Status = 400, AttendanceTimeWindow? Slot = null);

    
    //  1) Time windows (Production House)
    
    public async Task<ApiResponse<List<AttendanceTimeWindowDto>>> SetTimeWindowsAsync(
        int currentUserId, int serviceRequestId, SetAttendanceWindowDto dto, CancellationToken ct = default)
    {
        var user = await GetActiveUserAsync(currentUserId, ct);
        if (user is null) return Unauthorized<List<AttendanceTimeWindowDto>>();

        var sr = await _uow.ServiceRequests.GetByIdAsync(serviceRequestId, ct);
        if (sr is null) return Fail<List<AttendanceTimeWindowDto>>("Service request was not found.", "طلب الخدمة غير موجود.", 404);

        if (!Permissions(user).Contains(ManagePermission) || !IsProductionHouseOf(user, sr))
            return Fail<List<AttendanceTimeWindowDto>>("Only the Production House of this request can define the attendance time window.",
                "فقط بيت الإنتاج صاحب الطلب يمكنه تحديد وقت تسجيل الحضور.", 403);

        if (sr.StatusId != LookupConstants.ServiceRequestStatus.InProgress)
            return Fail<List<AttendanceTimeWindowDto>>("The service request is not in progress.", "طلب الخدمة غير قيد التنفيذ.");

        var from = dto.FromDate.Date;
        var to = (dto.ToDate ?? dto.FromDate).Date;
        var today = (DateTime.UtcNow + LocalOffset).Date;

        if (to < from || (to - from).TotalDays > MaxRangeDays)
            return Fail<List<AttendanceTimeWindowDto>>("Invalid date range.", "نطاق التاريخ غير صالح.");
        if (from < sr.StartDate.Date || to > sr.EndDate.Date)
            return Fail<List<AttendanceTimeWindowDto>>("Dates must be within the service request period.", "يجب أن تكون التواريخ ضمن فترة طلب الخدمة.");
        if (from < today)
            return Fail<List<AttendanceTimeWindowDto>>("A time window cannot be created or changed for a past day.", "لا يمكن إنشاء أو تعديل وقت التسجيل ليوم سابق.");
        // The number of slots must equal the number of check-ins the service requires per day.
        if (sr.AttendanceFrequency <= 0)
            return Fail<List<AttendanceTimeWindowDto>>("Attendance frequency is not configured for this service request.", "عدد مرات تسجيل الحضور غير محدد لطلب الخدمة.");
        if (dto.Slots is null || dto.Slots.Count != sr.AttendanceFrequency)
            return Fail<List<AttendanceTimeWindowDto>>($"You must define exactly {sr.AttendanceFrequency} time windows (one per check-in).",
                $"يجب تحديد {sr.AttendanceFrequency} فتحات بالضبط (فتحة لكل مرة تسجيل).");

        var slots = dto.Slots.OrderBy(x => x.OpensAtLocal).ToList();
        for (var i = 0; i < slots.Count; i++)
        {
            if (slots[i].OpensAtLocal < TimeSpan.Zero || slots[i].ClosesAtLocal > TimeSpan.FromHours(24) || slots[i].ClosesAtLocal <= slots[i].OpensAtLocal)
                return Fail<List<AttendanceTimeWindowDto>>("Each window must close after it opens (same day).", "يجب أن يكون وقت إغلاق كل فتحة بعد وقت فتحها في نفس اليوم.");
            if (i > 0 && slots[i].OpensAtLocal < slots[i - 1].ClosesAtLocal)
                return Fail<List<AttendanceTimeWindowDto>>("Time windows must not overlap.", "يجب ألا تتداخل فتحات التسجيل.");
        }

        // A day that already has recorded attendance can no longer be re-scheduled.
        var recorded = await _repo.ListByServiceRequestAsync(sr.Id, from, to, ct);
        if (recorded.Count > 0)
            return Fail<List<AttendanceTimeWindowDto>>(
                $"Attendance is already recorded on {recorded.Min(r => r.AttendanceForDate):yyyy-MM-dd}; its windows can no longer be changed.",
                "تم تسجيل حضور في أحد هذه الأيام، ولا يمكن تغيير فتحاته.");

        var now = DateTime.UtcNow;
        var existing = (await _repo.ListWindowsAsync(sr.Id, from, to, ct)).ToDictionary(w => (w.AttendanceDate.Date, w.SlotNumber));
        var result = new List<AttendanceTimeWindow>();

        for (var day = from; day <= to; day = day.AddDays(1))
        {
            for (var i = 0; i < slots.Count; i++)
            {
                var number = i + 1;
                var opens = day + slots[i].OpensAtLocal - LocalOffset;
                var closes = day + slots[i].ClosesAtLocal - LocalOffset;

                if (existing.TryGetValue((day, number), out var w))
                {
                    w.OpensAtUtc = opens; w.ClosesAtUtc = closes;
                    w.UpdatedBy = user.UserId; w.UpdatedDate = now;
                }
                else
                {
                    w = new AttendanceTimeWindow
                    {
                        ServiceRequestId = sr.Id,
                        AttendanceDate = day,
                        SlotNumber = number,
                        OpensAtUtc = opens,
                        ClosesAtUtc = closes,
                        CreatedBy = user.UserId
                    };
                    await _repo.AddWindowAsync(w, ct);
                }
                result.Add(w);
            }
        }

        await _uow.SaveChangesAsync(ct);
        return ApiResponse<List<AttendanceTimeWindowDto>>.SuccessResponse(
            result.Select(ToDto).ToList(), "Attendance time window saved.", "تم حفظ وقت تسجيل الحضور.");
    }

    public async Task<ApiResponse<List<AttendanceTimeWindowDto>>> GetTimeWindowsAsync(
        int currentUserId, int serviceRequestId, DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        var (user, sr, error) = await LoadForViewAsync(currentUserId, serviceRequestId, ct);
        if (error is not null) return Fail<List<AttendanceTimeWindowDto>>(error.En!, error.Ar!, error.Status);

        var f = (from ?? sr!.StartDate).Date;
        var t = (to ?? sr!.EndDate).Date;
        var windows = await _repo.ListWindowsAsync(sr!.Id, f, t, ct);
        return ApiResponse<List<AttendanceTimeWindowDto>>.SuccessResponse(
            windows.Select(ToDto).ToList(), "Time windows fetched.", "تم جلب أوقات التسجيل.");
    }


    //  2) Recording (Association or Production House)

    public async Task<ApiResponse<WorkerAttendanceDto>> CheckInAsync(int currentUserId, RecordAttendanceDto dto, CancellationToken ct = default)
    {
        var user = await GetActiveUserAsync(currentUserId, ct);
        if (user is null) return Unauthorized<WorkerAttendanceDto>();

        if (string.IsNullOrWhiteSpace(dto.QrCode))
            return Fail<WorkerAttendanceDto>("The worker's QR code is required.", "رمز QR الخاص بالعامل مطلوب.");

        var sr = await _uow.ServiceRequests.GetByIdAsync(dto.ServiceRequestId, ct);
        if (sr is null) return Fail<WorkerAttendanceDto>("Service request was not found.", "طلب الخدمة غير موجود.", 404);

        // The worker is identified ONLY by his own QR code, and must be assigned to this very request.
        var link = await _repo.GetActiveLinkByQrAsync(dto.QrCode.Trim(), sr.Id, ct);
        if (link is null)
            return Fail<WorkerAttendanceDto>("This QR code does not belong to a worker assigned to this service request.",
                "رمز QR هذا لا يخص عاملاً مسجلاً في طلب الخدمة هذا.", 404);

        var now = DateTime.UtcNow;
        var date = (dto.AttendanceForDate ?? (now + LocalOffset)).Date;

        var gate = await ValidateRecordingAsync(user, sr, date, now, true, ct);
        if (gate.En is not null) return Fail<WorkerAttendanceDto>(gate.En, gate.Ar!, gate.Status);

        if (sr.AttendanceFrequency <= 0)
            return Fail<WorkerAttendanceDto>("Attendance frequency is not configured for this service request.", "عدد مرات تسجيل الحضور غير محدد لطلب الخدمة.");

        // The scan belongs to the window that is open right now, and a worker can be scanned ONCE per window.
        var slot = gate.Slot!;
        if (slot.SlotNumber > sr.AttendanceFrequency)
            return Fail<WorkerAttendanceDto>("This window exceeds the required number of check-ins.", "هذه الفتحة تتجاوز عدد مرات التسجيل المطلوبة.");

        var existing = await _repo.ListByLinkAndDateAsync(link.Id, date, ct);
        if (existing.Any(a => a.CheckInNumber == slot.SlotNumber))
            return Fail<WorkerAttendanceDto>($"This worker was already scanned in the current window (#{slot.SlotNumber}). Wait for the next window.",
                $"تم تسجيل هذا العامل مسبقاً في فتحة التسجيل الحالية (#{slot.SlotNumber}). انتظر الفتحة التالية.", 409);

        var entry = new WorkerAttendance
        {
            WorkerProjectLinkId = link.Id,
            AttendanceForDate = date,
            CheckInNumber = slot.SlotNumber,
            EntryTypeId = dto.EntryTypeId != 0 ? dto.EntryTypeId : LookupConstants.AttendanceEntryType.Present,
            CityId = sr.Association.CityId,
            CreatedBy = user.UserId
        };

        if (gate.Side == Side.Association)
        {
            entry.AssociationUserId = user.UserId;
            entry.AssociationCheckInDate = now;
            entry.ApprovalStatusId = LookupConstants.AttendanceApprovalStatus.Pending;   // waits for the Production House
        }
        else
        {
            entry.ProductionCompanyUserId = user.UserId;
            entry.ProductionCompanyCheckInDate = now;
            entry.ApprovalStatusId = LookupConstants.AttendanceApprovalStatus.Approved;  // the Production House is the approver
            entry.ReviewedByUserId = user.UserId;
            entry.ReviewedDate = now;
        }

        await _repo.AddAsync(entry, ct);
        await _uow.SaveChangesAsync(ct);

        var saved = (await _repo.GetByIdAsync(entry.Id, ct))!;     // reloads with the lookup names
        var result = ToDto(saved);
        result.WorkerId = link.WorkerId;
        result.WorkerName = link.Worker is null ? null : $"{link.Worker.FirstName} {link.Worker.LastName}";   // lets the scanner confirm who was scanned
        return ApiResponse<WorkerAttendanceDto>.SuccessResponse(result, "Attendance recorded.", "تم تسجيل الحضور.", 201);
    }
    public async Task<ApiResponse<WorkerAttendanceDto>> CheckOutAsync(int currentUserId, RecordAttendanceDto dto, CancellationToken ct = default)
    {
        var user = await GetActiveUserAsync(currentUserId, ct);
        if (user is null) return Unauthorized<WorkerAttendanceDto>();

        if (string.IsNullOrWhiteSpace(dto.QrCode))
            return Fail<WorkerAttendanceDto>("The worker's QR code is required.", "رمز QR الخاص بالعامل مطلوب.");

        var sr = await _uow.ServiceRequests.GetByIdAsync(dto.ServiceRequestId, ct);
        if (sr is null) return Fail<WorkerAttendanceDto>("Service request was not found.", "طلب الخدمة غير موجود.", 404);

        var link = await _repo.GetActiveLinkByQrAsync(dto.QrCode.Trim(), sr.Id, ct);
        if (link is null)
            return Fail<WorkerAttendanceDto>("This QR code does not belong to a worker assigned to this service request.",
                "رمز QR هذا لا يخص عاملاً مسجلاً في طلب الخدمة هذا.", 404);

        var now = DateTime.UtcNow;
        var date = (dto.AttendanceForDate ?? (now + LocalOffset)).Date;
        var gate = await ValidateRecordingAsync(user, sr, date, now, false, ct);
        if (gate.En is not null) return Fail<WorkerAttendanceDto>(gate.En, gate.Ar!, gate.Status);

        // Close the latest check-in of this worker that THIS side opened and has not closed yet.
        var rows = await _repo.ListByLinkAndDateAsync(link.Id, date, ct);
        var open = rows.LastOrDefault(r =>
            r.EntryTypeId == LookupConstants.AttendanceEntryType.Present &&
            r.ApprovalStatusId != LookupConstants.AttendanceApprovalStatus.Rejected &&
            (gate.Side == Side.Association
                ? r.AssociationCheckInDate != null && r.AssociationCheckOutDate == null
                : r.ProductionCompanyCheckInDate != null && r.ProductionCompanyCheckOutDate == null));

        if (open is null)
            return Fail<WorkerAttendanceDto>("This worker has no open check-in to close.", "لا يوجد تسجيل دخول مفتوح لهذا العامل.");

        if (gate.Side == Side.Association) open.AssociationCheckOutDate = now;
        else open.ProductionCompanyCheckOutDate = now;
        open.UpdatedBy = user.UserId; open.UpdatedDate = now;

        await _uow.SaveChangesAsync(ct);
        var result = ToDto(open);
        result.WorkerId = link.WorkerId;
        result.WorkerName = link.Worker is null ? null : $"{link.Worker.FirstName} {link.Worker.LastName}";
        return ApiResponse<WorkerAttendanceDto>.SuccessResponse(result, "Check-out recorded.", "تم تسجيل الخروج.");
    }

    //  3) Approval (Production House, one record = one worker at a time)
    public Task<ApiResponse<WorkerAttendanceDto>> ApproveAsync(int currentUserId, int attendanceId, CancellationToken ct = default) =>
        ReviewAsync(currentUserId, attendanceId, approve: true, reason: null, ct);

    public Task<ApiResponse<WorkerAttendanceDto>> RejectAsync(int currentUserId, int attendanceId, string reason, CancellationToken ct = default) =>
        ReviewAsync(currentUserId, attendanceId, approve: false, reason, ct);

    private async Task<ApiResponse<WorkerAttendanceDto>> ReviewAsync(int currentUserId, int attendanceId, bool approve, string? reason, CancellationToken ct)
    {
        var user = await GetActiveUserAsync(currentUserId, ct);
        if (user is null) return Unauthorized<WorkerAttendanceDto>();

        if (!approve && string.IsNullOrWhiteSpace(reason))
            return Fail<WorkerAttendanceDto>("A rejection reason is required.", "سبب الرفض مطلوب.");

        var a = await _repo.GetByIdAsync(attendanceId, ct);
        if (a is null) return Fail<WorkerAttendanceDto>("Attendance record was not found.", "سجل الحضور غير موجود.", 404);

        var sr = await _uow.ServiceRequests.GetByIdAsync(a.WorkerProjectLink.ServiceRequestId, ct);
        if (sr is null) return Fail<WorkerAttendanceDto>("Service request was not found.", "طلب الخدمة غير موجود.", 404);

        if (!Permissions(user).Contains(ManagePermission) || !IsProductionHouseOf(user, sr))
            return Fail<WorkerAttendanceDto>("Only the Production House of this request can approve or reject attendance.",
                "فقط بيت الإنتاج صاحب الطلب يمكنه اعتماد أو رفض الحضور.", 403);

        if (sr.StatusId != LookupConstants.ServiceRequestStatus.InProgress)
            return Fail<WorkerAttendanceDto>("The service request is closed; attendance can no longer be changed.", "طلب الخدمة مغلق ولا يمكن تعديل الحضور.");

        if (a.ApprovalStatusId != LookupConstants.AttendanceApprovalStatus.Pending)
            return Fail<WorkerAttendanceDto>("This attendance record was already reviewed.", "تمت مراجعة سجل الحضور هذا مسبقاً.");

        var now = DateTime.UtcNow;
        a.ApprovalStatusId = approve
            ? LookupConstants.AttendanceApprovalStatus.Approved
            : LookupConstants.AttendanceApprovalStatus.Rejected;
        a.RejectionReason = approve ? null : reason!.Trim();
        a.ReviewedByUserId = user.UserId;
        a.ReviewedDate = now;
        a.UpdatedBy = user.UserId; a.UpdatedDate = now;

        await _uow.SaveChangesAsync(ct);
        a = (await _repo.GetByIdAsync(a.Id, ct))!;
        return ApiResponse<WorkerAttendanceDto>.SuccessResponse(ToDto(a),
            approve ? "Attendance approved." : "Attendance rejected.",
            approve ? "تم اعتماد الحضور." : "تم رفض الحضور.");
    }

    //  4) Reads: day roster + summary (final wages preview)
    public async Task<ApiResponse<DayRosterDto>> GetDayRosterAsync(int currentUserId, int serviceRequestId, DateTime? date, CancellationToken ct = default)
    {
        var (_, sr, error) = await LoadForViewAsync(currentUserId, serviceRequestId, ct);
        if (error is not null) return Fail<DayRosterDto>(error.En!, error.Ar!, error.Status);

        var day = (date ?? (DateTime.UtcNow + LocalOffset)).Date;
        var links = await _repo.ListLinksByServiceRequestAsync(sr!.Id, ct);
        var rows = await _repo.ListByServiceRequestAsync(sr.Id, day, day, ct);
        var slots = await _repo.ListWindowsForDateAsync(sr.Id, day, ct);
        var now = DateTime.UtcNow;

        var dto = new DayRosterDto
        {
            ServiceRequestId = sr.Id,
            Date = day,
            ExpectedCheckInsPerDay = sr.AttendanceFrequency,
            Slots = slots.Select(ToDto).ToList(),
            CurrentSlotNumber = slots.FirstOrDefault(x => now >= x.OpensAtUtc && now < x.ClosesAtUtc)?.SlotNumber
        };

        foreach (var link in links)
        {
            var mine = rows.Where(r => r.WorkerProjectLinkId == link.Id).ToList();
            dto.Workers.Add(new DayRosterItemDto
            {
                WorkerProjectLinkId = link.Id,
                WorkerId = link.WorkerId,
                WorkerName = link.Worker is null ? null : $"{link.Worker.FirstName} {link.Worker.LastName}",
                DayStatus = ComputeDayStatus(sr.AttendanceFrequency, day, slots, mine, now).ToString(),
                ExpectedCheckIns = sr.AttendanceFrequency,
                RecordedCheckIns = mine.Count,
                ApprovedCheckIns = mine.Count(r => r.ApprovalStatusId == LookupConstants.AttendanceApprovalStatus.Approved),
                PendingCheckIns = mine.Count(r => r.ApprovalStatusId == LookupConstants.AttendanceApprovalStatus.Pending),
                Entries = mine.Select(ToDto).ToList()
            });
        }

        return ApiResponse<DayRosterDto>.SuccessResponse(dto, "Attendance roster fetched.", "تم جلب كشف الحضور.");
    }

    public async Task<ApiResponse<AttendanceSummaryDto>> GetSummaryAsync(int currentUserId, int serviceRequestId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        var (_, sr, error) = await LoadForViewAsync(currentUserId, serviceRequestId, ct);
        if (error is not null) return Fail<AttendanceSummaryDto>(error.En!, error.Ar!, error.Status);

        var f = from.Date < sr!.StartDate.Date ? sr.StartDate.Date : from.Date;
        var t = to.Date > sr.EndDate.Date ? sr.EndDate.Date : to.Date;
        if (t < f || (t - f).TotalDays > MaxRangeDays)
            return Fail<AttendanceSummaryDto>("Invalid date range.", "نطاق التاريخ غير صالح.");

        var links = await _repo.ListLinksByServiceRequestAsync(sr.Id, ct);
        var rows = (await _repo.ListByServiceRequestAsync(sr.Id, f, t, ct)).ToLookup(r => (r.WorkerProjectLinkId, r.AttendanceForDate.Date));
        var windows = (await _repo.ListWindowsAsync(sr.Id, f, t, ct)).ToLookup(w => w.AttendanceDate.Date);
        var overtimes = (await _repo.ListOvertimeByServiceRequestAsync(sr.Id, f, t, ct)).ToLookup(o => (o.WorkerProjectLinkId, o.OvertimeForDate.Date));
        var now = DateTime.UtcNow;

        var dto = new AttendanceSummaryDto
        {
            ServiceRequestId = sr.Id,
            From = f,
            To = t,
            ExpectedCheckInsPerDay = sr.AttendanceFrequency,
            ServiceRequestShiftsCount = sr.ShiftsCount
        };

        foreach (var link in links)
        {
            var item = new AttendanceSummaryItemDto
            {
                WorkerProjectLinkId = link.Id,
                WorkerId = link.WorkerId,
                WorkerName = link.Worker is null ? null : $"{link.Worker.FirstName} {link.Worker.LastName}",
                DailyWageAmount = link.DailyWageAmount
            };

            // A worker is only judged from the day he was linked to the request.
            var start = (link.CreatedDate + LocalOffset).Date;
            for (var day = f > start ? f : start; day <= t; day = day.AddDays(1))
            {
                var status = ComputeDayStatus(sr.AttendanceFrequency, day, windows[day].ToList(), rows[(link.Id, day)].ToList(), now);
                switch (status)
                {
                    case AttendanceDayStatus.Present: item.PresentDays++; break;
                    case AttendanceDayStatus.Absent: item.AbsentDays++; break;
                    case AttendanceDayStatus.PendingApproval: item.PendingApprovalDays++; break;
                    case AttendanceDayStatus.Open: item.OpenDays++; break;
                    default: item.NotScheduledDays++; break;
                }

                // Overtime: pending is only shown; approved counts ONLY on a day the worker was Present; rejected never counts.
                var overtime = overtimes[(link.Id, day)].FirstOrDefault();
                if (overtime is not null)
                {
                    if (overtime.ApprovalStatusId == LookupConstants.AttendanceApprovalStatus.Pending)
                    {
                        item.PendingOvertimeAmount += overtime.Amount;
                    }
                    else if (overtime.ApprovalStatusId == LookupConstants.AttendanceApprovalStatus.Approved && status == AttendanceDayStatus.Present)
                    {
                        item.ApprovedOvertimeHours += overtime.Hours;
                        item.ApprovedOvertimeAmount += overtime.Amount;
                    }
                }
            }

            item.AttendanceAmount = item.PresentDays * item.DailyWageAmount;                 // unapproved attendance never counts
            item.PayableAmount = item.AttendanceAmount + item.ApprovedOvertimeAmount;        // unapproved overtime never counts
            dto.TotalApprovedOvertimeAmount += item.ApprovedOvertimeAmount;
            dto.TotalPendingOvertimeAmount += item.PendingOvertimeAmount;
            dto.TotalPayableAmount += item.PayableAmount;
            dto.Workers.Add(item);
        }

        return ApiResponse<AttendanceSummaryDto>.SuccessResponse(dto, "Attendance summary fetched.", "تم جلب ملخص الحضور.");
    }

    
    //  Business rules
    

    // Day outcome. Present only if ALL required check-ins are recorded AND approved.
    //Otherwise, once the day is over (window closed) the worker is automatically Absent for the whole day.
    //Computed on read, so nothing needs a background job and approvals made later are picked up automatically.
    private static AttendanceDayStatus ComputeDayStatus(int frequency, DateTime date, IReadOnlyList<AttendanceTimeWindow> slots,
        IReadOnlyList<WorkerAttendance> rows, DateTime nowUtc)
    {
        if (slots.Count == 0 && rows.Count == 0) return AttendanceDayStatus.NotScheduled;

        var approvedPresent = rows.Count(r => r.EntryTypeId == LookupConstants.AttendanceEntryType.Present && r.ApprovalStatusId == LookupConstants.AttendanceApprovalStatus.Approved);
        if (frequency > 0 && approvedPresent >= frequency) return AttendanceDayStatus.Present;

        // A rejected record or an approved absence decides the day immediately.
        if (rows.Any(r => r.ApprovalStatusId == LookupConstants.AttendanceApprovalStatus.Rejected ||
                          (r.EntryTypeId == LookupConstants.AttendanceEntryType.Absent && r.ApprovalStatusId == LookupConstants.AttendanceApprovalStatus.Approved)))
            return AttendanceDayStatus.Absent;

        var closesAt = slots.Count > 0 ? slots.Max(x => x.ClosesAtUtc) : date.AddDays(1) - LocalOffset;   // day ends when its LAST window closes
        if (nowUtc <= closesAt)
            return rows.Any(r => r.ApprovalStatusId == LookupConstants.AttendanceApprovalStatus.Pending)
                ? AttendanceDayStatus.PendingApproval
                : AttendanceDayStatus.Open;

        return AttendanceDayStatus.Absent;   // automatic absence for the entire day
    }

    private async Task<Gate> ValidateRecordingAsync(User user, ServiceRequest sr, DateTime date, DateTime nowUtc, bool requireOpenSlot, CancellationToken ct)
    {
        if (!Permissions(user).Contains(ManagePermission))
            return new Gate(Side.None, "You do not have permission to record attendance.", "ليس لديك صلاحية تسجيل الحضور.", 403);

        if (sr.StatusId != LookupConstants.ServiceRequestStatus.InProgress)
            return new Gate(Side.None, "Attendance cannot be recorded: the service request is not in progress (closed, completed, cancelled or not yet approved).",
                "لا يمكن تسجيل الحضور: طلب الخدمة غير قيد التنفيذ (مغلق أو مكتمل أو ملغى أو لم تتم الموافقة عليه).");

        if (date < sr.StartDate.Date || date > sr.EndDate.Date)
            return new Gate(Side.None, "The date is outside the service request period.", "التاريخ خارج فترة طلب الخدمة.");

        var side = await ResolveSideAsync(user, sr, ct);
        if (side == Side.None)
            return new Gate(Side.None, "You are not allowed to record attendance for this service request.", "غير مصرح لك بتسجيل الحضور لهذا الطلب.", 403);

        // Check-out happens at the end of the shift, so it is not tied to a scan window.
        if (!requireOpenSlot) return new Gate(side);

        var daySlots = await _repo.ListWindowsForDateAsync(sr.Id, date, ct);
        if (daySlots.Count == 0)
            return new Gate(Side.None, "The Production House has not defined time windows for this day.", "لم يحدد بيت الإنتاج فتحات التسجيل لهذا اليوم.");

        var open = daySlots.FirstOrDefault(x => nowUtc >= x.OpensAtUtc && nowUtc < x.ClosesAtUtc);
        if (open is null)
            return new Gate(Side.None, "Attendance can only be recorded inside an open time window.", "يمكن تسجيل الحضور فقط خلال فتحة تسجيل مفتوحة.");

        return new Gate(side, Slot: open);
    }

    private async Task<Side> ResolveSideAsync(User user, ServiceRequest sr, CancellationToken ct)
    {
        // Production House side = the Location Manager assigned to the project (the person who is physically on site).
        if (IsProductionHouseOf(user, sr))
            return sr.Project.LocationManagerUserId == user.UserId ? Side.ProductionCompany : Side.None;

        // Association Representative = an Association user assigned as supervisor on the request's project.
        if (user.EntityType == EntityType.Association && user.EntityId == sr.AssociationId)
        {
            var supervisors = await _uow.AssociationProjectSupervisors.GetByProjectIdAsync(sr.ProjectId, ct);
            if (supervisors.Any(s => s.UserId == user.UserId)) return Side.Association;
        }
        return Side.None;
    }

    private static bool IsProductionHouseOf(User user, ServiceRequest sr) =>
        user.EntityType == EntityType.ProductionCompany && user.EntityId == sr.ProductionCompanyId;

    private async Task<(User? User, ServiceRequest? Sr, Gate? Error)> LoadForViewAsync(int currentUserId, int serviceRequestId, CancellationToken ct)
    {
        var user = await GetActiveUserAsync(currentUserId, ct);
        if (user is null) return (null, null, new Gate(Side.None, "Current user was not found or is inactive.", "لم يتم العثور على المستخدم الحالي أو أن حسابه غير نشط.", 401));

        var sr = await _uow.ServiceRequests.GetByIdAsync(serviceRequestId, ct);
        if (sr is null) return (user, null, new Gate(Side.None, "Service request was not found.", "طلب الخدمة غير موجود.", 404));

        var perms = Permissions(user);
        var hasPermission = perms.Contains(ViewPermission) || perms.Contains(ManagePermission);
        var inScope = user.EntityType is EntityType.BaytAlUrdon or EntityType.Aseza
                      || IsProductionHouseOf(user, sr)
                      || (user.EntityType == EntityType.Association && user.EntityId == sr.AssociationId);

        if (!hasPermission || !inScope)
            return (user, sr, new Gate(Side.None, "You do not have access to this attendance data.", "ليس لديك صلاحية الوصول إلى بيانات الحضور هذه.", 403));

        return (user, sr, null);
    }

    
    //  Helpers
    
    private async Task<User?> GetActiveUserAsync(int id, CancellationToken ct)
    {
        var u = await _uow.Users.GetWithPermissionsAsync(id, ct);
        return u is { IsActive: true } ? u : null;
    }

    private static HashSet<string> Permissions(User user)
    {
        var role = user.Role.RolePermissions
            .Where(rp => rp.IsActive && rp.Permission.IsActive).Select(rp => rp.Permission.PermissionNameEn);
        var direct = user.UserPermissions
            .Where(up => up.IsActive && up.Permission.IsActive).Select(up => up.Permission.PermissionNameEn);
        var group = user.UserGroups
            .Where(ug => ug.Group.IsActive).SelectMany(ug => ug.Group.GroupPermissions)
            .Where(gp => gp.IsActive && gp.Permission.IsActive).Select(gp => gp.Permission.PermissionNameEn);
        return role.Concat(direct).Concat(group).ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static ApiResponse<T> Fail<T>(string en, string ar, int status = 400)
    {
        var r = ApiResponse<T>.FailureResponse(en, ar);
        r.StatusCode = status;
        return r;
    }

    private static ApiResponse<T> Unauthorized<T>() =>
        Fail<T>("Current user was not found or is inactive.", "لم يتم العثور على المستخدم الحالي أو أن حسابه غير نشط.", 401);

    private static AttendanceTimeWindowDto ToDto(AttendanceTimeWindow w) => new()
    {
        Date = w.AttendanceDate.Date,
        SlotNumber = w.SlotNumber,
        OpensAtUtc = w.OpensAtUtc,
        ClosesAtUtc = w.ClosesAtUtc,
        OpensAtLocal = (w.OpensAtUtc + LocalOffset).TimeOfDay,
        ClosesAtLocal = (w.ClosesAtUtc + LocalOffset).TimeOfDay,
      
    };

    private static WorkerAttendanceDto ToDto(WorkerAttendance a) => new()
    {
        Id = a.Id,
        WorkerProjectLinkId = a.WorkerProjectLinkId,
        AttendanceForDate = a.AttendanceForDate.Date,
        CheckInNumber = a.CheckInNumber,
        EntryTypeId = a.EntryTypeId,
        EntryType = LookupConstants.AttendanceEntryTypeCode(a.EntryTypeId),
        EntryTypeNameEn = a.EntryType?.EnglishName,
        EntryTypeNameAr = a.EntryType?.ArabicName,
        ApprovalStatusId = a.ApprovalStatusId,
        ApprovalStatus = LookupConstants.AttendanceApprovalStatusCode(a.ApprovalStatusId),
        ApprovalStatusNameEn = a.ApprovalStatus?.EnglishName,
        ApprovalStatusNameAr = a.ApprovalStatus?.ArabicName,
        CityId = a.CityId,
        AssociationUserId = a.AssociationUserId,
        AssociationCheckInDate = a.AssociationCheckInDate,
        AssociationCheckOutDate = a.AssociationCheckOutDate,
        ProductionCompanyUserId = a.ProductionCompanyUserId,
        ProductionCompanyCheckInDate = a.ProductionCompanyCheckInDate,
        ProductionCompanyCheckOutDate = a.ProductionCompanyCheckOutDate,
        ReviewedByUserId = a.ReviewedByUserId,
        ReviewedDate = a.ReviewedDate,
        RejectionReason = a.RejectionReason
    };
}
