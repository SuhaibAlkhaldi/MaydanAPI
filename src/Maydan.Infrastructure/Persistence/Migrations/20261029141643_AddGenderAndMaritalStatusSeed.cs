using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Maydan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGenderAndMaritalStatusSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "LookupCategories",
                columns: new[] { "Id", "ArabicName", "Code", "CreatedAt", "CreatedBy", "DeletedAt", "EnglishName", "IsActive", "IsDeleted", "UpdatedAt" },
                values: new object[,]
                {
                    { 1006, "الجنس", "Gender", null, null, null, "Gender", true, false, null },
                    { 1007, "الحالة الاجتماعية", "MaritalStatus", null, null, null, "Marital Status", true, false, null }
                });

            migrationBuilder.InsertData(
                table: "LookupItems",
                columns: new[] { "Id", "ArabicName", "CategoryId", "Code", "CreatedAt", "CreatedBy", "DeletedAt", "EnglishName", "IsActive", "IsDeleted", "SortOrder", "UpdatedAt" },
                values: new object[,]
                {
                    { 10021, "ذكر", 1006, "Male", null, null, null, "Male", true, false, 1, null },
                    { 10022, "أنثى", 1006, "Female", null, null, null, "Female", true, false, 2, null },
                    { 10023, "أعزب", 1007, "Single", null, null, null, "Single", true, false, 1, null },
                    { 10024, "متزوج", 1007, "Married", null, null, null, "Married", true, false, 2, null },
                    { 10025, "مطلق", 1007, "Divorced", null, null, null, "Divorced", true, false, 3, null },
                    { 10026, "أرمل", 1007, "Widowed", null, null, null, "Widowed", true, false, 4, null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "LookupItems",
                keyColumn: "Id",
                keyValue: 10021);

            migrationBuilder.DeleteData(
                table: "LookupItems",
                keyColumn: "Id",
                keyValue: 10022);

            migrationBuilder.DeleteData(
                table: "LookupItems",
                keyColumn: "Id",
                keyValue: 10023);

            migrationBuilder.DeleteData(
                table: "LookupItems",
                keyColumn: "Id",
                keyValue: 10024);

            migrationBuilder.DeleteData(
                table: "LookupItems",
                keyColumn: "Id",
                keyValue: 10025);

            migrationBuilder.DeleteData(
                table: "LookupItems",
                keyColumn: "Id",
                keyValue: 10026);

            migrationBuilder.DeleteData(
                table: "LookupCategories",
                keyColumn: "Id",
                keyValue: 1006);

            migrationBuilder.DeleteData(
                table: "LookupCategories",
                keyColumn: "Id",
                keyValue: 1007);
        }
    }
}
