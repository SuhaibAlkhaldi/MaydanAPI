using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maydan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAssociationRoleNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 7,
                column: "RoleNameEn",
                value: "Admin-Association");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 9,
                columns: new[] { "RoleNameAr", "RoleNameEn" },
                values: new object[] { "مندوب الجمعية", "Association Representative" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 7,
                column: "RoleNameEn",
                value: "WorkerManager");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 9,
                columns: new[] { "RoleNameAr", "RoleNameEn" },
                values: new object[] { "مسؤول الحضور", "AttendanceOfficer" });
        }
    }
}
