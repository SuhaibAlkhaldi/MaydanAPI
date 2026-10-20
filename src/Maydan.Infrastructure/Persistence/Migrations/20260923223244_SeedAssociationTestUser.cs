using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maydan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedAssociationTestUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "UserId", "CreatedAt", "CreatedBy", "DeletedAt", "Email", "EntityId", "EntityType", "FirstNameAr", "FirstNameEn", "Id", "IsActive", "IsDeleted", "LastNameAr", "LastNameEn", "MustResetPassword", "PasswordHash", "PhoneNumber", "RoleId", "UpdatedAt" },
                values: new object[] { 1001, new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "association-tester@example.org", 1, "Association", "جمعية", "Association", 0, true, false, "اختبار", "Tester", false, "PBKDF2-SHA256.100000.Neiqo3DL9KA6VnPDZZgaWQ==.euR5zQ/Skjf64sJn7SnvR/IDgkd5c/CgJLLfx9HjaXU=", "+962770000025", 4, new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 1001);
        }
    }
}
