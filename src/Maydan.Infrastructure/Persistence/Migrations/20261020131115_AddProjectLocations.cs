using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Maydan.Infrastructure.Persistence.Migrations
{
    public partial class AddProjectLocations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // إضافة الأعمدة مع ربطها بالموقع والمدينة رقم 1 الموجودين مسبقاً في الداتابيس
            migrationBuilder.AddColumn<int>(
                name: "CityId",
                table: "Projects",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "CityLocationId",
                table: "Projects",
                type: "int",
                nullable: false,
                defaultValue: 1);


            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "PermissionId", "RoleId", "IsActive" },
                values: new object[,]
                {
                    { 41, 1, true },
                    { 42, 1, true }
                });


            migrationBuilder.CreateIndex(
                name: "IX_Projects_CityId",
                table: "Projects",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_CityLocationId",
                table: "Projects",
                column: "CityLocationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Cities_CityId",
                table: "Projects",
                column: "CityId",
                principalTable: "Cities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_CityLocations_CityLocationId",
                table: "Projects",
                column: "CityLocationId",
                principalTable: "CityLocations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Cities_CityId",
                table: "Projects");

            migrationBuilder.DropForeignKey(
                name: "FK_Projects_CityLocations_CityLocationId",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Projects_CityId",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Projects_CityLocationId",
                table: "Projects");

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 41, 1 });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 42, 1 });

            migrationBuilder.DropColumn(
                name: "CityId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "CityLocationId",
                table: "Projects");
        }
    }
}