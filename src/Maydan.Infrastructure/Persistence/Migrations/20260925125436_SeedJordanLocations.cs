using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Maydan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedJordanLocations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Countries",
                columns: new[] { "Id", "ArabicName", "CreatedAt", "CreatedBy", "DeletedAt", "EnglishName", "IsActive", "IsDeleted", "UpdatedAt" },
                values: new object[] { 1, "الأردن", null, null, null, "Jordan", true, false, null });

            migrationBuilder.InsertData(
                table: "Cities",
                columns: new[] { "Id", "ArabicName", "CountryId", "CreatedAt", "CreatedBy", "DeletedAt", "EnglishName", "IsActive", "IsDeleted", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "عمان", 1, null, null, null, "Amman", true, false, null },
                    { 2, "إربد", 1, null, null, null, "Irbid", true, false, null },
                    { 3, "الزرقاء", 1, null, null, null, "Zarqa", true, false, null },
                    { 4, "البلقاء", 1, null, null, null, "Balqa", true, false, null },
                    { 5, "مأدبا", 1, null, null, null, "Madaba", true, false, null },
                    { 6, "الكرك", 1, null, null, null, "Karak", true, false, null },
                    { 7, "الطفيلة", 1, null, null, null, "Tafilah", true, false, null },
                    { 8, "معان", 1, null, null, null, "Ma'an", true, false, null },
                    { 9, "العقبة", 1, null, null, null, "Aqaba", true, false, null },
                    { 10, "جرش", 1, null, null, null, "Jerash", true, false, null },
                    { 11, "عجلون", 1, null, null, null, "Ajloun", true, false, null },
                    { 12, "المفرق", 1, null, null, null, "Mafraq", true, false, null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "Countries",
                keyColumn: "Id",
                keyValue: 1);
        }
    }
}
