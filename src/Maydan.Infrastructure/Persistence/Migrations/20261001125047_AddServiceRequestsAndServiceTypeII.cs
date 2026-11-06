using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maydan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceRequestsAndServiceTypeII : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ServiceRequests_Projects_ProjectTypeId",
                table: "ServiceRequests");

            migrationBuilder.RenameColumn(
                name: "ProjectTypeId",
                table: "ServiceRequests",
                newName: "ProjectId");

            migrationBuilder.RenameIndex(
                name: "IX_ServiceRequests_ProjectTypeId",
                table: "ServiceRequests",
                newName: "IX_ServiceRequests_ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceRequests_Projects_ProjectId",
                table: "ServiceRequests",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ServiceRequests_Projects_ProjectId",
                table: "ServiceRequests");

            migrationBuilder.RenameColumn(
                name: "ProjectId",
                table: "ServiceRequests",
                newName: "ProjectTypeId");

            migrationBuilder.RenameIndex(
                name: "IX_ServiceRequests_ProjectId",
                table: "ServiceRequests",
                newName: "IX_ServiceRequests_ProjectTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceRequests_Projects_ProjectTypeId",
                table: "ServiceRequests",
                column: "ProjectTypeId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
