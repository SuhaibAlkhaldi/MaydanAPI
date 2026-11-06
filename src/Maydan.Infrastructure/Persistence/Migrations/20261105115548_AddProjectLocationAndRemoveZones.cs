using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace Maydan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectLocationAndRemoveZones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssociationZones");

            migrationBuilder.AddColumn<Point>(
                name: "Location",
                table: "Projects",
                type: "geography",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Location",
                table: "Projects");

            migrationBuilder.CreateTable(
                name: "AssociationZones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssociationId = table.Column<int>(type: "int", nullable: false),
                    ServiceRequestId = table.Column<int>(type: "int", nullable: true),
                    ArabicName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    BookedFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BookedUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Boundary = table.Column<Polygon>(type: "geography", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EnglishName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssociationZones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssociationZones_Associations_AssociationId",
                        column: x => x.AssociationId,
                        principalTable: "Associations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssociationZones_ServiceRequests_ServiceRequestId",
                        column: x => x.ServiceRequestId,
                        principalTable: "ServiceRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssociationZones_AssociationId",
                table: "AssociationZones",
                column: "AssociationId");

            migrationBuilder.CreateIndex(
                name: "IX_AssociationZones_ServiceRequestId",
                table: "AssociationZones",
                column: "ServiceRequestId");
        }
    }
}
