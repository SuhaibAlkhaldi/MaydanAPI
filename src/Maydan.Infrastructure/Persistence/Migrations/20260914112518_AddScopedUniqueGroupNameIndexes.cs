using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maydan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddScopedUniqueGroupNameIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Groups_EntityType_EntityId",
                table: "Groups");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_EntityType_EntityId_GroupNameAr",
                table: "Groups",
                columns: new[] { "EntityType", "EntityId", "GroupNameAr" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Groups_EntityType_EntityId_GroupNameEn",
                table: "Groups",
                columns: new[] { "EntityType", "EntityId", "GroupNameEn" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Groups_EntityType_EntityId_GroupNameAr",
                table: "Groups");

            migrationBuilder.DropIndex(
                name: "IX_Groups_EntityType_EntityId_GroupNameEn",
                table: "Groups");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_EntityType_EntityId",
                table: "Groups",
                columns: new[] { "EntityType", "EntityId" });
        }
    }
}
