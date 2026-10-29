using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maydan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkerOvertime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WorkerOvertimes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkerProjectLinkId = table.Column<int>(type: "int", nullable: false),
                    OvertimeForDate = table.Column<DateTime>(type: "date", nullable: false),
                    Hours = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    HoursPerUnit = table.Column<int>(type: "int", nullable: false),
                    UnitWageSnapshot = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RecordedByUserId = table.Column<int>(type: "int", nullable: false),
                    RecordedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovalStatus = table.Column<int>(type: "int", nullable: false),
                    ReviewedByUserId = table.Column<int>(type: "int", nullable: true),
                    ReviewedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkerOvertimes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkerOvertimes_WorkerProjectLinks_WorkerProjectLinkId",
                        column: x => x.WorkerProjectLinkId,
                        principalTable: "WorkerProjectLinks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "PermissionId", "CreatedAt", "CreatedBy", "DeletedAt", "Id", "IsActive", "IsDeleted", "Module", "PermissionNameAr", "PermissionNameEn", "UpdatedAt" },
                values: new object[] { 43, new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 0, true, false, "Attendance", "اعتماد الوقت الإضافي", "Approve Overtime", new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "PermissionId", "RoleId", "IsActive" },
                values: new object[] { 43, 1, true });

            migrationBuilder.CreateIndex(
                name: "IX_WorkerOvertimes_ApprovalStatus",
                table: "WorkerOvertimes",
                column: "ApprovalStatus");

            migrationBuilder.CreateIndex(
                name: "IX_WorkerOvertimes_WorkerProjectLinkId_OvertimeForDate",
                table: "WorkerOvertimes",
                columns: new[] { "WorkerProjectLinkId", "OvertimeForDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkerOvertimes");

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 43, 1 });

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 43);
        }
    }
}
