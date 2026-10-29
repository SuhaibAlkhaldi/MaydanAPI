using Maydan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maydan.Infrastructure.Persistence.Seed
{
    public static class WorkerSuggestionLookupSeed
    {
        // Lookup Category IDs
        private const int DataTypeCategoryId = 1001;
        private const int SourceTypeCategoryId = 1002;
        private const int RuleTypeCategoryId = 1003;
        private const int OperatorCategoryId = 1004;
        private const int SortDirectionCategoryId = 1005;

        public static void Seed(ModelBuilder modelBuilder)
        {
            // Lookup Categories
            modelBuilder.Entity<LookupCategory>().HasData(
                Category(
                    DataTypeCategoryId,
                    "AttributeDataType",
                    "نوع بيانات المعيار",
                    "Attribute Data Type"),

                Category(
                    SourceTypeCategoryId,
                    "AttributeSourceType",
                    "مصدر بيانات المعيار",
                    "Attribute Source Type"),

                Category(
                    RuleTypeCategoryId,
                    "SuggestionRuleType",
                    "نوع قاعدة الاقتراح",
                    "Suggestion Rule Type"),

                Category(
                    OperatorCategoryId,
                    "ComparisonOperator",
                    "عملية المقارنة",
                    "Comparison Operator"),

                Category(
                    SortDirectionCategoryId,
                    "SortDirection",
                    "اتجاه الترتيب",
                    "Sort Direction")
            );

            // Lookup Items
            modelBuilder.Entity<LookupItem>().HasData(

                // Attribute Data Types
                Item(10001, DataTypeCategoryId,
                    "Number", "رقم", "Number", 1),

                Item(10002, DataTypeCategoryId,
                    "Text", "نص", "Text", 2),

                Item(10003, DataTypeCategoryId,
                    "Boolean", "منطقي", "Boolean", 3),

                Item(10004, DataTypeCategoryId,
                    "Lookup", "قائمة خيارات", "Lookup", 4),

                Item(10005, DataTypeCategoryId,
                    "Date", "تاريخ", "Date", 5),

                // Attribute Source Types
                Item(10006, SourceTypeCategoryId,
                    "DirectProperty", "خاصية مباشرة",
                    "Direct Property", 1),

                Item(10007, SourceTypeCategoryId,
                    "CalculatedProperty", "خاصية محسوبة",
                    "Calculated Property", 2),

                Item(10008, SourceTypeCategoryId,
                    "LookupProperty", "خاصية مرتبطة بقائمة",
                    "Lookup Property", 3),

                Item(10009, SourceTypeCategoryId,
                    "RelatedCollection", "بيانات مرتبطة",
                    "Related Collection", 4),

                // Suggestion Rule Types
                Item(10010, RuleTypeCategoryId,
                    "Filter", "تصفية", "Filter", 1),

                Item(10011, RuleTypeCategoryId,
                    "Sort", "ترتيب", "Sort", 2),

                // Comparison Operators
                Item(10012, OperatorCategoryId,
                    "Equal", "يساوي", "Equal", 1),

                Item(10013, OperatorCategoryId,
                    "NotEqual", "لا يساوي", "Not Equal", 2),

                Item(10014, OperatorCategoryId,
                    "GreaterThan", "أكبر من", "Greater Than", 3),

                Item(10015, OperatorCategoryId,
                    "LessThan", "أقل من", "Less Than", 4),

                Item(10016, OperatorCategoryId,
                    "GreaterThanOrEqual", "أكبر من أو يساوي",
                    "Greater Than or Equal", 5),

                Item(10017, OperatorCategoryId,
                    "LessThanOrEqual", "أقل من أو يساوي",
                    "Less Than or Equal", 6),

                Item(10018, OperatorCategoryId,
                    "Contains", "يحتوي على", "Contains", 7),

                // Sort Directions
                Item(10019, SortDirectionCategoryId,
                    "Ascending", "تصاعدي", "Ascending", 1),

                Item(10020, SortDirectionCategoryId,
                    "Descending", "تنازلي", "Descending", 2)
            );
        }

        // Helper Method for Lookup Categories
        private static LookupCategory Category(
            int id,
            string code,
            string arabicName,
            string englishName)
        {
            return new LookupCategory
            {
                Id = id,
                Code = code,
                ArabicName = arabicName,
                EnglishName = englishName,
                IsActive = true,
                IsDeleted = false
            };
        }

        // Helper Method for Lookup Items
        private static LookupItem Item(
            int id,
            int categoryId,
            string code,
            string arabicName,
            string englishName,
            int sortOrder)
        {
            return new LookupItem
            {
                Id = id,
                CategoryId = categoryId,
                Code = code,
                ArabicName = arabicName,
                EnglishName = englishName,
                SortOrder = sortOrder,
                IsActive = true,
                IsDeleted = false
            };
        }
    }
}

