using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maydan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AssociationsValidates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "Longitude",
                table: "Associations",
                type: "decimal(9,6)",
                precision: 9,
                scale: 6,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(9,6)",
                oldPrecision: 9,
                oldScale: 6,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Latitude",
                table: "Associations",
                type: "decimal(9,6)",
                precision: 9,
                scale: 6,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(9,6)",
                oldPrecision: 9,
                oldScale: 6,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_FirstNameAr_LastNameAr",
                table: "Users",
                columns: new[] { "FirstNameAr", "LastNameAr" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_FirstNameEn_LastNameEn",
                table: "Users",
                columns: new[] { "FirstNameEn", "LastNameEn" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Associations_ArabicName_EnglishName",
                table: "Associations",
                columns: new[] { "ArabicName", "EnglishName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Associations_ContactPhone",
                table: "Associations",
                column: "ContactPhone",
                unique: true,
                filter: "[ContactPhone] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Associations_Latitude_Longitude",
                table: "Associations",
                columns: new[] { "Latitude", "Longitude" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_FirstNameAr_LastNameAr",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_FirstNameEn_LastNameEn",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Associations_ArabicName_EnglishName",
                table: "Associations");

            migrationBuilder.DropIndex(
                name: "IX_Associations_ContactPhone",
                table: "Associations");

            migrationBuilder.DropIndex(
                name: "IX_Associations_Latitude_Longitude",
                table: "Associations");

            migrationBuilder.AlterColumn<decimal>(
                name: "Longitude",
                table: "Associations",
                type: "decimal(9,6)",
                precision: 9,
                scale: 6,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(9,6)",
                oldPrecision: 9,
                oldScale: 6);

            migrationBuilder.AlterColumn<decimal>(
                name: "Latitude",
                table: "Associations",
                type: "decimal(9,6)",
                precision: 9,
                scale: 6,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(9,6)",
                oldPrecision: 9,
                oldScale: 6);
        }
    }
}
