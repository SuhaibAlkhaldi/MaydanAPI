using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maydan.Infrastructure.Persistence.Migrations
{
    public partial class RevertWorkerBilingualNameColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('Workers', 'EnFirstName') IS NOT NULL
BEGIN
    DECLARE @dropConstraintsSql NVARCHAR(MAX) = N'';

    SELECT @dropConstraintsSql = @dropConstraintsSql + N'ALTER TABLE Workers DROP CONSTRAINT [' + dc.name + N'];' + CHAR(13)
    FROM sys.default_constraints dc
    JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
    WHERE dc.parent_object_id = OBJECT_ID('Workers')
      AND c.name IN ('EnFirstName','ArFirstName','EnMiddleName','ArMiddleName','EnLastName','ArLastName','WorkerType');

    IF @dropConstraintsSql <> N''
        EXEC sp_executesql @dropConstraintsSql;

    ALTER TABLE Workers DROP COLUMN EnFirstName, ArFirstName, EnMiddleName, ArMiddleName, EnLastName, ArLastName, WorkerType;
END
");

            migrationBuilder.Sql(@"
IF COL_LENGTH('Workers', 'FirstName') IS NULL
BEGIN
    ALTER TABLE Workers ADD FirstName nvarchar(100) NOT NULL DEFAULT '';
END
");

            migrationBuilder.Sql(@"
IF COL_LENGTH('Workers', 'LastName') IS NULL
BEGIN
    ALTER TABLE Workers ADD LastName nvarchar(100) NOT NULL DEFAULT '';
END
");

            migrationBuilder.Sql(@"
IF COL_LENGTH('Workers', 'MiddleName') IS NULL
BEGIN
    ALTER TABLE Workers ADD MiddleName nvarchar(100) NULL;
END
");

            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20260930212507_UpdateWorkerEntityColumns')
BEGIN
    DELETE FROM __EFMigrationsHistory WHERE MigrationId = '20260930212507_UpdateWorkerEntityColumns';
END
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
