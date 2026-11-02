using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maydan.Application.DTOs.WorkerSuggestions
{
    public sealed record WorkerSuggestionAttributeDto(
    string Key,
    string NameEn,
    string NameAr,
    string DataTypeCode,
    string SourceTypeCode,
    bool IsFilterable,
    bool IsSortable,
    IReadOnlyList<string> AllowedRuleTypes,
    IReadOnlyList<string> AllowedOperators,
    string InputType,
    string? LookupCategoryCode
);
}