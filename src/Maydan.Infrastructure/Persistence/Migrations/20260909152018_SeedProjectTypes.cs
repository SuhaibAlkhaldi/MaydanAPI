using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Maydan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedProjectTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "ProjectTypes",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "DeletedAt", "IsActive", "IsDeleted", "NameAr", "NameEn", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, null, null, null, true, false, "عرض واقعي", "Reality Show", null },
                    { 2, null, null, null, true, false, "فيديو موسيقي", "Music Video", null },
                    { 3, null, null, null, true, false, "إعلانات متلفزة", "Commercials", null },
                    { 4, null, null, null, true, false, "فيلم قصير", "Short Film", null },
                    { 5, null, null, null, true, false, "فيلم طويل", "Feature Film", null },
                    { 6, null, null, null, true, false, "صور متحركة", "Animation", null },
                    { 7, null, null, null, true, false, "تصوير فوتوغرافي", "Photography", null },
                    { 8, null, null, null, true, false, "برامج", "TV Program", null },
                    { 9, null, null, null, true, false, "مسلسل", "Series", null },
                    { 10, null, null, null, true, false, "ألعاب تفاعلية", "Interactive/Game", null },
                    { 11, null, null, null, true, false, "وثائقي طويل", "Feature Documentary", null },
                    { 12, null, null, null, true, false, "وثائقي قصير", "Short Documentary", null },
                    { 13, null, null, null, true, false, "مسلسل وثائقي", "Documentary Series", null },
                    { 14, null, null, null, true, false, "وثائقي صناعي/شركات", "Corporate/Industrial Documentary", null },
                    { 15, null, null, null, true, false, "إنتاج طلابي", "Student Film", null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ProjectTypes",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "ProjectTypes",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "ProjectTypes",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "ProjectTypes",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "ProjectTypes",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "ProjectTypes",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "ProjectTypes",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "ProjectTypes",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "ProjectTypes",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "ProjectTypes",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "ProjectTypes",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "ProjectTypes",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "ProjectTypes",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "ProjectTypes",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "ProjectTypes",
                keyColumn: "Id",
                keyValue: 15);
        }
    }
}
