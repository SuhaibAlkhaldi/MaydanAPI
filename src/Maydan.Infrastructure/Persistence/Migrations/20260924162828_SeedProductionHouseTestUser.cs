using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maydan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedProductionHouseTestUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "UserId", "CreatedAt", "CreatedBy", "DeletedAt", "Email", "EntityId", "EntityType", "FirstNameAr", "FirstNameEn", "Id", "IsActive", "IsDeleted", "LastNameAr", "LastNameEn", "MustResetPassword", "PasswordHash", "PhoneNumber", "RoleId", "UpdatedAt" },
                values: new object[] { 1003, new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "production-tester@example.org", 1, "ProductionCompany", "شركة", "Production", 0, true, false, "اختبار", "Tester", false, "PBKDF2-SHA256.100000.7DqYchDowXom9IfwCgW+4g==.d41vmNOYVi3nthYMOioczS+W871M/cTKq6Ps58ISAcg=", "+962770000026", 3, new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 1003);
        }
    }
}
