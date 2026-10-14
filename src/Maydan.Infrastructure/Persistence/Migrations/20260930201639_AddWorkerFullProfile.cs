using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maydan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkerFullProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Services_NameAr",
                table: "Services");

            migrationBuilder.DropIndex(
                name: "IX_Services_NameEn",
                table: "Services");

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

            migrationBuilder.AddColumn<int>(
                name: "Gender",
                table: "Workers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaritalStatus",
                table: "Workers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Nationality",
                table: "Workers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "YearsOfExperience",
                table: "Workers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "WorkerServiceLinks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkerId = table.Column<int>(type: "int", nullable: false),
                    ServiceId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkerServiceLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkerServiceLinks_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkerServiceLinks_Workers_WorkerId",
                        column: x => x.WorkerId,
                        principalTable: "Workers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Workers_CityId",
                table: "Workers",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_Workers_CountryId",
                table: "Workers",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_Services_NameAr",
                table: "Services",
                column: "NameAr",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Services_NameEn",
                table: "Services",
                column: "NameEn",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_WorkerServiceLinks_ServiceId",
                table: "WorkerServiceLinks",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkerServiceLinks_WorkerId_ServiceId",
                table: "WorkerServiceLinks",
                columns: new[] { "WorkerId", "ServiceId" },
                unique: true);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Workers_Cities_CityId",
                table: "Workers");

            migrationBuilder.DropForeignKey(
                name: "FK_Workers_Countries_CountryId",
                table: "Workers");

            migrationBuilder.DropTable(
                name: "WorkerServiceLinks");

            migrationBuilder.DropIndex(
                name: "IX_Workers_CityId",
                table: "Workers");

            migrationBuilder.DropIndex(
                name: "IX_Workers_CountryId",
                table: "Workers");

            migrationBuilder.DropIndex(
                name: "IX_Services_NameAr",
                table: "Services");

            migrationBuilder.DropIndex(
                name: "IX_Services_NameEn",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "CityId",
                table: "Workers");

            migrationBuilder.DropColumn(
                name: "CountryId",
                table: "Workers");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "Workers");

            migrationBuilder.DropColumn(
                name: "MaritalStatus",
                table: "Workers");

            migrationBuilder.DropColumn(
                name: "Nationality",
                table: "Workers");

            migrationBuilder.DropColumn(
                name: "YearsOfExperience",
                table: "Workers");

            migrationBuilder.CreateIndex(
                name: "IX_Services_NameAr",
                table: "Services",
                column: "NameAr",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Services_NameEn",
                table: "Services",
                column: "NameEn",
                unique: true);
        }
    }
}
