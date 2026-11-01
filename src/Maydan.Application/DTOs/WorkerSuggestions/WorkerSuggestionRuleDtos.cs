using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Maydan.Application.WorkerSuggestions;

namespace Maydan.Application.DTOs.WorkerSuggestions
{
    // Create a new Filter or Sort rule
    public sealed record CreateWorkerSuggestionRuleRequest(
        string AttributeKey,
        int RuleTypeId,
        int? OperatorId,
        int? SortDirectionId,
        string? ComparisonValue,
        int? ComparisonLookupItemId,
        int PriorityOrder,
        bool IsActive = true
    );

    // Update an existing rule

    public sealed class UpdateWorkerSuggestionRuleRequest
    {
        public string? AttributeKey { get; set; }
        public int? RuleTypeId { get; set; }
        public int? OperatorId { get; set; }
        public int? SortDirectionId { get; set; }
        public string? ComparisonValue { get; set; }
        public int? ComparisonLookupItemId { get; set; }
        public int? PriorityOrder { get; set; }
        public bool? IsActive { get; set; }
    }


    // Reorder rules
    public sealed record ReorderWorkerSuggestionRulesRequest(
        IReadOnlyList<int> OrderedRuleIds
    );

    // API response

    public sealed record WorkerSuggestionRuleDto(
        int Id,
        string AttributeKey,
        int RuleTypeId,
        string RuleTypeCode,
        int? OperatorId,
        string? OperatorCode,
        int? SortDirectionId,
        string? SortDirectionCode,
        string? ComparisonValue,
        int? ComparisonLookupItemId,
        string? ComparisonLookupItemCode,
        int PriorityOrder,
        bool IsActive,
        bool IsDeleted,
        DateTime? DeletedAt
    )
    {
        private WorkerAttributeDescriptor? Attribute =>
            WorkerAttributeCatalog.GetAll()
                .FirstOrDefault(x =>
                    string.Equals(
                        x.Key,
                        AttributeKey,
                        StringComparison.OrdinalIgnoreCase));

        public string AttributeNameEn =>
            Attribute?.NameEn ?? AttributeKey;

        public string AttributeNameAr =>
            Attribute?.NameAr ?? AttributeKey;

        public string RuleTypeNameAr => RuleTypeCode switch
        {
            "Filter" => "تصفية",
            "Sort" => "ترتيب",
            _ => RuleTypeCode
        };

        public string RuleTypeNameEn => RuleTypeCode switch
        {
            "Filter" => "Filter",
            "Sort" => "Sort",
            _ => RuleTypeCode
        };

        public string? SortDirectionNameAr => SortDirectionCode switch
        {
            "Ascending" => "تصاعدي",
            "Descending" => "تنازلي",
            _ => null
        };

        public string? SortDirectionNameEn => SortDirectionCode switch
        {
            "Ascending" => "Ascending",
            "Descending" => "Descending",
            _ => null
        };

        public string? OperatorNameAr => OperatorCode switch
        {
            "Equal" => "يساوي",
            "NotEqual" => "لا يساوي",
            "GreaterThan" => "أكبر من",
            "LessThan" => "أقل من",
            "GreaterThanOrEqual" => "أكبر من أو يساوي",
            "LessThanOrEqual" => "أقل من أو يساوي",
            "Contains" => "يحتوي على",
            _ => null
        };

        public string? OperatorNameEn => OperatorCode switch
        {
            "Equal" => "Equal",
            "NotEqual" => "Not Equal",
            "GreaterThan" => "Greater Than",
            "LessThan" => "Less Than",
            "GreaterThanOrEqual" => "Greater Than or Equal",
            "LessThanOrEqual" => "Less Than or Equal",
            "Contains" => "Contains",
            _ => null
        };

        private string ComparisonDisplayAr => ComparisonValue switch
        {
            "true" => "نعم",
            "false" => "لا",
            _ => ComparisonValue ?? ComparisonLookupItemCode ?? "-"
        };

        private string ComparisonDisplayEn =>
            ComparisonValue ?? ComparisonLookupItemCode ?? "-";

        public string DescriptionAr => RuleTypeCode switch
        {
            "Sort" =>
                $"ترتيب العمال حسب {AttributeNameAr} " +
                $"({SortDirectionNameAr ?? "غير محدد"})، " +
                "والقيم غير المسجلة تظهر بالأخير.",

            "Filter" =>
                $"تصفية العمال حسب الشرط: {AttributeNameAr} " +
                $"{OperatorNameAr ?? "عملية غير معروفة"} " +
                $"{ComparisonDisplayAr}.",

            _ => "نوع القاعدة غير معروف."
        };

        public string DescriptionEn => RuleTypeCode switch
        {
            "Sort" =>
                $"Sort workers by {AttributeNameEn} " +
                $"({SortDirectionNameEn ?? "unspecified"}). " +
                "Missing values appear last.",

            "Filter" =>
                $"Filter workers where {AttributeNameEn} " +
                $"{OperatorNameEn ?? "unknown operator"} " +
                $"{ComparisonDisplayEn}.",

            _ => "Unknown rule type."
        };
    }

}
