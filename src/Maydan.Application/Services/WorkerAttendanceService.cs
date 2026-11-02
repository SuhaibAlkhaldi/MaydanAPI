using Maydan.Application.DTOs.Attendance;
using Maydan.Application.DTOs.Common;
using Maydan.Application.DTOs.ServiceRequests;
using Maydan.Application.Interfaces;
using Maydan.Domain.Common;
using Maydan.Domain.Entities;
using Maydan.Domain.Enums;

namespace Maydan.Application.Services;

public partial class WorkerAttendanceService : IWorkerAttendanceService
{
    private const string ViewPermission = "View Attendance";
    private const string ManagePermission = "Manage Attendance";
    private const string ResolvePermission = "Resolve Attendance";
    private const int MaxRangeDays = 366;

    // Jordan is permanently UTC+3 (no daylight saving since 2022).
    // Windows are entered in local time and stored in UTC.
    private static readonly TimeSpan LocalOffset = TimeSpan.FromHours(3);

    private readonly IUnitOfWork _uow;
    private readonly IWorkerAttendanceRepository _repo;
    private readonly IEmailSender _email;


    public WorkerAttendanceService(IUnitOfWork uow, IWorkerAttendanceRepository repo,IEmailSender email)
    {
        _uow = uow;
        _repo = repo;
        _email = email;
    }

    private enum Side { None, Association, ProductionCompany }

    private sealed record Gate(Side Side, string? En = null, string? Ar = null, int Status = 400, AttendanceTimeWindow? Slot = null);

    
    //  1) Time windows (Production House)
    
    public async Task<ApiResponse<List<AttendanceTimeWindowDto>>> SetTimeWindowsAsync(
        int currentUserId, int serviceRequestId, SetAttendanceWindowDto dto, CancellationToken ct = default)
    {
        var user = await GetActiveUserAsync(currentUserId, ct);
        if (user is null) return Unauthorized<List<AttendanceTimeWindowDto>>();

        var sr = await _uow.ServiceRequests.GetAttendanceInfoAsync(serviceRequestId, ct);
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

        var sr = await _uow.ServiceRequests.GetAttendanceInfoAsync(dto.ServiceRequestId, ct);
        if (sr is null) return Fail<WorkerAttendanceDto>("Service request was not found.", "طلب الخدمة غير موجود.", 404);

        // العامل بيتحدد بالـ QR تبعه بس، ولازم يكون مسجل بنفس الطلب
        var link = await _repo.GetActiveLinkByQrAsync(dto.QrCode.Trim(), sr.Id, ct);
        if (link is null)
            return Fail<WorkerAttendanceDto>("This QR code does not belong to a worker assigned to this service request.",
                "رمز QR هذا لا يخص عاملاً مسجلاً في طلب الخدمة هذا.", 404);

        var date = (dto.AttendanceForDate ?? (DateTime.UtcNow + LocalOffset)).Date;
        return await RecordSideAsync(user, sr, link, date, LookupConstants.AttendanceEntryType.Present, null, ct);
    }

    public async Task<ApiResponse<WorkerAttendanceDto>> MarkAbsentAsync(int currentUserId, MarkAbsentDto dto, CancellationToken ct = default)
    {
        var user = await GetActiveUserAsync(currentUserId, ct);
        if (user is null) return Unauthorized<WorkerAttendanceDto>();

        var sr = await _uow.ServiceRequests.GetAttendanceInfoAsync(dto.ServiceRequestId, ct);
        if (sr is null) return Fail<WorkerAttendanceDto>("Service request was not found.", "طلب الخدمة غير موجود.", 404);

        // العامل بيتحدد من القائمة (بدون QR)
        var link = await _repo.GetActiveLinkByIdAsync(dto.WorkerProjectLinkId, sr.Id, ct);
        if (link is null)
            return Fail<WorkerAttendanceDto>("This worker is not assigned to this service request.",
                "هذا العامل غير مسجل في طلب الخدمة هذا.", 404);

        var date = (dto.AttendanceForDate ?? (DateTime.UtcNow + LocalOffset)).Date;
        return await RecordSideAsync(user, sr, link, date, LookupConstants.AttendanceEntryType.Absent, dto.Reason, ct);
    }

