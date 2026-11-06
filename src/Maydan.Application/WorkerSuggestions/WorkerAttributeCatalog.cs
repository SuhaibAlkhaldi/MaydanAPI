using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maydan.Application.WorkerSuggestions
{
    public sealed record WorkerAttributeDescriptor(
        string Key,
        string NameEn,
        string NameAr,
        string SourceKey,
        string DataTypeCode,
        string SourceTypeCode,
        bool IsFilterable,
        bool IsSortable
    );

    public static class WorkerAttributeCatalog
    {
        private static readonly IReadOnlyList<WorkerAttributeDescriptor>
            Attributes = Array.AsReadOnly(new[]
            {
                new WorkerAttributeDescriptor(
                    "YearsOfExperience",
                    "Years of Experience",
                    "سنوات الخبرة",
                    "YearsOfExperience",
                    "Number",
                    "DirectProperty",
                    true,
                    true
                ),


                new WorkerAttributeDescriptor(
                    "Age",
                    "Age",
                    "العمر",
                    "Age",
                    "Number",
                    "CalculatedProperty",
                    true,
                    true
                ),

                new WorkerAttributeDescriptor(
                     "Nationality",
                     "Nationality",
                     "الجنسية",
                     "Nationality",
                     "Text",
                     "LookupProperty",
                      true,
                      false
    ),

                new WorkerAttributeDescriptor(
                    "Gender",
                    "Gender",
                    "الجنس",
                    "GenderId",
                    "Lookup",
                    "LookupProperty",
                    true,
                    false
                ),

                new WorkerAttributeDescriptor(
                    "MaritalStatus",
                    "Marital Status",
                    "الحالة الاجتماعية",
                    "MaritalStatusId",
                    "Lookup",
                    "LookupProperty",
                    true,
                    false
                ),

                new WorkerAttributeDescriptor(
                    "HasDocuments",
                    "Has Documents",
                    "لديه وثائق",
                    "HasDocuments",
                    "Boolean",
                    "RelatedCollection",
                    true,
                    false
                )
            });

        private static readonly IReadOnlyDictionary<
            string, WorkerAttributeDescriptor> AttributesBySourceKey
            = Attributes.ToDictionary(
                x => x.SourceKey,
                StringComparer.OrdinalIgnoreCase
            );

        public static IReadOnlyList<WorkerAttributeDescriptor> GetAll()
        {
            return Attributes;
        }

        public static WorkerAttributeDescriptor? FindBySourceKey(
            string sourceKey)
        {
            if (string.IsNullOrWhiteSpace(sourceKey))
                return null;

            return AttributesBySourceKey.TryGetValue(
                sourceKey, out var attribute)
                ? attribute
                : null;
        }
    }
}