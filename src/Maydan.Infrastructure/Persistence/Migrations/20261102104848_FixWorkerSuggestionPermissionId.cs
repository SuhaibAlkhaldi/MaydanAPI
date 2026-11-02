
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maydan.Infrastructure.Persistence.Migrations
{
    public partial class FixWorkerSuggestionPermissionId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add Worker Suggestion permission (45)
            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[]
                {
                    "PermissionId",
                    "CreatedAt",
                    "CreatedBy",
                    "DeletedAt",
                    "Id",
                    "IsActive",
                    "IsDeleted",
                    "Module",
                    "PermissionNameAr",
                    "PermissionNameEn",
                    "UpdatedAt"
                },
                values: new object[]
                {
                    45,
                    new DateTime(2026, 9, 7, 0, 0, 0, DateTimeKind.Utc),
                    null,
                    null,
                    0,
                    true,
                    false,
                    "WorkerSuggestions",
                    "إدارة قواعد اقتراح العمال",
                    "Manage Worker Suggestion Rules",
                    new DateTime(2026, 9, 7, 0, 0, 0, DateTimeKind.Utc)
                });

            // Grant permission 45 to Bayt Al-Urdon
            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[]
                {
                    "PermissionId",
                    "RoleId",
                    "IsActive"
                },
                values: new object[]
                {
                    45,
                    1,
                    true
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove only Worker Suggestion permission grant
            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[]
                {
                    "PermissionId",
                    "RoleId"
                },
                keyValues: new object[]
                {
                    45,
                    1
                });

            // Remove only Worker Suggestion permission
            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 45);
        }
    }
}