    // الميثود المشتركة: الطرف (جمعية / شركة إنتاج) بيسجل رأيه بالفتحة المفتوحة، حضور أو غياب
    private async Task<ApiResponse<WorkerAttendanceDto>> RecordSideAsync(
        User user, ServiceRequestAttendanceInfo sr, WorkerProjectLink link,
        DateTime date, int entryTypeId, string? reason, CancellationToken ct)
    {
        reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        if (reason is { Length: > 500 })
            return Fail<WorkerAttendanceDto>("The reason is too long (max 500 characters).", "السبب طويل جداً (الحد الأقصى 500 حرف).");

        var now = DateTime.UtcNow;

        // نفس قيد الفتحة المسموحة للحضور والغياب
        var gate = await ValidateRecordingAsync(user, sr, date, now, true, ct);
        if (gate.En is not null) return Fail<WorkerAttendanceDto>(gate.En, gate.Ar!, gate.Status);

        if (sr.AttendanceFrequency <= 0)
            return Fail<WorkerAttendanceDto>("Attendance frequency is not configured for this service request.", "عدد مرات تسجيل الحضور غير محدد لطلب الخدمة.");

        var slot = gate.Slot!;
        if (slot.SlotNumber > sr.AttendanceFrequency)
            return Fail<WorkerAttendanceDto>("This window exceeds the required number of check-ins.", "هذه الفتحة تتجاوز عدد مرات التسجيل المطلوبة.");

        var isAssoc = gate.Side == Side.Association;
        var isPresent = entryTypeId == LookupConstants.AttendanceEntryType.Present;

        var row = (await _repo.ListByLinkAndDateAsync(link.Id, date, ct))
                    .FirstOrDefault(a => a.CheckInNumber == slot.SlotNumber);
        var isNew = row is null;

        // كل طرف بيسجل مرة وحدة بالفتحة
        if (row is not null && (isAssoc ? row.AssociationEntryTypeId : row.ProductionCompanyEntryTypeId) is not null)
            return Fail<WorkerAttendanceDto>(
                $"You already recorded this worker in the current window (#{slot.SlotNumber}).",
                $"سجّلت هذا العامل مسبقاً في فتحة التسجيل الحالية (#{slot.SlotNumber}).", 409);

        // لو السجل انراجع (معتمد/مرفوض) ما بنعدّل عليه
        if (row is not null && row.ApprovalStatusId != LookupConstants.AttendanceApprovalStatus.Pending)
            return Fail<WorkerAttendanceDto>("This record was already reviewed.", "تمت مراجعة هذا السجل مسبقاً.", 409);

        if (row is null)
        {
            row = new WorkerAttendance
            {
                WorkerProjectLinkId = link.Id,
                AttendanceForDate = date,
                CheckInNumber = slot.SlotNumber,
                EntryTypeId = entryTypeId,   // أول تسجيل، وبيصير النهائي عند الاعتماد
                CityId = sr.CityId,
                ApprovalStatusId = LookupConstants.AttendanceApprovalStatus.Pending,   // دايماً Pending، حتى لو سجّل ممثل الشركة
                CreatedBy = user.UserId
            };
            await _repo.AddAsync(row, ct);
        }

        if (isAssoc)
        {
            row.AssociationEntryTypeId = entryTypeId;
            row.AssociationUserId = user.UserId;
            row.AssociationRecordedDate = now;
            row.AssociationReason = reason;
            if (isPresent) row.AssociationCheckInDate = now;
        }
        else
        {
            row.ProductionCompanyEntryTypeId = entryTypeId;
            row.ProductionCompanyUserId = user.UserId;
            row.ProductionCompanyRecordedDate = now;
            row.ProductionCompanyReason = reason;
            if (isPresent) row.ProductionCompanyCheckInDate = now;
        }

        if (!isNew) { row.UpdatedBy = user.UserId; row.UpdatedDate = now; }

        try
        {
            await _uow.SaveChangesAsync(ct);
        }
        catch (Exception) when (isNew)
        {
            // الطرفين سجّلوا بنفس اللحظة وانضرب الـ unique index؛ الفرونت بيعيد المحاولة
            return Fail<WorkerAttendanceDto>("The other representative recorded this worker at the same moment. Please try again.",
                "الممثل الآخر سجّل هذا العامل بنفس اللحظة. أعد المحاولة.", 409);
        }

        var result = ToDto(row);
        result.WorkerId = link.WorkerId;
        result.WorkerName = link.Worker is null ? null : $"{link.Worker.FirstName} {link.Worker.LastName}";
        return ApiResponse<WorkerAttendanceDto>.SuccessResponse(result,
            isPresent ? "Attendance recorded." : "Absence recorded.",
            isPresent ? "تم تسجيل الحضور." : "تم تسجيل الغياب.", isNew ? 201 : 200);
    }
    public async Task<ApiResponse<WorkerAttendanceDto>> CheckOutAsync(int currentUserId, RecordAttendanceDto dto, CancellationToken ct = default)
    {
        var user = await GetActiveUserAsync(currentUserId, ct);
        if (user is null) return Unauthorized<WorkerAttendanceDto>();

        if (string.IsNullOrWhiteSpace(dto.QrCode))
            return Fail<WorkerAttendanceDto>("The worker's QR code is required.", "رمز QR الخاص بالعامل مطلوب.");

        var sr = await _uow.ServiceRequests.GetAttendanceInfoAsync(dto.ServiceRequestId, ct);
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
     r.ApprovalStatusId != LookupConstants.AttendanceApprovalStatus.Rejected &&
     (gate.Side == Side.Association
         ? r.AssociationEntryTypeId == LookupConstants.AttendanceEntryType.Present
             && r.AssociationCheckInDate != null && r.AssociationCheckOutDate == null
         : r.ProductionCompanyEntryTypeId == LookupConstants.AttendanceEntryType.Present
             && r.ProductionCompanyCheckInDate != null && r.ProductionCompanyCheckOutDate == null));

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
    public Task<ApiResponse<WorkerAttendanceDto>> ApproveAsync(int currentUserId, int attendanceId, ApproveAttendanceDto? dto, CancellationToken ct = default) =>
    ReviewAsync(currentUserId, attendanceId, approve: true, reason: null, finalType: dto?.FinalEntryTypeId, ct);



