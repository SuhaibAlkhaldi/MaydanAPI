using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maydan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixWorkerProjectLink_SurrogateKey_CorrectAuditTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkerProjectLinks_Projects_ProjectId",
                table: "WorkerProjectLinks");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkerProjectLinks_Workers_WorkerId",
                table: "WorkerProjectLinks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_WorkerProjectLinks",
                table: "WorkerProjectLinks");

            

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedDate",
                table: "WorkerProjectLinks",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.DropColumn(
    name: "UpdatedBy",
    table: "WorkerProjectLinks");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "WorkerProjectLinks");

            migrationBuilder.AddColumn<int>(
                name: "UpdatedBy",
                table: "WorkerProjectLinks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedBy",
                table: "WorkerProjectLinks",
                type: "int",
                nullable: true);


            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "WorkerProjectLinks",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");


            migrationBuilder.AddPrimaryKey(
                name: "PK_WorkerProjectLinks",
                table: "WorkerProjectLinks",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_WorkerProjectLinks_ProjectId_WorkerId",
                table: "WorkerProjectLinks",
                columns: new[] { "ProjectId", "WorkerId" },
                unique: true,
                filter: "[IsWorkerActive] = 1");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkerProjectLinks_Projects_ProjectId",
                table: "WorkerProjectLinks",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkerProjectLinks_Workers_WorkerId",
                table: "WorkerProjectLinks",
                column: "WorkerId",
                principalTable: "Workers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkerProjectLinks_Projects_ProjectId",
                table: "WorkerProjectLinks");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkerProjectLinks_Workers_WorkerId",
                table: "WorkerProjectLinks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_WorkerProjectLinks",
                table: "WorkerProjectLinks");

            migrationBuilder.DropIndex(
                name: "IX_WorkerProjectLinks_ProjectId_WorkerId",
                table: "WorkerProjectLinks");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "WorkerProjectLinks");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedBy",
                table: "WorkerProjectLinks",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedDate",
                table: "WorkerProjectLinks",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedBy",
                table: "WorkerProjectLinks",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_WorkerProjectLinks",
                table: "WorkerProjectLinks",
                columns: new[] { "ProjectId", "WorkerId" });

            migrationBuilder.AddForeignKey(
                name: "FK_WorkerProjectLinks_Projects_ProjectId",
                table: "WorkerProjectLinks",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkerProjectLinks_Workers_WorkerId",
                table: "WorkerProjectLinks",
                column: "WorkerId",
                principalTable: "Workers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
