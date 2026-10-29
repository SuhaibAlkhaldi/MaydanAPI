using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Maydan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ServiceRequestAndAttendanceLookups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
       name: "IX_WorkerOvertimes_ApprovalStatus",
       table: "WorkerOvertimes");

            // Rename (keeps the old values) instead of drop + add
            migrationBuilder.RenameColumn(name: "Status", table: "ServiceRequests", newName: "StatusId");
            migrationBuilder.RenameColumn(name: "EntryType", table: "WorkerAttendances", newName: "EntryTypeId");
            migrationBuilder.RenameColumn(name: "ApprovalStatus", table: "WorkerAttendances", newName: "ApprovalStatusId");
            migrationBuilder.RenameColumn(name: "ApprovalStatus", table: "WorkerOvertimes", newName: "ApprovalStatusId");



            migrationBuilder.InsertData(
                table: "LookupCategories",
                columns: new[] { "Id", "ArabicName", "Code", "CreatedAt", "CreatedBy", "DeletedAt", "EnglishName", "IsActive", "IsDeleted", "UpdatedAt" },
                values: new object[,]
                {
                    { 3, "حالة طلب الخدمة", "ServiceRequestStatus", null, null, null, "Service Request Status", true, false, null },
                    { 4, "حالة اعتماد الحضور", "AttendanceApprovalStatus", null, null, null, "Attendance Approval Status", true, false, null },
                    { 5, "نوع تسجيل الحضور", "AttendanceEntryType", null, null, null, "Attendance Entry Type", true, false, null }
                });

            migrationBuilder.InsertData(
                table: "LookupItems",
                columns: new[] { "Id", "ArabicName", "CategoryId", "Code", "CreatedAt", "CreatedBy", "DeletedAt", "EnglishName", "IsActive", "IsDeleted", "SortOrder", "UpdatedAt" },
                values: new object[,]
                {
                    { 7, "بانتظار ترشيح العمال", 3, "PendingWorkerSelection", null, null, null, "Pending Worker Selection", true, false, 1, null },
                    { 8, "قيد التنفيذ", 3, "InProgress", null, null, null, "In Progress", true, false, 2, null },
                    { 9, "مكتمل", 3, "Completed", null, null, null, "Completed", true, false, 3, null },
                    { 10, "ملغي", 3, "Cancelled", null, null, null, "Cancelled", true, false, 4, null },
                    { 11, "مرفوض", 3, "Rejected", null, null, null, "Rejected", true, false, 5, null },
                    { 12, "قيد المراجعة", 4, "Pending", null, null, null, "Pending", true, false, 1, null },
                    { 13, "معتمد", 4, "Approved", null, null, null, "Approved", true, false, 2, null },
                    { 14, "مرفوض", 4, "Rejected", null, null, null, "Rejected", true, false, 3, null },
                    { 15, "حاضر", 5, "Present", null, null, null, "Present", true, false, 1, null },
                    { 16, "غائب", 5, "Absent", null, null, null, "Absent", true, false, 2, null }
                });

            // Old enum values -> new lookup ids (must run after InsertData and before the foreign keys)
            migrationBuilder.Sql("UPDATE ServiceRequests SET StatusId = StatusId + 6;");        // 1..5  -> 7..11
            migrationBuilder.Sql("UPDATE WorkerAttendances SET ApprovalStatusId = ApprovalStatusId + 11, EntryTypeId = EntryTypeId + 14;"); // 1..3 -> 12..14, 1..2 -> 15..16
            migrationBuilder.Sql("UPDATE WorkerOvertimes SET ApprovalStatusId = ApprovalStatusId + 11;");

            // Defaults declared in the model configuration
            migrationBuilder.AlterColumn<int>(name: "StatusId", table: "ServiceRequests", type: "int", nullable: false, defaultValue: 7, oldClrType: typeof(int), oldType: "int");
            migrationBuilder.AlterColumn<int>(name: "EntryTypeId", table: "WorkerAttendances", type: "int", nullable: false, defaultValue: 15, oldClrType: typeof(int), oldType: "int");
            migrationBuilder.AlterColumn<int>(name: "ApprovalStatusId", table: "WorkerAttendances", type: "int", nullable: false, defaultValue: 12, oldClrType: typeof(int), oldType: "int");
            migrationBuilder.AlterColumn<int>(name: "ApprovalStatusId", table: "WorkerOvertimes", type: "int", nullable: false, defaultValue: 12, oldClrType: typeof(int), oldType: "int");

            // Same indexes and foreign keys as generated (unchanged)
            migrationBuilder.CreateIndex(name: "IX_WorkerOvertimes_ApprovalStatusId", table: "WorkerOvertimes", column: "ApprovalStatusId");
            migrationBuilder.CreateIndex(name: "IX_WorkerAttendances_ApprovalStatusId", table: "WorkerAttendances", column: "ApprovalStatusId");
            migrationBuilder.CreateIndex(name: "IX_WorkerAttendances_EntryTypeId", table: "WorkerAttendances", column: "EntryTypeId");
            migrationBuilder.CreateIndex(name: "IX_ServiceRequests_StatusId", table: "ServiceRequests", column: "StatusId");

            migrationBuilder.AddForeignKey(name: "FK_ServiceRequests_LookupItems_StatusId", table: "ServiceRequests", column: "StatusId", principalTable: "LookupItems", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey(name: "FK_WorkerAttendances_LookupItems_ApprovalStatusId", table: "WorkerAttendances", column: "ApprovalStatusId", principalTable: "LookupItems", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey(name: "FK_WorkerAttendances_LookupItems_EntryTypeId", table: "WorkerAttendances", column: "EntryTypeId", principalTable: "LookupItems", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey(name: "FK_WorkerOvertimes_LookupItems_ApprovalStatusId", table: "WorkerOvertimes", column: "ApprovalStatusId", principalTable: "LookupItems", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        
            migrationBuilder.DropForeignKey(name: "FK_ServiceRequests_LookupItems_StatusId", table: "ServiceRequests");
            migrationBuilder.DropForeignKey(name: "FK_WorkerAttendances_LookupItems_ApprovalStatusId", table: "WorkerAttendances");
            migrationBuilder.DropForeignKey(name: "FK_WorkerAttendances_LookupItems_EntryTypeId", table: "WorkerAttendances");
            migrationBuilder.DropForeignKey(name: "FK_WorkerOvertimes_LookupItems_ApprovalStatusId", table: "WorkerOvertimes");

            migrationBuilder.DropIndex(name: "IX_WorkerOvertimes_ApprovalStatusId", table: "WorkerOvertimes");
            migrationBuilder.DropIndex(name: "IX_WorkerAttendances_ApprovalStatusId", table: "WorkerAttendances");
            migrationBuilder.DropIndex(name: "IX_WorkerAttendances_EntryTypeId", table: "WorkerAttendances");
            migrationBuilder.DropIndex(name: "IX_ServiceRequests_StatusId", table: "ServiceRequests");

            // lookup ids -> old enum values
            migrationBuilder.Sql("UPDATE ServiceRequests SET StatusId = StatusId - 6;");
            migrationBuilder.Sql("UPDATE WorkerAttendances SET ApprovalStatusId = ApprovalStatusId - 11, EntryTypeId = EntryTypeId - 14;");
            migrationBuilder.Sql("UPDATE WorkerOvertimes SET ApprovalStatusId = ApprovalStatusId - 11;");

            migrationBuilder.DeleteData(table: "LookupItems", keyColumn: "Id", keyValues: new object[] { 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 });
            migrationBuilder.DeleteData(table: "LookupCategories", keyColumn: "Id", keyValues: new object[] { 3, 4, 5 });

            // remove the defaults added in Up
            migrationBuilder.AlterColumn<int>(name: "StatusId", table: "ServiceRequests", type: "int", nullable: false, oldClrType: typeof(int), oldType: "int", oldDefaultValue: 7);
            migrationBuilder.AlterColumn<int>(name: "EntryTypeId", table: "WorkerAttendances", type: "int", nullable: false, oldClrType: typeof(int), oldType: "int", oldDefaultValue: 15);
            migrationBuilder.AlterColumn<int>(name: "ApprovalStatusId", table: "WorkerAttendances", type: "int", nullable: false, oldClrType: typeof(int), oldType: "int", oldDefaultValue: 12);
            migrationBuilder.AlterColumn<int>(name: "ApprovalStatusId", table: "WorkerOvertimes", type: "int", nullable: false, oldClrType: typeof(int), oldType: "int", oldDefaultValue: 12);

            migrationBuilder.RenameColumn(name: "StatusId", table: "ServiceRequests", newName: "Status");
            migrationBuilder.RenameColumn(name: "EntryTypeId", table: "WorkerAttendances", newName: "EntryType");
            migrationBuilder.RenameColumn(name: "ApprovalStatusId", table: "WorkerAttendances", newName: "ApprovalStatus");
            migrationBuilder.RenameColumn(name: "ApprovalStatusId", table: "WorkerOvertimes", newName: "ApprovalStatus");

            migrationBuilder.CreateIndex(name: "IX_WorkerOvertimes_ApprovalStatus", table: "WorkerOvertimes", column: "ApprovalStatus");
        }
    }
    
}
