using Maydan.Application.DTOs.Attendance;
using Maydan.Application.DTOs.Common;
using Maydan.Application.DTOs.Lookups;
using Maydan.Domain.Common;
using Maydan.Domain.Entities;
using Maydan.Domain.Enums;

namespace Maydan.Application.Services;

// Overtime = hours worked AFTER the check-out. Recorded on site, counted in the total only after
// Bayt Al Urdon approves it.
public partial class WorkerAttendanceService
{
    private const string ApproveOvertimePermission = "Approve Overtime";
    private const decimal MinOvertimeHours = 1m;
    private const decimal MaxOvertimeHours = 12m;

    //Hours covered by one unit of the service price (see ServiceTimeUnit: shift = 12h, day = 9h).
    private static int HoursPerUnit(CalculationType type) => type switch
    {
        CalculationType.PerShift => 12,
        CalculationType.PerDay => 9,
        _ => 1   // PerHour
    };

    
    //  Record (Association supervisor / Production House location manager)
    
    public async Task<ApiResponse<WorkerOvertimeDto>> RecordOvertimeAsync(int currentUserId, RecordOvertimeDto dto, CancellationToken ct = default)
    {
        var user = await GetActiveUserAsync(currentUserId, ct);
        if (user is null) return Unauthorized<WorkerOvertimeDto>();

        if (!Permissions(user).Contains(ManagePermission))
            return Fail<WorkerOvertimeDto>("You do not have permission to record overtime.", "ليس لديك صلاحية تسجيل الوقت الإضافي.", 403);

        if (string.IsNullOrWhiteSpace(dto.QrCode))
            return Fail<WorkerOvertimeDto>("The worker's QR code is required.", "رمز QR الخاص بالعامل مطلوب.");

        if (dto.Hours < MinOvertimeHours || dto.Hours > MaxOvertimeHours || dto.Hours % 0.5m != 0)
            return Fail<WorkerOvertimeDto>($"Overtime hours must be between {MinOvertimeHours} and {MaxOvertimeHours}, in steps of 0.5.",
                $"ساعات الوقت الإضافي يجب أن تكون بين {MinOvertimeHours} و{MaxOvertimeHours} وبمضاعفات 0.5.");

        var sr = await _uow.ServiceRequests.GetByIdAsync(dto.ServiceRequestId, ct);
        if (sr is null) return Fail<WorkerOvertimeDto>("Service request was not found.", "طلب الخدمة غير موجود.", 404);

        if (sr.StatusId != LookupConstants.ServiceRequestStatus.InProgress)
            return Fail<WorkerOvertimeDto>("Overtime cannot be recorded: the service request is not in progress.", "لا يمكن تسجيل وقت إضافي: طلب الخدمة غير قيد التنفيذ.");

        // Same people who take attendance: association supervisor of the project, or the project's location manager.
        if (await ResolveSideAsync(user, sr, ct) == Side.None)
            return Fail<WorkerOvertimeDto>("You are not allowed to record overtime for this service request.", "غير مصرح لك بتسجيل الوقت الإضافي لهذا الطلب.", 403);

        // The worker is identified only by his own QR and must be assigned to this request.
        var link = await _repo.GetActiveLinkByQrAsync(dto.QrCode.Trim(), sr.Id, ct);
        if (link is null)
            return Fail<WorkerOvertimeDto>("This QR code does not belong to a worker assigned to this service request.",
                "رمز QR هذا لا يخص عاملاً مسجلاً في طلب الخدمة هذا.", 404);

        var now = DateTime.UtcNow;
        var today = (now + LocalOffset).Date;
        var date = (dto.AttendanceForDate ?? today).Date;
        if (date < sr.StartDate.Date || date > sr.EndDate.Date )
            return Fail<WorkerOvertimeDto>("The date is outside the service request period or in the future.", "التاريخ خارج فترة طلب الخدمة أو في المستقبل.");

        // Rule: overtime can only be entered AFTER the worker's check-out of that day.
        var rows = await _repo.ListByLinkAndDateAsync(link.Id, date, ct);
        var checkedOut = rows.Any(r => r.EntryTypeId == LookupConstants.AttendanceEntryType.Present
                                    && r.ApprovalStatusId != LookupConstants.AttendanceApprovalStatus.Rejected
                                    && (r.AssociationCheckOutDate != null || r.ProductionCompanyCheckOutDate != null));
        if (!checkedOut)
            return Fail<WorkerOvertimeDto>("Overtime can only be recorded after the worker's check-out for that day.",
                "لا يمكن تسجيل وقت إضافي إلا بعد تسجيل خروج العامل في ذلك اليوم.");

        // Priced like the service: one unit wage covers HoursPerUnit hours.
        var hoursPerUnit = HoursPerUnit(sr.Service.CalculationType);
        var amount = Math.Round(link.DailyWageAmount * dto.Hours / hoursPerUnit, 2, MidpointRounding.AwayFromZero);

        var overtime = await _repo.GetOvertimeAsync(link.Id, date, ct);
        if (overtime is null)
        {
            overtime = new WorkerOvertime { WorkerProjectLinkId = link.Id, OvertimeForDate = date, CreatedBy = user.UserId };
            await _repo.AddOvertimeAsync(overtime, ct);
        }
        else
        {
            if (overtime.ApprovalStatusId == LookupConstants.AttendanceApprovalStatus.Approved)
                return Fail<WorkerOvertimeDto>("Overtime for this day was already approved and can no longer be changed.", "تم اعتماد الوقت الإضافي لهذا اليوم ولا يمكن تعديله.", 409);
            overtime.UpdatedBy = user.UserId;
            overtime.UpdatedDate = now;
        }

        // New record, edit of a pending one, or re-submission of a rejected one: always goes back to Pending.
        overtime.Hours = dto.Hours;
        overtime.HoursPerUnit = hoursPerUnit;
        overtime.UnitWageSnapshot = link.DailyWageAmount;
        overtime.Amount = amount;
        overtime.RecordedByUserId = user.UserId;
        overtime.RecordedDate = now;
        overtime.ApprovalStatusId = LookupConstants.AttendanceApprovalStatus.Pending;
        overtime.ReviewedByUserId = null;
        overtime.ReviewedDate = null;
        overtime.RejectionReason = null;

        await _uow.SaveChangesAsync(ct);
        overtime = (await _repo.GetOvertimeByIdAsync(overtime.Id, ct))!;   
        return ApiResponse<WorkerOvertimeDto>.SuccessResponse(ToOvertimeDto(overtime, link),
            "Overtime recorded. It is not counted until Bayt Al Urdon approves it.", "تم تسجيل الوقت الإضافي ولن يُحتسب إلا بعد موافقة بيت الأردن.", 201);
    }

    
    //  Read
    
