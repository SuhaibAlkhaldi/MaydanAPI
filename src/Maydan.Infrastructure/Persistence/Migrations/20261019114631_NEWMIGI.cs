using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maydan.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NEWMIGI : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ServiceRequests_IdempotencyKey_ProductionCompanyId",
                table: "ServiceRequests");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "ServiceRequests");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "ServiceRequests",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRequests_IdempotencyKey_ProductionCompanyId",
                table: "ServiceRequests",
                columns: new[] { "IdempotencyKey", "ProductionCompanyId" },
                unique: true,
                filter: "[IdempotencyKey] IS NOT NULL");
        }
    }
}
