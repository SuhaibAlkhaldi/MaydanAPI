using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maydan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkerAttendance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AttendanceTimeWindows",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServiceRequestId = table.Column<int>(type: "int", nullable: false),
                    AttendanceDate = table.Column<DateTime>(type: "date", nullable: false),
                    OpensAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClosesAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceTimeWindows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttendanceTimeWindows_ServiceRequests_ServiceRequestId",
                        column: x => x.ServiceRequestId,
                        principalTable: "ServiceRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkerAttendances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkerProjectLinkId = table.Column<int>(type: "int", nullable: false),
                    AttendanceForDate = table.Column<DateTime>(type: "date", nullable: false),
                    CheckInNumber = table.Column<int>(type: "int", nullable: false),
                    EntryType = table.Column<int>(type: "int", nullable: false),
                    CityId = table.Column<int>(type: "int", nullable: false),
                    AssociationUserId = table.Column<int>(type: "int", nullable: true),
                    AssociationCheckInDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AssociationCheckOutDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProductionCompanyUserId = table.Column<int>(type: "int", nullable: true),
                    ProductionCompanyCheckInDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProductionCompanyCheckOutDate = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_WorkerAttendances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkerAttendances_Cities_CityId",
                        column: x => x.CityId,
                        principalTable: "Cities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkerAttendances_WorkerProjectLinks_WorkerProjectLinkId",
                        column: x => x.WorkerProjectLinkId,
                        principalTable: "WorkerProjectLinks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceTimeWindows_ServiceRequestId_AttendanceDate",
                table: "AttendanceTimeWindows",
                columns: new[] { "ServiceRequestId", "AttendanceDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkerAttendances_AttendanceForDate",
                table: "WorkerAttendances",
                column: "AttendanceForDate");

            migrationBuilder.CreateIndex(
                name: "IX_WorkerAttendances_CityId",
                table: "WorkerAttendances",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkerAttendances_WorkerProjectLinkId_AttendanceForDate_CheckInNumber",
                table: "WorkerAttendances",
                columns: new[] { "WorkerProjectLinkId", "AttendanceForDate", "CheckInNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttendanceTimeWindows");

            migrationBuilder.DropTable(
                name: "WorkerAttendances");
        }
    }
}