    public async Task<ApiResponse<List<WorkerOvertimeDto>>> GetOvertimeAsync(
        int currentUserId, int serviceRequestId, DateTime? from, DateTime? to, int? statusId, CancellationToken ct = default)
    {
        var (_, sr, error) = await LoadForViewAsync(currentUserId, serviceRequestId, ct);
        if (error is not null) return Fail<List<WorkerOvertimeDto>>(error.En!, error.Ar!, error.Status);

        var f = (from ?? sr!.StartDate).Date;
        var t = (to ?? sr!.EndDate).Date;
        var list = await _repo.ListOvertimeByServiceRequestAsync(sr!.Id, f, t, ct);
        if (statusId.HasValue) list = list.Where(o => o.ApprovalStatusId == statusId.Value).ToList();

        return ApiResponse<List<WorkerOvertimeDto>>.SuccessResponse(
            list.Select(o => ToOvertimeDto(o)).ToList(), "Overtime fetched.", "تم جلب الوقت الإضافي.");
    }

    //All overtime waiting for a decision, across every project (Bayt Al Urdon only).
    public async Task<ApiResponse<List<WorkerOvertimeDto>>> GetPendingOvertimeAsync(int currentUserId, CancellationToken ct = default)
    {
        var user = await GetActiveUserAsync(currentUserId, ct);
        if (user is null) return Unauthorized<List<WorkerOvertimeDto>>();
        if (!CanApproveOvertime(user))
            return Fail<List<WorkerOvertimeDto>>("Only Bayt Al Urdon can review overtime.", "فقط بيت الأردن يمكنه مراجعة الوقت الإضافي.", 403);

        var list = await _repo.ListOvertimeByStatusAsync(LookupConstants.AttendanceApprovalStatus.Pending, ct);
        return ApiResponse<List<WorkerOvertimeDto>>.SuccessResponse(
            list.Select(o => ToOvertimeDto(o)).ToList(), "Pending overtime fetched.", "تم جلب الوقت الإضافي المعلّق.");
    }

    
    //  Approve / Reject (Bayt Al Urdon only)
    
