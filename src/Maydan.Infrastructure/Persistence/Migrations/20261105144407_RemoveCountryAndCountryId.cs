using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Maydan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCountryAndCountryId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cities_Countries_CountryId",
                table: "Cities");

            migrationBuilder.DropTable(
                name: "Countries");

            migrationBuilder.DropIndex(
                name: "IX_Cities_CountryId",
                table: "Cities");

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 43, 1 });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 44, 1 });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 43, 3 });

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 43);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 44);

            migrationBuilder.DropColumn(
                name: "CountryId",
                table: "Cities");

            migrationBuilder.CreateTable(
                name: "WorkerSuggestionRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AttributeKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RuleTypeId = table.Column<int>(type: "int", nullable: false),
                    OperatorId = table.Column<int>(type: "int", nullable: true),
                    SortDirectionId = table.Column<int>(type: "int", nullable: true),
                    ComparisonValue = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ComparisonLookupItemId = table.Column<int>(type: "int", nullable: true),
                    PriorityOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkerSuggestionRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkerSuggestionRules_LookupItems_ComparisonLookupItemId",
                        column: x => x.ComparisonLookupItemId,
                        principalTable: "LookupItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkerSuggestionRules_LookupItems_OperatorId",
                        column: x => x.OperatorId,
                        principalTable: "LookupItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkerSuggestionRules_LookupItems_RuleTypeId",
                        column: x => x.RuleTypeId,
                        principalTable: "LookupItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkerSuggestionRules_LookupItems_SortDirectionId",
                        column: x => x.SortDirectionId,
                        principalTable: "LookupItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "PermissionId", "CreatedAt", "CreatedBy", "DeletedAt", "Id", "IsActive", "IsDeleted", "Module", "PermissionNameAr", "PermissionNameEn", "UpdatedAt" },
                values: new object[] { 45, new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 0, true, false, "WorkerSuggestions", "إدارة قواعد اقتراح العمال", "Manage Worker Suggestion Rules", new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "PermissionId", "RoleId", "IsActive" },
                values: new object[] { 45, 1, true });

            migrationBuilder.CreateIndex(
                name: "IX_WorkerSuggestionRules_ComparisonLookupItemId",
                table: "WorkerSuggestionRules",
                column: "ComparisonLookupItemId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkerSuggestionRules_IsActive_PriorityOrder",
                table: "WorkerSuggestionRules",
                columns: new[] { "IsActive", "PriorityOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkerSuggestionRules_OperatorId",
                table: "WorkerSuggestionRules",
                column: "OperatorId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkerSuggestionRules_RuleTypeId",
                table: "WorkerSuggestionRules",
                column: "RuleTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkerSuggestionRules_SortDirectionId",
                table: "WorkerSuggestionRules",
                column: "SortDirectionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkerSuggestionRules");

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 45, 1 });

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 45);

            migrationBuilder.AddColumn<int>(
                name: "CountryId",
                table: "Cities",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Countries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ArabicName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
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
                    table.PrimaryKey("PK_Countries", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: 1,
                column: "CountryId",
                value: 1);

            migrationBuilder.UpdateData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: 2,
                column: "CountryId",
                value: 1);

            migrationBuilder.UpdateData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: 3,
                column: "CountryId",
                value: 1);

            migrationBuilder.UpdateData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: 4,
                column: "CountryId",
                value: 1);

            migrationBuilder.UpdateData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: 5,
                column: "CountryId",
                value: 1);

            migrationBuilder.UpdateData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: 6,
                column: "CountryId",
                value: 1);

            migrationBuilder.UpdateData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: 7,
                column: "CountryId",
                value: 1);

            migrationBuilder.UpdateData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: 8,
                column: "CountryId",
                value: 1);

            migrationBuilder.UpdateData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: 9,
                column: "CountryId",
                value: 1);

            migrationBuilder.UpdateData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: 10,
                column: "CountryId",
                value: 1);

            migrationBuilder.UpdateData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: 11,
                column: "CountryId",
                value: 1);

            migrationBuilder.UpdateData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: 12,
                column: "CountryId",
                value: 1);

            migrationBuilder.InsertData(
                table: "Countries",
                columns: new[] { "Id", "ArabicName", "CreatedAt", "CreatedBy", "DeletedAt", "EnglishName", "IsActive", "IsDeleted", "UpdatedAt" },
                values: new object[] { 1, "الأردن", null, null, null, "Jordan", true, false, null });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "PermissionId", "CreatedAt", "CreatedBy", "DeletedAt", "Id", "IsActive", "IsDeleted", "Module", "PermissionNameAr", "PermissionNameEn", "UpdatedAt" },
                values: new object[,]
                {
                    { 43, new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 0, true, false, "Attendance", "اعتماد الوقت الإضافي", "Approve Overtime", new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 44, new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 0, true, false, "Attendance", "معالجة حالات الحضور غير المسجلة", "Resolve Attendance", new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "PermissionId", "RoleId", "IsActive" },
                values: new object[,]
                {
                    { 43, 1, true },
                    { 44, 1, true },
                    { 43, 3, true }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cities_CountryId",
                table: "Cities",
                column: "CountryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Cities_Countries_CountryId",
                table: "Cities",
                column: "CountryId",
                principalTable: "Countries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
