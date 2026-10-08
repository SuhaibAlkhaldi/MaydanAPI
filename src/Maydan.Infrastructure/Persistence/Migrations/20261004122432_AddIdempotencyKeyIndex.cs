using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maydan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIdempotencyKeyIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TimeUnit",
                table: "ServiceRequests",
                type: "int",
                nullable: false,
                defaultValue: 1);
            migrationBuilder.Sql(@"
    WITH Duplicates AS
    (
        SELECT 
            Id,
            ROW_NUMBER() OVER (
                PARTITION BY IdempotencyKey, ProductionCompanyId
                ORDER BY Id
            ) AS RowNum
        FROM ServiceRequests
        WHERE IdempotencyKey IS NOT NULL
    )
    UPDATE sr
    SET IdempotencyKey = NULL
    FROM ServiceRequests sr
    INNER JOIN Duplicates d ON sr.Id = d.Id
    WHERE d.RowNum > 1;
");
            migrationBuilder.CreateIndex(
                name: "IX_ServiceRequests_IdempotencyKey_ProductionCompanyId",
                table: "ServiceRequests",
                columns: new[] { "IdempotencyKey", "ProductionCompanyId" },
                unique: true,
                filter: "[IdempotencyKey] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ServiceRequests_IdempotencyKey_ProductionCompanyId",
                table: "ServiceRequests");

            migrationBuilder.DropColumn(
                name: "TimeUnit",
                table: "ServiceRequests");
        }
    }
}
