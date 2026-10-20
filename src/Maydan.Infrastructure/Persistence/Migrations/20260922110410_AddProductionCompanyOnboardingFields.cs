using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maydan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductionCompanyOnboardingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CityId",
                table: "ProductionCompanies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "RegistrationNumber",
                table: "ProductionCompanies",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionCompanies_CityId",
                table: "ProductionCompanies",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionCompanies_RegistrationNumber",
                table: "ProductionCompanies",
                column: "RegistrationNumber",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionCompanies_Cities_CityId",
                table: "ProductionCompanies",
                column: "CityId",
                principalTable: "Cities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductionCompanies_Cities_CityId",
                table: "ProductionCompanies");

            migrationBuilder.DropIndex(
                name: "IX_ProductionCompanies_CityId",
                table: "ProductionCompanies");

            migrationBuilder.DropIndex(
                name: "IX_ProductionCompanies_RegistrationNumber",
                table: "ProductionCompanies");

            migrationBuilder.DropColumn(
                name: "CityId",
                table: "ProductionCompanies");

            migrationBuilder.DropColumn(
                name: "RegistrationNumber",
                table: "ProductionCompanies");
        }
    }
}
