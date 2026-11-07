using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maydan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddViewWorkerSuggestionRulesPermission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "PermissionId", "CreatedAt", "CreatedBy", "DeletedAt", "Id", "IsActive", "IsDeleted", "Module", "PermissionNameAr", "PermissionNameEn", "UpdatedAt" },
                values: new object[] { 46, new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 0, true, false, "WorkerSuggestions", "عرض قواعد اقتراح العمال", "View Worker Suggestion Rules", new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "PermissionId", "RoleId", "IsActive" },
                values: new object[] { 46, 2, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 46, 2 });

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 46);
        }
    }
}
