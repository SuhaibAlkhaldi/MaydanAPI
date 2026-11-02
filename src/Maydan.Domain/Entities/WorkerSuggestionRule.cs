using Maydan.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maydan.Domain.Entities
{
    public class WorkerSuggestionRule : SharedEntities
    {
        // Attribute from WorkerAttributeCatalog
        public string AttributeKey { get; set; } = string.Empty;

        // Filter or Sort
        public int RuleTypeId { get; set; }
        public LookupItem RuleType { get; set; } = null!;

        // Filter Operator
        public int? OperatorId { get; set; }
        public LookupItem? Operator { get; set; }

        // Sort Direction
        public int? SortDirectionId { get; set; }
        public LookupItem? SortDirection { get; set; }

        // Filter Value
        public string? ComparisonValue { get; set; }

        // Lookup-based Filter Value
        public int? ComparisonLookupItemId { get; set; }
        public LookupItem? ComparisonLookupItem { get; set; }

        // Rule Priority
        public int PriorityOrder { get; set; }
    }
}
