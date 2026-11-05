using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maydan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveWorkerCountryCity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Workers_Cities_CityId",
                table: "Workers");

            migrationBuilder.DropForeignKey(
                name: "FK_Workers_Countries_CountryId",
                table: "Workers");

            migrationBuilder.DropIndex(
                name: "IX_Workers_CityId",
                table: "Workers");

            migrationBuilder.DropIndex(
                name: "IX_Workers_CountryId",
                table: "Workers");

            migrationBuilder.DropColumn(
                name: "CityId",
                table: "Workers");

            migrationBuilder.DropColumn(
                name: "CountryId",
                table: "Workers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CityId",
                table: "Workers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CountryId",
                table: "Workers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Workers_CityId",
                table: "Workers",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_Workers_CountryId",
                table: "Workers",
                column: "CountryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Workers_Cities_CityId",
                table: "Workers",
                column: "CityId",
                principalTable: "Cities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Workers_Countries_CountryId",
                table: "Workers",
                column: "CountryId",
                principalTable: "Countries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
