using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maydan.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AssoctionV2Remove : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Workers_AssociationsV2_AssociationV2Id",
                table: "Workers");

            migrationBuilder.DropTable(
                name: "AssociationsV2");

            migrationBuilder.DropIndex(
                name: "IX_Workers_AssociationV2Id",
                table: "Workers");

            migrationBuilder.DropIndex(
                name: "IX_Users_FirstNameAr_LastNameAr",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_FirstNameEn_LastNameEn",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AssociationV2Id",
                table: "Workers");

            migrationBuilder.AlterColumn<string>(
                name: "ContactPhone",
                table: "Associations",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ContactEmail",
                table: "Associations",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CityId1",
                table: "Associations",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_PhoneNumber",
                table: "Users",
                column: "PhoneNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Associations_CityId1",
                table: "Associations",
                column: "CityId1");

            migrationBuilder.CreateIndex(
                name: "IX_Associations_ContactEmail",
                table: "Associations",
                column: "ContactEmail",
                unique: true,
                filter: "[ContactEmail] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Associations_Cities_CityId1",
                table: "Associations",
                column: "CityId1",
                principalTable: "Cities",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Associations_Cities_CityId1",
                table: "Associations");

            migrationBuilder.DropIndex(
                name: "IX_Users_PhoneNumber",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Associations_CityId1",
                table: "Associations");

            migrationBuilder.DropIndex(
                name: "IX_Associations_ContactEmail",
                table: "Associations");

            migrationBuilder.DropColumn(
                name: "CityId1",
                table: "Associations");

            migrationBuilder.AddColumn<int>(
                name: "AssociationV2Id",
                table: "Workers",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ContactPhone",
                table: "Associations",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "ContactEmail",
                table: "Associations",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.CreateTable(
                name: "AssociationsV2",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CityId = table.Column<int>(type: "int", nullable: false),
                    ArabicName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ContactEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ContactPhone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EnglishName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: false),
                    LocationOnGoogleMaps = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssociationsV2", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssociationsV2_Cities_CityId",
                        column: x => x.CityId,
                        principalTable: "Cities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Workers_AssociationV2Id",
                table: "Workers",
                column: "AssociationV2Id");

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
                name: "IX_AssociationsV2_ArabicName",
                table: "AssociationsV2",
                column: "ArabicName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssociationsV2_CityId",
                table: "AssociationsV2",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_AssociationsV2_ContactEmail",
                table: "AssociationsV2",
                column: "ContactEmail",
                unique: true,
                filter: "[ContactEmail] IS NOT NULL AND [ContactEmail] != ''");

            migrationBuilder.CreateIndex(
                name: "IX_AssociationsV2_ContactPhone",
                table: "AssociationsV2",
                column: "ContactPhone",
                unique: true,
                filter: "[ContactPhone] IS NOT NULL AND [ContactPhone] != ''");

            migrationBuilder.CreateIndex(
                name: "IX_AssociationsV2_EnglishName",
                table: "AssociationsV2",
                column: "EnglishName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssociationsV2_Latitude_Longitude",
                table: "AssociationsV2",
                columns: new[] { "Latitude", "Longitude" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Workers_AssociationsV2_AssociationV2Id",
                table: "Workers",
                column: "AssociationV2Id",
                principalTable: "AssociationsV2",
                principalColumn: "Id");
        }
    }
}
