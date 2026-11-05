using Maydan.Application.DTOs.Attendance;
using Maydan.Application.DTOs.Common;
using Maydan.Domain.Common;
using Maydan.Domain.Entities;
using Maydan.Domain.Enums;

namespace Maydan.Application.Services;

public partial class WorkerAttendanceService
{
    private const int ReminderEveryHours = 24;
    private const int ResolutionDeadlineDays = 3;   // لاحقاً ممكن تنقليهم لـ SystemConfiguration

    public async Task<ApiResponse<WorkerAttendanceDto>> ResolveAsync(int currentUserId, int attendanceId, ResolveAttendanceDto dto, CancellationToken ct = default)
    {
        var user = await GetActiveUserAsync(currentUserId, ct);
        if (user is null) return Unauthorized<WorkerAttendanceDto>();

        if (!CanResolveAttendance(user))
            return Fail<WorkerAttendanceDto>("Only Bayt Al Urdon can resolve unrecorded attendance.",
                "فقط بيت الأردن يمكنه معالجة الحضور غير المسجل.", 403);

        var note = string.IsNullOrWhiteSpace(dto.Note) ? null : dto.Note.Trim();
        if (note is { Length: > 500 })
            return Fail<WorkerAttendanceDto>("The note is too long (max 500 characters).", "الملاحظة طويلة جداً (الحد الأقصى 500 حرف).");

        var a = await _repo.GetByIdAsync(attendanceId, ct);
        if (a is null) return Fail<WorkerAttendanceDto>("Attendance record was not found.", "سجل الحضور غير موجود.", 404);

        if (a.EntryTypeId != LookupConstants.AttendanceEntryType.NotRecorded
            || a.ApprovalStatusId != LookupConstants.AttendanceApprovalStatus.Pending)
            return Fail<WorkerAttendanceDto>("This record is not waiting for resolution.", "هذا السجل غير بانتظار المعالجة.", 409);

        var now = DateTime.UtcNow;
        a.EntryTypeId = (int)dto.FinalEntryTypeId;
        a.ApprovalStatusId = LookupConstants.AttendanceApprovalStatus.Approved;
        a.ReviewedByUserId = user.UserId;
        a.ReviewedDate = now;
        a.ResolutionNote = note;
        a.RejectionReason = null;
        a.UpdatedBy = user.UserId; a.UpdatedDate = now;

        await _uow.SaveChangesAsync(ct);

        var result = ToDto(a);
        ApplyWorker(result, a.WorkerProjectLink);

        await FillLookupNamesAsync(result, ct);
        return ApiResponse<WorkerAttendanceDto>.SuccessResponse(result, "Attendance resolved.", "تمت معالجة الحضور.");
        //return ApiResponse<WorkerAttendanceDto>.SuccessResponse(ToDto(a), "Attendance resolved.", "تمت معالجة الحضور.");
    }

    public async Task<ApiResponse<List<PendingResolutionDto>>> GetPendingResolutionAsync(int currentUserId, CancellationToken ct = default)
    {
        var user = await GetActiveUserAsync(currentUserId, ct);
        if (user is null) return Unauthorized<List<PendingResolutionDto>>();
        if (!CanResolveAttendance(user))
            return Fail<List<PendingResolutionDto>>("Only Bayt Al Urdon can view unresolved attendance.", "فقط بيت الأردن يمكنه عرض الحضور غير المعالج.", 403);

        var today = (DateTime.UtcNow + LocalOffset).Date;
        var rows = await _repo.ListPendingResolutionAsync(ct);

        var items = rows.Select(r =>
        {
            var att = ToDto(r);
            att.WorkerId = r.WorkerProjectLink.WorkerId;
            att.WorkerName = r.WorkerProjectLink.Worker is null ? null : $"{r.WorkerProjectLink.Worker.FirstName} {r.WorkerProjectLink.Worker.LastName}";
            var deadline = r.AttendanceForDate.Date.AddDays(ResolutionDeadlineDays);
            return new PendingResolutionDto
            {
                Attendance = att,
                ServiceRequestId = r.WorkerProjectLink.ServiceRequestId,
                DeadlineDate = deadline,
                IsOverdue = today > deadline
            };
        }).ToList();
        await FillLookupNamesAsync(items.Select(i => i.Attendance), ct);

        return ApiResponse<List<PendingResolutionDto>>.SuccessResponse(items, "Pending attendance fetched.", "تم جلب الحضور المعلق.");
    }

    // ===== الجوب =====

    // لكل فتحة انتهت: العامل اللي أي طرف ما سجّله بيصير NotRecorded + Pending لبيت الأردن
    public async Task<int> ProcessClosedSlotsAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var windows = await _repo.ListUnprocessedClosedWindowsAsync(now, ct);
        var created = 0;

