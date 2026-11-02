using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maydan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceRequestIdToWorkerProjectLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM [WorkerProjectLinks];");
            migrationBuilder.AddColumn<int>(
                name: "ServiceRequestId",
                table: "WorkerProjectLinks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_WorkerProjectLinks_ServiceRequestId",
                table: "WorkerProjectLinks",
                column: "ServiceRequestId");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkerProjectLinks_ServiceRequests_ServiceRequestId",
                table: "WorkerProjectLinks",
                column: "ServiceRequestId",
                principalTable: "ServiceRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkerProjectLinks_ServiceRequests_ServiceRequestId",
                table: "WorkerProjectLinks");

            migrationBuilder.DropIndex(
                name: "IX_WorkerProjectLinks_ServiceRequestId",
                table: "WorkerProjectLinks");

            migrationBuilder.DropColumn(
                name: "ServiceRequestId",
                table: "WorkerProjectLinks");
        }
    }
}
