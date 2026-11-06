using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maydan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSideEntryTypesToAttendance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkerAttendances_EntryTypeId",
                table: "WorkerAttendances");

            migrationBuilder.AddColumn<int>(
                name: "AssociationEntryTypeId",
                table: "WorkerAttendances",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssociationReason",
                table: "WorkerAttendances",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AssociationRecordedDate",
                table: "WorkerAttendances",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastReminderDate",
                table: "WorkerAttendances",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NotRecordedReason",
                table: "WorkerAttendances",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProductionCompanyEntryTypeId",
                table: "WorkerAttendances",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductionCompanyReason",
                table: "WorkerAttendances",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProductionCompanyRecordedDate",
                table: "WorkerAttendances",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResolutionNote",
                table: "WorkerAttendances",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UnrecordedProcessedDate",
                table: "AttendanceTimeWindows",
                type: "datetime2",
                nullable: true);

            migrationBuilder.InsertData(
                table: "LookupItems",
                columns: new[] { "Id", "ArabicName", "CategoryId", "Code", "CreatedAt", "CreatedBy", "DeletedAt", "EnglishName", "IsActive", "IsDeleted", "SortOrder", "UpdatedAt" },
                values: new object[] { 17, "لم يُسجَّل", 5, "NotRecorded", null, null, null, "Not Recorded", true, false, 3, null });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "PermissionId", "CreatedAt", "CreatedBy", "DeletedAt", "Id", "IsActive", "IsDeleted", "Module", "PermissionNameAr", "PermissionNameEn", "UpdatedAt" },
                values: new object[] { 44, new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 0, true, false, "Attendance", "معالجة حالات الحضور غير المسجلة", "Resolve Attendance", new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "PermissionId", "RoleId", "IsActive" },
                values: new object[] { 44, 1, true });

            migrationBuilder.CreateIndex(
                name: "IX_WorkerAttendances_AssociationEntryTypeId",
                table: "WorkerAttendances",
                column: "AssociationEntryTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkerAttendances_EntryTypeId_ApprovalStatusId",
                table: "WorkerAttendances",
                columns: new[] { "EntryTypeId", "ApprovalStatusId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkerAttendances_ProductionCompanyEntryTypeId",
                table: "WorkerAttendances",
                column: "ProductionCompanyEntryTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceTimeWindows_UnrecordedProcessedDate_ClosesAtUtc",
                table: "AttendanceTimeWindows",
                columns: new[] { "UnrecordedProcessedDate", "ClosesAtUtc" });

            migrationBuilder.AddForeignKey(
                name: "FK_WorkerAttendances_LookupItems_AssociationEntryTypeId",
                table: "WorkerAttendances",
                column: "AssociationEntryTypeId",
                principalTable: "LookupItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkerAttendances_LookupItems_ProductionCompanyEntryTypeId",
                table: "WorkerAttendances",
                column: "ProductionCompanyEntryTypeId",
                principalTable: "LookupItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
            migrationBuilder.Sql(@"
    UPDATE WorkerAttendances
    SET AssociationEntryTypeId = EntryTypeId,
        AssociationRecordedDate = AssociationCheckInDate
    WHERE AssociationCheckInDate IS NOT NULL;

    UPDATE WorkerAttendances
    SET ProductionCompanyEntryTypeId = EntryTypeId,
        ProductionCompanyRecordedDate = ProductionCompanyCheckInDate
    WHERE ProductionCompanyCheckInDate IS NOT NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkerAttendances_LookupItems_AssociationEntryTypeId",
                table: "WorkerAttendances");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkerAttendances_LookupItems_ProductionCompanyEntryTypeId",
                table: "WorkerAttendances");

            migrationBuilder.DropIndex(
                name: "IX_WorkerAttendances_AssociationEntryTypeId",
                table: "WorkerAttendances");

            migrationBuilder.DropIndex(
                name: "IX_WorkerAttendances_EntryTypeId_ApprovalStatusId",
                table: "WorkerAttendances");

            migrationBuilder.DropIndex(
                name: "IX_WorkerAttendances_ProductionCompanyEntryTypeId",
                table: "WorkerAttendances");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceTimeWindows_UnrecordedProcessedDate_ClosesAtUtc",
                table: "AttendanceTimeWindows");

            migrationBuilder.DeleteData(
                table: "LookupItems",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 44, 1 });

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 44);

            migrationBuilder.DropColumn(
                name: "AssociationEntryTypeId",
                table: "WorkerAttendances");

            migrationBuilder.DropColumn(
                name: "AssociationReason",
                table: "WorkerAttendances");

            migrationBuilder.DropColumn(
                name: "AssociationRecordedDate",
                table: "WorkerAttendances");

            migrationBuilder.DropColumn(
                name: "LastReminderDate",
                table: "WorkerAttendances");

            migrationBuilder.DropColumn(
                name: "NotRecordedReason",
                table: "WorkerAttendances");

            migrationBuilder.DropColumn(
                name: "ProductionCompanyEntryTypeId",
                table: "WorkerAttendances");

            migrationBuilder.DropColumn(
                name: "ProductionCompanyReason",
                table: "WorkerAttendances");

            migrationBuilder.DropColumn(
                name: "ProductionCompanyRecordedDate",
                table: "WorkerAttendances");

            migrationBuilder.DropColumn(
                name: "ResolutionNote",
                table: "WorkerAttendances");

            migrationBuilder.DropColumn(
                name: "UnrecordedProcessedDate",
                table: "AttendanceTimeWindows");

            migrationBuilder.CreateIndex(
                name: "IX_WorkerAttendances_EntryTypeId",
                table: "WorkerAttendances",
                column: "EntryTypeId");
        }
    }
}
