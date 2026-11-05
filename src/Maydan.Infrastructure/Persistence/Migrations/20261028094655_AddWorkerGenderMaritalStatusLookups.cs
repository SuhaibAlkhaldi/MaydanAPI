using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Maydan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkerGenderMaritalStatusLookups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MaritalStatus",
                table: "Workers",
                newName: "MaritalStatusId");

            migrationBuilder.RenameColumn(
                name: "Gender",
                table: "Workers",
                newName: "GenderId");

            migrationBuilder.CreateTable(
                name: "LookupCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ArabicName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    EnglishName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LookupCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LookupItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ArabicName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    EnglishName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LookupItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LookupItems_LookupCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "LookupCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "LookupCategories",
                columns: new[] { "Id", "ArabicName", "Code", "CreatedAt", "CreatedBy", "DeletedAt", "EnglishName", "IsActive", "IsDeleted", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "الجنس", "Gender", null, null, null, "Gender", true, false, null },
                    { 2, "الحالة الاجتماعية", "MaritalStatus", null, null, null, "Marital Status", true, false, null }
                });

            migrationBuilder.InsertData(
                table: "LookupItems",
                columns: new[] { "Id", "ArabicName", "CategoryId", "Code", "CreatedAt", "CreatedBy", "DeletedAt", "EnglishName", "IsActive", "IsDeleted", "SortOrder", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "ذكر", 1, "Male", null, null, null, "Male", true, false, 1, null },
                    { 2, "أنثى", 1, "Female", null, null, null, "Female", true, false, 2, null },
                    { 3, "أعزب", 2, "Single", null, null, null, "Single", true, false, 1, null },
                    { 4, "متزوج", 2, "Married", null, null, null, "Married", true, false, 2, null },
                    { 5, "مطلق", 2, "Divorced", null, null, null, "Divorced", true, false, 3, null },
                    { 6, "أرمل", 2, "Widowed", null, null, null, "Widowed", true, false, 4, null }
                });

            // Old MaritalStatus enum values (1=Single..4=Widowed) don't line up with the new
            // LookupItems rows seeded above (Ids 3-6, since Gender's Male/Female took 1-2 in the
            // same shared table) — GenderId needs no shift since Male=1/Female=2 already match.
            migrationBuilder.Sql("UPDATE Workers SET MaritalStatusId = MaritalStatusId + 2 WHERE MaritalStatusId IS NOT NULL;");

            migrationBuilder.CreateIndex(
                name: "IX_Workers_GenderId",
                table: "Workers",
                column: "GenderId");

            migrationBuilder.CreateIndex(
                name: "IX_Workers_MaritalStatusId",
                table: "Workers",
                column: "MaritalStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_LookupCategories_Code",
                table: "LookupCategories",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LookupItems_CategoryId_Code",
                table: "LookupItems",
                columns: new[] { "CategoryId", "Code" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Workers_LookupItems_GenderId",
                table: "Workers",
                column: "GenderId",
                principalTable: "LookupItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Workers_LookupItems_MaritalStatusId",
                table: "Workers",
                column: "MaritalStatusId",
                principalTable: "LookupItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Workers_LookupItems_GenderId",
                table: "Workers");

            migrationBuilder.DropForeignKey(
                name: "FK_Workers_LookupItems_MaritalStatusId",
                table: "Workers");

            migrationBuilder.DropTable(
                name: "LookupItems");

            migrationBuilder.DropTable(
                name: "LookupCategories");

            migrationBuilder.DropIndex(
                name: "IX_Workers_GenderId",
                table: "Workers");

            migrationBuilder.DropIndex(
                name: "IX_Workers_MaritalStatusId",
                table: "Workers");

            migrationBuilder.Sql("UPDATE Workers SET MaritalStatusId = MaritalStatusId - 2 WHERE MaritalStatusId IS NOT NULL;");

            migrationBuilder.RenameColumn(
                name: "MaritalStatusId",
                table: "Workers",
                newName: "MaritalStatus");

            migrationBuilder.RenameColumn(
                name: "GenderId",
                table: "Workers",
                newName: "Gender");
        }
    }
}
