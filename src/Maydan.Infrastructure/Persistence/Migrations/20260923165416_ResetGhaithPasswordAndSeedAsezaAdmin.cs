using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maydan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ResetGhaithPasswordAndSeedAsezaAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 1,
                column: "PasswordHash",
                value: "PBKDF2-SHA256.100000.kr2Do1moATCbhMGahUAANg==.2I1SQILgcLQCpWA+3XSAEXhoilqTklMJh0t7BV2LKxo=");

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "UserId", "CreatedAt", "CreatedBy", "DeletedAt", "Email", "EntityId", "EntityType", "FirstNameAr", "FirstNameEn", "Id", "IsActive", "IsDeleted", "LastNameAr", "LastNameEn", "MustResetPassword", "PasswordHash", "PhoneNumber", "RoleId", "UpdatedAt" },
                values: new object[] { 1000, new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "aseza-admin@aseza.jo", 1, "Aseza", "أسيزا", "Aseza", 0, true, false, "أدمن", "Admin", false, "PBKDF2-SHA256.100000.NMQd4g85bDIwAs//rBeXvw==.haZXxr9bl/ZmbtsVxJH5pBI2Tz5x42PyO004Wsuv3bo=", "+962770000024", 2, new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 1000);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 1,
                column: "PasswordHash",
                value: "PBKDF2-SHA256.100000.Yil011Rar6X/CCTuFMwT7w==.ClYpfJnPTPSooq0QEiIiMwl0qlPe5xp3LPkjYjNcX10=");
        }
    }
}
