using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maydan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EditCalculationsTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TimeUnit",
                table: "ServiceRequests");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TimeUnit",
                table: "ServiceRequests",
                type: "int",
                nullable: false,
                defaultValue: 1);
        }
    }
}
