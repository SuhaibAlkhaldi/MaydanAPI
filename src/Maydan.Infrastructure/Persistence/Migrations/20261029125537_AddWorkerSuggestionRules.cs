using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Maydan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkerSuggestionRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
          

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
                table: "LookupCategories",
                columns: new[] { "Id", "ArabicName", "Code", "CreatedAt", "CreatedBy", "DeletedAt", "EnglishName", "IsActive", "IsDeleted", "UpdatedAt" },
                values: new object[,]
                {
                    { 1001, "نوع بيانات المعيار", "AttributeDataType", null, null, null, "Attribute Data Type", true, false, null },
                    { 1002, "مصدر بيانات المعيار", "AttributeSourceType", null, null, null, "Attribute Source Type", true, false, null },
                    { 1003, "نوع قاعدة الاقتراح", "SuggestionRuleType", null, null, null, "Suggestion Rule Type", true, false, null },
                    { 1004, "عملية المقارنة", "ComparisonOperator", null, null, null, "Comparison Operator", true, false, null },
                    { 1005, "اتجاه الترتيب", "SortDirection", null, null, null, "Sort Direction", true, false, null }
                });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "PermissionId", "CreatedAt", "CreatedBy", "DeletedAt", "Id", "IsActive", "IsDeleted", "Module", "PermissionNameAr", "PermissionNameEn", "UpdatedAt" },
                values: new object[] { 44, new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 0, true, false, "WorkerSuggestions", "إدارة قواعد اقتراح العمال", "Manage Worker Suggestion Rules", new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.InsertData(
                table: "LookupItems",
                columns: new[] { "Id", "ArabicName", "CategoryId", "Code", "CreatedAt", "CreatedBy", "DeletedAt", "EnglishName", "IsActive", "IsDeleted", "SortOrder", "UpdatedAt" },
                values: new object[,]
                {
                    { 10001, "رقم", 1001, "Number", null, null, null, "Number", true, false, 1, null },
                    { 10002, "نص", 1001, "Text", null, null, null, "Text", true, false, 2, null },
                    { 10003, "منطقي", 1001, "Boolean", null, null, null, "Boolean", true, false, 3, null },
                    { 10004, "قائمة خيارات", 1001, "Lookup", null, null, null, "Lookup", true, false, 4, null },
                    { 10005, "تاريخ", 1001, "Date", null, null, null, "Date", true, false, 5, null },
                    { 10006, "خاصية مباشرة", 1002, "DirectProperty", null, null, null, "Direct Property", true, false, 1, null },
                    { 10007, "خاصية محسوبة", 1002, "CalculatedProperty", null, null, null, "Calculated Property", true, false, 2, null },
                    { 10008, "خاصية مرتبطة بقائمة", 1002, "LookupProperty", null, null, null, "Lookup Property", true, false, 3, null },
                    { 10009, "بيانات مرتبطة", 1002, "RelatedCollection", null, null, null, "Related Collection", true, false, 4, null },
                    { 10010, "تصفية", 1003, "Filter", null, null, null, "Filter", true, false, 1, null },
                    { 10011, "ترتيب", 1003, "Sort", null, null, null, "Sort", true, false, 2, null },
                    { 10012, "يساوي", 1004, "Equal", null, null, null, "Equal", true, false, 1, null },
                    { 10013, "لا يساوي", 1004, "NotEqual", null, null, null, "Not Equal", true, false, 2, null },
                    { 10014, "أكبر من", 1004, "GreaterThan", null, null, null, "Greater Than", true, false, 3, null },
                    { 10015, "أقل من", 1004, "LessThan", null, null, null, "Less Than", true, false, 4, null },
                    { 10016, "أكبر من أو يساوي", 1004, "GreaterThanOrEqual", null, null, null, "Greater Than or Equal", true, false, 5, null },
                    { 10017, "أقل من أو يساوي", 1004, "LessThanOrEqual", null, null, null, "Less Than or Equal", true, false, 6, null },
                    { 10018, "يحتوي على", 1004, "Contains", null, null, null, "Contains", true, false, 7, null },
                    { 10019, "تصاعدي", 1005, "Ascending", null, null, null, "Ascending", true, false, 1, null },
                    { 10020, "تنازلي", 1005, "Descending", null, null, null, "Descending", true, false, 2, null }
                });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "PermissionId", "RoleId", "IsActive" },
                values: new object[] { 44, 1, true });

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
                table: "LookupItems",
                keyColumn: "Id",
                keyValue: 10001);

            migrationBuilder.DeleteData(
                table: "LookupItems",
                keyColumn: "Id",
                keyValue: 10002);

            migrationBuilder.DeleteData(
                table: "LookupItems",
                keyColumn: "Id",
                keyValue: 10003);

            migrationBuilder.DeleteData(
                table: "LookupItems",
                keyColumn: "Id",
                keyValue: 10004);

            migrationBuilder.DeleteData(
                table: "LookupItems",
                keyColumn: "Id",
                keyValue: 10005);

            migrationBuilder.DeleteData(
                table: "LookupItems",
                keyColumn: "Id",
                keyValue: 10006);

            migrationBuilder.DeleteData(
                table: "LookupItems",
                keyColumn: "Id",
                keyValue: 10007);

            migrationBuilder.DeleteData(
                table: "LookupItems",
                keyColumn: "Id",
                keyValue: 10008);

            migrationBuilder.DeleteData(
                table: "LookupItems",
                keyColumn: "Id",
                keyValue: 10009);

            migrationBuilder.DeleteData(
                table: "LookupItems",
                keyColumn: "Id",
                keyValue: 10010);

            migrationBuilder.DeleteData(
                table: "LookupItems",
                keyColumn: "Id",
                keyValue: 10011);

            migrationBuilder.DeleteData(
                table: "LookupItems",
                keyColumn: "Id",
                keyValue: 10012);

            migrationBuilder.DeleteData(
                table: "LookupItems",
                keyColumn: "Id",
                keyValue: 10013);

            migrationBuilder.DeleteData(
                table: "LookupItems",
                keyColumn: "Id",
                keyValue: 10014);

            migrationBuilder.DeleteData(
                table: "LookupItems",
                keyColumn: "Id",
                keyValue: 10015);

            migrationBuilder.DeleteData(
                table: "LookupItems",
                keyColumn: "Id",
                keyValue: 10016);

            migrationBuilder.DeleteData(
                table: "LookupItems",
                keyColumn: "Id",
                keyValue: 10017);

            migrationBuilder.DeleteData(
                table: "LookupItems",
                keyColumn: "Id",
                keyValue: 10018);

            migrationBuilder.DeleteData(
                table: "LookupItems",
                keyColumn: "Id",
                keyValue: 10019);

            migrationBuilder.DeleteData(
                table: "LookupItems",
                keyColumn: "Id",
                keyValue: 10020);

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 44, 1 });

            migrationBuilder.DeleteData(
                table: "LookupCategories",
                keyColumn: "Id",
                keyValue: 1001);

            migrationBuilder.DeleteData(
                table: "LookupCategories",
                keyColumn: "Id",
                keyValue: 1002);

            migrationBuilder.DeleteData(
                table: "LookupCategories",
                keyColumn: "Id",
                keyValue: 1003);

            migrationBuilder.DeleteData(
                table: "LookupCategories",
                keyColumn: "Id",
                keyValue: 1004);

            migrationBuilder.DeleteData(
                table: "LookupCategories",
                keyColumn: "Id",
                keyValue: 1005);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 44);


        }
    }
}
