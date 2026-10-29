using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maydan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSlotNumberToTimeWindows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkerProjectLinks_ProjectId_WorkerId",
                table: "WorkerProjectLinks");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceTimeWindows_ServiceRequestId_AttendanceDate",
                table: "AttendanceTimeWindows");

            migrationBuilder.AddColumn<int>(
                name: "SlotNumber",
                table: "AttendanceTimeWindows",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_WorkerProjectLinks_ProjectId_WorkerId_ServiceRequestId",
                table: "WorkerProjectLinks",
                columns: new[] { "ProjectId", "WorkerId", "ServiceRequestId" },
                unique: true,
                filter: "[IsWorkerActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceTimeWindows_ServiceRequestId_AttendanceDate_SlotNumber",
                table: "AttendanceTimeWindows",
                columns: new[] { "ServiceRequestId", "AttendanceDate", "SlotNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkerProjectLinks_ProjectId_WorkerId_ServiceRequestId",
                table: "WorkerProjectLinks");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceTimeWindows_ServiceRequestId_AttendanceDate_SlotNumber",
                table: "AttendanceTimeWindows");

            migrationBuilder.DropColumn(
                name: "SlotNumber",
                table: "AttendanceTimeWindows");

            migrationBuilder.CreateIndex(
                name: "IX_WorkerProjectLinks_ProjectId_WorkerId",
                table: "WorkerProjectLinks",
                columns: new[] { "ProjectId", "WorkerId" },
                unique: true,
                filter: "[IsWorkerActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceTimeWindows_ServiceRequestId_AttendanceDate",
                table: "AttendanceTimeWindows",
                columns: new[] { "ServiceRequestId", "AttendanceDate" },
                unique: true);
        }
    }
}
