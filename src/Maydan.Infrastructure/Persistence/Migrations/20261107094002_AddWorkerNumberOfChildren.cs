using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maydan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkerNumberOfChildren : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "NumberOfChildren",
                table: "Workers",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NumberOfChildren",
                table: "Workers");
        }
    }
}