        foreach (var group in windows.GroupBy(w => w.ServiceRequestId))
        {
            var sr = await _uow.ServiceRequests.GetAttendanceInfoAsync(group.Key, ct);
            if (sr is null) continue;

            var links = (await _repo.ListLinksByServiceRequestAsync(sr.Id, ct)).Where(l => l.IsWorkerActive).ToList();

            foreach (var w in group)
            {
                var day = w.AttendanceDate.Date;
                var rows = (await _repo.ListBySlotAsync(sr.Id, day, w.SlotNumber, ct)).ToDictionary(r => r.WorkerProjectLinkId);

                foreach (var link in links)
                {
                    if ((link.CreatedDate + LocalOffset).Date > day) continue;   // العامل انضاف بعد هذا اليوم
                    rows.TryGetValue(link.Id, out var row);

                    var missAssoc = row?.AssociationEntryTypeId is null;
                    var missProd = row?.ProductionCompanyEntryTypeId is null;
                    if (!missAssoc && !missProd) continue;                         // الطرفين سجّلوا: شركة الإنتاج بتراجع
                    if (row is not null && row.ApprovalStatusId != LookupConstants.AttendanceApprovalStatus.Pending) continue;

                    if (row is null)
                    {
                        row = new WorkerAttendance
                        {
                            WorkerProjectLinkId = link.Id,
                            AttendanceForDate = day,
                            CheckInNumber = w.SlotNumber,
                            CityId = sr.CityId,
                            ApprovalStatusId = LookupConstants.AttendanceApprovalStatus.Pending
                        };
                        await _repo.AddAsync(row, ct);
                    }

                    row.EntryTypeId = LookupConstants.AttendanceEntryType.NotRecorded;
                    row.NotRecordedReason = missAssoc && missProd ? "ممثل الجمعية وممثل شركة الإنتاج لم يحضرا"
                                          : missAssoc ? "ممثل الجمعية لم يحضر"
                                          : "ممثل شركة الإنتاج لم يحضر";
                    row.LastReminderDate = now;
                    created++;
                }

                w.UnrecordedProcessedDate = now;
                await _uow.SaveChangesAsync(ct);   // فتحة فتحة، عشان لو صار خطأ ما نخسر اللي خلص
            }
        }

        if (created > 0)
            await NotifyBaytAlUrdonAsync("Attendance needs your decision | حضور بحاجة لقرارك",
                $"<p>{created} attendance record(s) were not recorded by one or both representatives and wait for your decision. Wages stay on hold until they are resolved.</p>" +
                $"<p>يوجد {created} سجل حضور لم يسجّله أحد الممثلين أو كلاهما وينتظر قرارك. لن تُحتسب الأجور قبل معالجتها.</p>", ct);

        return created;
    }

    public async Task<int> SendUnresolvedRemindersAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var due = await _repo.ListRemindableAsync(now.AddHours(-ReminderEveryHours), ct);
        if (due.Count == 0) return 0;

        var today = (now + LocalOffset).Date;
        var overdue = due.Count(a => today > a.AttendanceForDate.Date.AddDays(ResolutionDeadlineDays));

        foreach (var a in due) a.LastReminderDate = now;
        await _uow.SaveChangesAsync(ct);

        await NotifyBaytAlUrdonAsync(
            overdue > 0 ? "OVERDUE: unresolved attendance | حضور متأخر المعالجة" : "Reminder: unresolved attendance | تذكير بحضور معلق",
            $"<p>{due.Count} attendance record(s) still wait for your decision ({overdue} past the {ResolutionDeadlineDays}-day deadline). Wages cannot be calculated until they are resolved.</p>" +
            $"<p>{due.Count} سجل حضور ما زال بانتظار قرارك ({overdue} تجاوز مهلة {ResolutionDeadlineDays} أيام). لا يمكن احتساب الأجور قبل المعالجة.</p>", ct);

        return due.Count;
    }

    private async Task NotifyBaytAlUrdonAsync(string subject, string bodyHtml, CancellationToken ct)
    {
        var users = await _uow.Users.GetByEntityAsync(EntityType.BaytAlUrdon, 1, null, ct);   // EntityId لبيت الأردن = 1
        foreach (var u in users)
        {
            var full = await _uow.Users.GetWithPermissionsAsync(u.UserId, ct);
            if (full is not { IsActive: true } || !CanResolveAttendance(full) || string.IsNullOrWhiteSpace(full.Email)) continue;
            try { await _email.SendAsync(full.Email, subject, bodyHtml, ct); }
            catch { /* فشل الإيميل ما بوقف المعالجة */ }
        }
    }
}