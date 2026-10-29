using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    );
}
