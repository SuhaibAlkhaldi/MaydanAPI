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
                    "FirstName",
                    "First Name",
                    "الاسم الأول",
                    "FirstName",
                    "Text",
                    "DirectProperty",
                    false,
                    true
                ),

                new WorkerAttributeDescriptor(
                    "LastName",
                    "Last Name",
                    "اسم العائلة",
                    "LastName",
                    "Text",
                    "DirectProperty",
                    false,
                    true
                ),

                new WorkerAttributeDescriptor(
                    "FullName",
                    "Full Name",
                    "الاسم الكامل",
                    "FullName",
                    "Text",
                    "CalculatedProperty",
                    false,
                    true
                ),

                new WorkerAttributeDescriptor(
                    "Age",
                    "Age",
                    "العمر",
                    "Age",
                    "Number",
                    "CalculatedProperty",
                    false,
                    false
                ),

                new WorkerAttributeDescriptor(
                    "Gender",
                    "Gender",
                    "الجنس",
                    "GenderId",
                    "Lookup",
                    "LookupProperty",
                    false,
                    false
                ),

                new WorkerAttributeDescriptor(
                    "MaritalStatus",
                    "Marital Status",
                    "الحالة الاجتماعية",
                    "MaritalStatusId",
                    "Lookup",
                    "LookupProperty",
                    false,
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