    public Task<ApiResponse<WorkerAttendanceDto>> RejectAsync(int currentUserId, int attendanceId, string reason, CancellationToken ct = default) =>
     ReviewAsync(currentUserId, attendanceId, approve: false, reason, finalType: null, ct);

    private async Task<ApiResponse<WorkerAttendanceDto>> ReviewAsync(int currentUserId, int attendanceId, bool approve, string? reason,
    LookupConstants.AttendanceEntryTypeInput? finalType, CancellationToken ct)
    {
        var user = await GetActiveUserAsync(currentUserId, ct);
        if (user is null) return Unauthorized<WorkerAttendanceDto>();

        if (!approve && string.IsNullOrWhiteSpace(reason))
            return Fail<WorkerAttendanceDto>("A rejection reason is required.", "سبب الرفض مطلوب.");

        var a = await _repo.GetForReviewAsync(attendanceId, ct);
        if (a is null) return Fail<WorkerAttendanceDto>("Attendance record was not found.", "سجل الحضور غير موجود.", 404);

        var sr = await _uow.ServiceRequests.GetAttendanceInfoAsync(a.WorkerProjectLink.ServiceRequestId, ct);
        if (sr is null) return Fail<WorkerAttendanceDto>("Service request was not found.", "طلب الخدمة غير موجود.", 404);

        if (!Permissions(user).Contains(ManagePermission) || !IsProductionHouseOf(user, sr))
            return Fail<WorkerAttendanceDto>("Only the Production House of this request can approve or reject attendance.",
                "فقط بيت الإنتاج صاحب الطلب يمكنه اعتماد أو رفض الحضور.", 403);

        if (sr.StatusId != LookupConstants.ServiceRequestStatus.InProgress)
            return Fail<WorkerAttendanceDto>("The service request is closed; attendance can no longer be changed.", "طلب الخدمة مغلق ولا يمكن تعديل الحضور.");

        if (a.ApprovalStatusId != LookupConstants.AttendanceApprovalStatus.Pending)
            return Fail<WorkerAttendanceDto>("This attendance record was already reviewed.", "تمت مراجعة سجل الحضور هذا مسبقاً.");

        //------------------------------------------------ 
        var bothSides = a.AssociationEntryTypeId is not null && a.ProductionCompanyEntryTypeId is not null;
        if (!bothSides || a.EntryTypeId == LookupConstants.AttendanceEntryType.NotRecorded)
            return Fail<WorkerAttendanceDto>("A representative did not record this worker; only Bayt Al Urdon can resolve it.",
                "أحد الممثلين لم يسجّل هذا العامل؛ فقط بيت الأردن يمكنه معالجته.", 403);

        if (approve)
        {
            if (a.AssociationEntryTypeId == a.ProductionCompanyEntryTypeId)
                a.EntryTypeId = a.AssociationEntryTypeId!.Value;                       
            else if (finalType is null)
                return Fail<WorkerAttendanceDto>("The two representatives disagree; choose the final entry type (Present or Absent).",
                    "الممثلان مختلفان؛ اختر النوع النهائي (حاضر أو غائب).");
            else
                a.EntryTypeId = (int)finalType.Value;                                   
        }

        var now = DateTime.UtcNow;

//----------------------------------------------------------------------

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
        var (user, sr, error) = await LoadForViewAsync(currentUserId, serviceRequestId, ct);
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
        var mySide = await ResolveSideAsync(user!, sr!, ct);
        dto.MySide = mySide == Side.None ? null : mySide.ToString();

        foreach (var link in links)
        {
            var mine = rows.Where(r => r.WorkerProjectLinkId == link.Id).ToList();
            dto.Workers.Add(new DayRosterItemDto
            {
                SlotStatuses = slots.Select(s => ToSlotStatus(s.SlotNumber, mine.FirstOrDefault(r => r.CheckInNumber == s.SlotNumber))).ToList(),

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
        dto.CanFinalize = dto.Workers.All(w => w.PendingApprovalDays == 0 && w.OpenDays == 0);

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

        var approvedPresent = rows.Count(r => r.EntryTypeId == LookupConstants.AttendanceEntryType.Present
                                           && r.ApprovalStatusId == LookupConstants.AttendanceApprovalStatus.Approved);
        if (frequency > 0 && approvedPresent >= frequency) return AttendanceDayStatus.Present;

        // رفض أو غياب معتمد بيحسم اليوم فوراً (زي ما كان)
        if (rows.Any(r => r.ApprovalStatusId == LookupConstants.AttendanceApprovalStatus.Rejected ||
                          (r.EntryTypeId == LookupConstants.AttendanceEntryType.Absent && r.ApprovalStatusId == LookupConstants.AttendanceApprovalStatus.Approved)))
            return AttendanceDayStatus.Absent;

        // أي سجل بانتظار قرار (شركة الإنتاج أو بيت الأردن) = بانتظار، بغض النظر عن الوقت
        if (rows.Any(r => r.ApprovalStatusId == LookupConstants.AttendanceApprovalStatus.Pending))
            return AttendanceDayStatus.PendingApproval;

        var closesAt = slots.Count > 0 ? slots.Max(x => x.ClosesAtUtc) : date.AddDays(1) - LocalOffset;
        if (nowUtc <= closesAt) return AttendanceDayStatus.Open;

        // بعد نهاية اليوم: فتحة بدون سجل = الجوب لسا ما عالجها، فلسا مش غياب
        if (slots.Any(s => rows.All(r => r.CheckInNumber != s.SlotNumber)))
            return AttendanceDayStatus.PendingApproval;

        return AttendanceDayStatus.Absent;
    }

    private async Task<Gate> ValidateRecordingAsync(User user, ServiceRequestAttendanceInfo sr, DateTime date, DateTime nowUtc, bool requireOpenSlot, CancellationToken ct)
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

    private async Task<Side> ResolveSideAsync(User user, ServiceRequestAttendanceInfo sr, CancellationToken ct)
    {
        // Production House side = the Location Manager assigned to the project (the person who is physically on site).
        if (IsProductionHouseOf(user, sr))
            return sr.LocationManagerUserId == user.UserId ? Side.ProductionCompany : Side.None;

        // Association Representative = an Association user assigned as supervisor on the request's project.
        if (user.EntityType == EntityType.Association && user.EntityId == sr.AssociationId)
        {
            var supervisors = await _uow.AssociationProjectSupervisors.GetByProjectIdAsync(sr.ProjectId, ct);
            if (supervisors.Any(s => s.UserId == user.UserId)) return Side.Association;
        }
        return Side.None;
    }

    private static bool IsProductionHouseOf(User user, ServiceRequestAttendanceInfo sr) =>
        user.EntityType == EntityType.ProductionCompany && user.EntityId == sr.ProductionCompanyId;

    private async Task<(User? User, ServiceRequestAttendanceInfo? Sr, Gate? Error)> LoadForViewAsync(int currentUserId, int serviceRequestId, CancellationToken ct)
    {
        var user = await GetActiveUserAsync(currentUserId, ct);
        if (user is null) return (null, null, new Gate(Side.None, "Current user was not found or is inactive.", "لم يتم العثور على المستخدم الحالي أو أن حسابه غير نشط.", 401));

        var sr = await _uow.ServiceRequests.GetAttendanceInfoAsync(serviceRequestId, ct);
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
    private static bool CanResolveAttendance(User user) =>
    user.EntityType == EntityType.BaytAlUrdon && Permissions(user).Contains(ResolvePermission);
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
    private static string SideCode(int? id) =>
    id is null ? "NotRecorded" : LookupConstants.AttendanceEntryTypeCode(id.Value);

    private static SlotStatusDto ToSlotStatus(int slotNumber, WorkerAttendance? row)
    {
        var dto = new SlotStatusDto
        {
            SlotNumber = slotNumber,
            AttendanceId = row?.Id,
            AssociationStatus = SideCode(row?.AssociationEntryTypeId),
            ProductionCompanyStatus = SideCode(row?.ProductionCompanyEntryTypeId)
        };

        if (row is null) return dto;   // Status = NotRecorded

        // قرار نهائي معتمد (شركة الإنتاج أو بيت الأردن)
        if (row.ApprovalStatusId == LookupConstants.AttendanceApprovalStatus.Approved)
        { dto.Status = LookupConstants.AttendanceEntryTypeCode(row.EntryTypeId); return dto; }

        var a = row.AssociationEntryTypeId;
        var p = row.ProductionCompanyEntryTypeId;
        if (a is not null && p is not null)
            dto.Status = a == p ? LookupConstants.AttendanceEntryTypeCode(a.Value) : "Conflict";
        // طرف واحد بس: بيضل NotRecorded، والتفصيل بالـ side statuses

        return dto;
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
        AssociationEntryTypeId = a.AssociationEntryTypeId,
        AssociationEntryType = a.AssociationEntryTypeId is null ? null : LookupConstants.AttendanceEntryTypeCode(a.AssociationEntryTypeId.Value),
        AssociationRecordedDate = a.AssociationRecordedDate,
        AssociationReason = a.AssociationReason,
        ProductionCompanyEntryTypeId = a.ProductionCompanyEntryTypeId,
        ProductionCompanyEntryType = a.ProductionCompanyEntryTypeId is null ? null : LookupConstants.AttendanceEntryTypeCode(a.ProductionCompanyEntryTypeId.Value),
        ProductionCompanyRecordedDate = a.ProductionCompanyRecordedDate,
        ProductionCompanyReason = a.ProductionCompanyReason,
        NotRecordedReason = a.NotRecordedReason,
        ResolutionNote = a.ResolutionNote,
        IsConflict = a.AssociationEntryTypeId != null && a.ProductionCompanyEntryTypeId != null
             && a.AssociationEntryTypeId != a.ProductionCompanyEntryTypeId,
        RejectionReason = a.RejectionReason
    };
}