    public Task<ApiResponse<WorkerOvertimeDto>> ApproveOvertimeAsync(int currentUserId, int overtimeId, CancellationToken ct = default) =>
        ReviewOvertimeAsync(currentUserId, overtimeId, approve: true, reason: null, ct);

    public Task<ApiResponse<WorkerOvertimeDto>> RejectOvertimeAsync(int currentUserId, int overtimeId, string reason, CancellationToken ct = default) =>
        ReviewOvertimeAsync(currentUserId, overtimeId, approve: false, reason, ct);

    private async Task<ApiResponse<WorkerOvertimeDto>> ReviewOvertimeAsync(int currentUserId, int overtimeId, bool approve, string? reason, CancellationToken ct)
    {
        var user = await GetActiveUserAsync(currentUserId, ct);
        if (user is null) return Unauthorized<WorkerOvertimeDto>();

        if (!CanApproveOvertime(user))
            return Fail<WorkerOvertimeDto>("Only Bayt Al Urdon can approve or reject overtime.", "فقط بيت الأردن يمكنه اعتماد أو رفض الوقت الإضافي.", 403);

        if (!approve && string.IsNullOrWhiteSpace(reason))
            return Fail<WorkerOvertimeDto>("A rejection reason is required.", "سبب الرفض مطلوب.");

        var overtime = await _repo.GetOvertimeByIdAsync(overtimeId, ct);
        if (overtime is null) return Fail<WorkerOvertimeDto>("Overtime record was not found.", "سجل الوقت الإضافي غير موجود.", 404);

        if (overtime.ApprovalStatusId != LookupConstants.AttendanceApprovalStatus.Pending)
            return Fail<WorkerOvertimeDto>("This overtime was already reviewed.", "تمت مراجعة هذا الوقت الإضافي مسبقاً.");

        var now = DateTime.UtcNow;
        overtime.ApprovalStatusId = approve
                    ? LookupConstants.AttendanceApprovalStatus.Approved
                    : LookupConstants.AttendanceApprovalStatus.Rejected; overtime.RejectionReason = approve ? null : reason!.Trim();
        overtime.ReviewedByUserId = user.UserId;
        overtime.ReviewedDate = now;
        overtime.UpdatedBy = user.UserId;
        overtime.UpdatedDate = now;

        await _uow.SaveChangesAsync(ct);
        return ApiResponse<WorkerOvertimeDto>.SuccessResponse(ToOvertimeDto(overtime),
            approve ? "Overtime approved and added to the worker's total." : "Overtime rejected; it will not be counted.",
            approve ? "تم اعتماد الوقت الإضافي وإضافته إلى إجمالي العامل." : "تم رفض الوقت الإضافي ولن يُحتسب.");
    }

    private static bool CanApproveOvertime(User user) =>
        user.EntityType == EntityType.BaytAlUrdon && Permissions(user).Contains(ApproveOvertimePermission);

    private static WorkerOvertimeDto ToOvertimeDto(WorkerOvertime o, WorkerProjectLink? link = null)
    {
        var l = link ?? o.WorkerProjectLink;
        return new WorkerOvertimeDto
        {
            Id = o.Id,
            WorkerProjectLinkId = o.WorkerProjectLinkId,
            WorkerId = l?.WorkerId ?? 0,
            WorkerName = l?.Worker is null ? null : $"{l.Worker.FirstName} {l.Worker.LastName}",
            ProjectId = l?.ProjectId ?? 0,
            ServiceRequestId = l?.ServiceRequestId ?? 0,
            OvertimeForDate = o.OvertimeForDate.Date,
            Hours = o.Hours,
            HoursPerUnit = o.HoursPerUnit,
            UnitWageAmount = o.UnitWageSnapshot,
            Amount = o.Amount,
            ApprovalStatusId = o.ApprovalStatusId,                       
            ApprovalStatus = LookupConstants.AttendanceApprovalStatusCode(o.ApprovalStatusId),
            ApprovalStatusNameEn = o.ApprovalStatus?.EnglishName,
            ApprovalStatusNameAr = o.ApprovalStatus?.ArabicName,
            RecordedByUserId = o.RecordedByUserId,
            RecordedDate = o.RecordedDate,
            ReviewedByUserId = o.ReviewedByUserId,
            ReviewedDate = o.ReviewedDate,
            RejectionReason = o.RejectionReason
        };
    }
}
