using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maydan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class GrantApproveOvertimeToProductionHouse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "PermissionId", "RoleId", "IsActive" },
                values: new object[] { 43, 3, true });
        }
//            migrationBuilder.Sql(@"UPDATE WorkerAttendances SET AssociationEntryTypeId = EntryTypeId WHERE AssociationCheckInDate IS NOT NULL;
//UPDATE WorkerAttendances SET ProductionCompanyEntryTypeId = EntryTypeId WHERE ProductionCompanyCheckInDate IS NOT NULL;");
//        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 43, 3 });
        }
    }
}
