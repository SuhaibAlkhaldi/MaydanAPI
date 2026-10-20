using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Maydan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Initialize_DB : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Every operation originally generated here exactly duplicated an operation already
            // performed by 20260909141654_InitialCreate (rename Users.UserName -> LastNameEn,
            // drop IX_Users_UserName, add the audit columns, insert the seed Role/User rows,
            // create IX_Users_Email/IX_Roles_RoleNameAr/IX_Permissions_PermissionNameAr/
            // IX_Groups_GroupNameAr, etc.). The model state this migration targets was already
            // reached by InitialCreate three migrations earlier, so re-applying it fails on the
            // very first statement (IX_Users_UserName no longer exists). This migration is a
            // deliberate no-op.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Inverse of Up(): no-op for the same reason.
        }
    }
}
