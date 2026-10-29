using Maydan.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Maydan.Infrastructure.Persistence.Seed;

public static class LookupSeed
{
    public const int GenderCategoryId = 1;
    public const int MaritalStatusCategoryId = 2;

    public const int GenderMaleItemId = 1;
    public const int GenderFemaleItemId = 2;

    public const int MaritalStatusSingleItemId = 3;
    public const int MaritalStatusMarriedItemId = 4;
    public const int MaritalStatusDivorcedItemId = 5;
    public const int MaritalStatusWidowedItemId = 6;

    public const int ServiceRequestStatusCategoryId = 3;
    public const int AttendanceApprovalStatusCategoryId = 4;
    public const int AttendanceEntryTypeCategoryId = 5;
    // Service Request Status (7-11)
    public const int ServiceRequestStatusPendingWorkerSelectionItemId = 7;
    public const int ServiceRequestStatusInProgressItemId = 8;
    public const int ServiceRequestStatusCompletedItemId = 9;
    public const int ServiceRequestStatusCancelledItemId = 10;
    public const int ServiceRequestStatusRejectedItemId = 11;
    // Attendance Approval Status (12-14)
    public const int AttendanceApprovalStatusPendingItemId = 12;
    public const int AttendanceApprovalStatusApprovedItemId = 13;
    public const int AttendanceApprovalStatusRejectedItemId = 14;
    // Attendance Entry Type (15-16)
    public const int AttendanceEntryTypePresentItemId = 15;
    public const int AttendanceEntryTypeAbsentItemId = 16;
    public static void Seed(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LookupCategory>().HasData(
            new LookupCategory { Id = GenderCategoryId, Code = "Gender", ArabicName = "الجنس", EnglishName = "Gender", IsActive = true, IsDeleted = false },
            new LookupCategory { Id = MaritalStatusCategoryId, Code = "MaritalStatus", ArabicName = "الحالة الاجتماعية", EnglishName = "Marital Status", IsActive = true, IsDeleted = false },
            new LookupCategory { Id = ServiceRequestStatusCategoryId, Code = "ServiceRequestStatus", ArabicName = "حالة طلب الخدمة", EnglishName = "Service Request Status", IsActive = true, IsDeleted = false },
            new LookupCategory { Id = AttendanceApprovalStatusCategoryId, Code = "AttendanceApprovalStatus", ArabicName = "حالة اعتماد الحضور", EnglishName = "Attendance Approval Status", IsActive = true, IsDeleted = false },
            new LookupCategory { Id = AttendanceEntryTypeCategoryId, Code = "AttendanceEntryType", ArabicName = "نوع تسجيل الحضور", EnglishName = "Attendance Entry Type", IsActive = true, IsDeleted = false }

         );

        modelBuilder.Entity<LookupItem>().HasData(
            new LookupItem { Id = GenderMaleItemId, CategoryId = GenderCategoryId, Code = "Male", ArabicName = "ذكر", EnglishName = "Male", SortOrder = 1, IsActive = true, IsDeleted = false },
            new LookupItem { Id = GenderFemaleItemId, CategoryId = GenderCategoryId, Code = "Female", ArabicName = "أنثى", EnglishName = "Female", SortOrder = 2, IsActive = true, IsDeleted = false },

            new LookupItem { Id = MaritalStatusSingleItemId, CategoryId = MaritalStatusCategoryId, Code = "Single", ArabicName = "أعزب", EnglishName = "Single", SortOrder = 1, IsActive = true, IsDeleted = false },
            new LookupItem { Id = MaritalStatusMarriedItemId, CategoryId = MaritalStatusCategoryId, Code = "Married", ArabicName = "متزوج", EnglishName = "Married", SortOrder = 2, IsActive = true, IsDeleted = false },
            new LookupItem { Id = MaritalStatusDivorcedItemId, CategoryId = MaritalStatusCategoryId, Code = "Divorced", ArabicName = "مطلق", EnglishName = "Divorced", SortOrder = 3, IsActive = true, IsDeleted = false },
            new LookupItem { Id = MaritalStatusWidowedItemId, CategoryId = MaritalStatusCategoryId, Code = "Widowed", ArabicName = "أرمل", EnglishName = "Widowed", SortOrder = 4, IsActive = true, IsDeleted = false },
            // Service Request Status
            new LookupItem { Id = ServiceRequestStatusPendingWorkerSelectionItemId, CategoryId = ServiceRequestStatusCategoryId, Code = "PendingWorkerSelection", ArabicName = "بانتظار ترشيح العمال", EnglishName = "Pending Worker Selection", SortOrder = 1, IsActive = true, IsDeleted = false },
            new LookupItem { Id = ServiceRequestStatusInProgressItemId, CategoryId = ServiceRequestStatusCategoryId, Code = "InProgress", ArabicName = "قيد التنفيذ", EnglishName = "In Progress", SortOrder = 2, IsActive = true, IsDeleted = false },
            new LookupItem { Id = ServiceRequestStatusCompletedItemId, CategoryId = ServiceRequestStatusCategoryId, Code = "Completed", ArabicName = "مكتمل", EnglishName = "Completed", SortOrder = 3, IsActive = true, IsDeleted = false },
            new LookupItem { Id = ServiceRequestStatusCancelledItemId, CategoryId = ServiceRequestStatusCategoryId, Code = "Cancelled", ArabicName = "ملغي", EnglishName = "Cancelled", SortOrder = 4, IsActive = true, IsDeleted = false },
            new LookupItem { Id = ServiceRequestStatusRejectedItemId, CategoryId = ServiceRequestStatusCategoryId, Code = "Rejected", ArabicName = "مرفوض", EnglishName = "Rejected", SortOrder = 5, IsActive = true, IsDeleted = false },
            // Attendance Approval Status
            new LookupItem { Id = AttendanceApprovalStatusPendingItemId, CategoryId = AttendanceApprovalStatusCategoryId, Code = "Pending", ArabicName = "قيد المراجعة", EnglishName = "Pending", SortOrder = 1, IsActive = true, IsDeleted = false },
            new LookupItem { Id = AttendanceApprovalStatusApprovedItemId, CategoryId = AttendanceApprovalStatusCategoryId, Code = "Approved", ArabicName = "معتمد", EnglishName = "Approved", SortOrder = 2, IsActive = true, IsDeleted = false },
            new LookupItem { Id = AttendanceApprovalStatusRejectedItemId, CategoryId = AttendanceApprovalStatusCategoryId, Code = "Rejected", ArabicName = "مرفوض", EnglishName = "Rejected", SortOrder = 3, IsActive = true, IsDeleted = false },
            // Attendance Entry Type
            new LookupItem { Id = AttendanceEntryTypePresentItemId, CategoryId = AttendanceEntryTypeCategoryId, Code = "Present", ArabicName = "حاضر", EnglishName = "Present", SortOrder = 1, IsActive = true, IsDeleted = false },
            new LookupItem { Id = AttendanceEntryTypeAbsentItemId, CategoryId = AttendanceEntryTypeCategoryId, Code = "Absent", ArabicName = "غائب", EnglishName = "Absent", SortOrder = 2, IsActive = true, IsDeleted = false }
        );
    }
}
