using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maydan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEntityScopeToGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Groups_GroupNameAr",
                table: "Groups");

            migrationBuilder.DropIndex(
                name: "IX_Groups_GroupNameEn",
                table: "Groups");

            migrationBuilder.AddColumn<int>(
                name: "EntityId",
                table: "Groups",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "EntityType",
                table: "Groups",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_EntityType_EntityId",
                table: "Groups",
                columns: new[] { "EntityType", "EntityId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Groups_EntityType_EntityId",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "EntityId",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "EntityType",
                table: "Groups");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_GroupNameAr",
                table: "Groups",
                column: "GroupNameAr",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Groups_GroupNameEn",
                table: "Groups",
                column: "GroupNameEn",
                unique: true);
        }
    }
}
