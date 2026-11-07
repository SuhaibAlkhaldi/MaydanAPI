using Maydan.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Maydan.Infrastructure.Persistence.Queries
{
    public static class WorkerSuggestionQueryBuilder
    {
        public static IQueryable<Worker> Apply(
            IQueryable<Worker> workers,
            IReadOnlyList<WorkerSuggestionRule> rules)
        {

            var activeRules = rules
                .Where(r => r.IsActive && !r.IsDeleted)
                // Ignore legacy sort rules that the query builder cannot execute
                .Where(r =>
                    r.RuleType?.Code != "Sort" ||
                    (r.AttributeKey is "YearsOfExperience" or "Age" or "MaritalStatus" or "NumberOfChildren"))
                .OrderBy(r => r.PriorityOrder)
                .ThenBy(r => r.Id)
                .ToList();


            // تطبيق كافة فلاتر التصفية أولاً
            foreach (var rule in activeRules
                .Where(r => r.RuleType?.Code == "Filter"))
            {
                workers = ApplyFilter(workers, rule);
            }

            IOrderedQueryable<Worker>? ordered = null;

            // تطبيق قواعد الترتيب حسب الأولوية
            foreach (var rule in activeRules
                .Where(r => r.RuleType?.Code == "Sort"))
            {
                ordered = ApplySort(workers, ordered, rule);
            }

            return ordered is null
                ? workers.OrderBy(w => w.Id)
                : ordered.ThenBy(w => w.Id);
        }

        private static IQueryable<Worker> ApplyFilter(
            IQueryable<Worker> workers,
            WorkerSuggestionRule rule)
        {
            var op = rule.Operator?.Code;

            if (rule.AttributeKey == "Age")
            {
                if (!int.TryParse(
                    rule.ComparisonValue,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out var ageValue))
                {
                    throw new InvalidOperationException(
                        "Invalid Age comparison value.");
                }

                var today = DateTime.UtcNow.Date;
                var targetDate = today.AddYears(-ageValue);

                return op switch
                {
                    "Equal" => workers.Where(w =>
                        w.DateOfBirth.HasValue &&
                        (today.Year - w.DateOfBirth.Value.Year -
                        (today.DayOfYear < w.DateOfBirth.Value.DayOfYear ? 1 : 0)) == ageValue),

                    "GreaterThan" => workers.Where(w =>
                        w.DateOfBirth.HasValue &&
                        w.DateOfBirth.Value < targetDate),

                    "LessThan" => workers.Where(w =>
                        w.DateOfBirth.HasValue &&
                        w.DateOfBirth.Value > targetDate),

                    "GreaterThanOrEqual" => workers.Where(w =>
                        w.DateOfBirth.HasValue &&
                        w.DateOfBirth.Value <= targetDate),

                    "LessThanOrEqual" => workers.Where(w =>
                        w.DateOfBirth.HasValue &&
                        w.DateOfBirth.Value >= targetDate),

                    "NotEqual" => workers.Where(w =>
                        w.DateOfBirth.HasValue &&
                        (today.Year - w.DateOfBirth.Value.Year -
                        (today.DayOfYear < w.DateOfBirth.Value.DayOfYear ? 1 : 0)) != ageValue),

                    _ => throw new InvalidOperationException(
                        "Unsupported age comparison operator.")
                };
            }

            if (rule.AttributeKey == "YearsOfExperience")
            {
                if (!int.TryParse(
                    rule.ComparisonValue,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out var value))
                {
                    throw new InvalidOperationException(
                        "Invalid YearsOfExperience comparison value.");
                }

                return op switch
                {
                    "Equal" => workers.Where(w =>
                        w.YearsOfExperience.HasValue &&
                        w.YearsOfExperience.Value == value),

                    "NotEqual" => workers.Where(w =>
                        w.YearsOfExperience.HasValue &&
                        w.YearsOfExperience.Value != value),

                    "GreaterThan" => workers.Where(w =>
                        w.YearsOfExperience.HasValue &&
                        w.YearsOfExperience.Value > value),

                    "LessThan" => workers.Where(w =>
                        w.YearsOfExperience.HasValue &&
                        w.YearsOfExperience.Value < value),

                    "GreaterThanOrEqual" => workers.Where(w =>
                        w.YearsOfExperience.HasValue &&
                        w.YearsOfExperience.Value >= value),

                    "LessThanOrEqual" => workers.Where(w =>
                        w.YearsOfExperience.HasValue &&
                        w.YearsOfExperience.Value <= value),

                    _ => throw new InvalidOperationException(
                        "Unsupported experience comparison operator.")
                };
            }

            if (rule.AttributeKey == "NumberOfChildren")
            {
                if (!int.TryParse(
                    rule.ComparisonValue,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out var childrenValue))
                {
                    throw new InvalidOperationException(
                        "Invalid NumberOfChildren comparison value.");
                }

                return op switch
                {
                    "Equal" => workers.Where(w =>
                        w.NumberOfChildren.HasValue &&
                        w.NumberOfChildren.Value == childrenValue),

                    "NotEqual" => workers.Where(w =>
                        w.NumberOfChildren.HasValue &&
                        w.NumberOfChildren.Value != childrenValue),

                    "GreaterThan" => workers.Where(w =>
                        w.NumberOfChildren.HasValue &&
                        w.NumberOfChildren.Value > childrenValue),

                    "LessThan" => workers.Where(w =>
                        w.NumberOfChildren.HasValue &&
                        w.NumberOfChildren.Value < childrenValue),

                    "GreaterThanOrEqual" => workers.Where(w =>
                        w.NumberOfChildren.HasValue &&
                        w.NumberOfChildren.Value >= childrenValue),

                    "LessThanOrEqual" => workers.Where(w =>
                        w.NumberOfChildren.HasValue &&
                        w.NumberOfChildren.Value <= childrenValue),

                    _ => throw new InvalidOperationException(
                        "Unsupported children comparison operator.")
                };
            }

            if (rule.AttributeKey == "HasDocuments")
            {
                if (!bool.TryParse(rule.ComparisonValue, out var required))
                    throw new InvalidOperationException(
                        "Invalid HasDocuments comparison value.");

                if (op == "NotEqual")
                    required = !required;
                else if (op != "Equal")
                    throw new InvalidOperationException(
                        "Unsupported document comparison operator.");

                return required
                    ? workers.Where(w => w.Documents.Any())
                    : workers.Where(w => !w.Documents.Any());
            }

            if (rule.AttributeKey == "Gender" ||
                rule.AttributeKey == "MaritalStatus")
            {
                var lookupId = rule.ComparisonLookupItemId;
                if (!lookupId.HasValue)
                {
                    throw new InvalidOperationException(
                        $"A lookup item ID is required for {rule.AttributeKey}.");
                }

                return op switch
                {
                    "Equal" => rule.AttributeKey == "Gender"
                        ? workers.Where(w => w.GenderId == lookupId.Value)
                        : workers.Where(w => w.MaritalStatusId == lookupId.Value),

                    "NotEqual" => rule.AttributeKey == "Gender"
                        ? workers.Where(w => w.GenderId != lookupId.Value)
                        : workers.Where(w => w.MaritalStatusId != lookupId.Value),

                    _ => throw new InvalidOperationException(
                        "Unsupported lookup comparison operator.")
                };
            }

            if (rule.AttributeKey == "Nationality")
            {
                var nationalityValue = rule.ComparisonValue;
                if (string.IsNullOrWhiteSpace(nationalityValue))
                {
                    throw new InvalidOperationException(
                        "A comparison value is required for Nationality.");
                }

                return op switch
                {
                    "Equal" => workers.Where(w => w.Nationality != null && w.Nationality == nationalityValue),
                    "NotEqual" => workers.Where(w => w.Nationality == null || w.Nationality != nationalityValue),
                    "Contains" => workers.Where(w => w.Nationality != null && w.Nationality.Contains(nationalityValue)),
                    _ => throw new InvalidOperationException(
                        "Unsupported nationality comparison operator.")
                };
            }

            throw new InvalidOperationException(
                $"Unsupported filter attribute: {rule.AttributeKey}");
        }

        private static IOrderedQueryable<Worker> ApplySort(
            IQueryable<Worker> workers,
            IOrderedQueryable<Worker>? ordered,
            WorkerSuggestionRule rule)
        {
            var direction = rule.SortDirection?.Code;

            if (direction is not ("Ascending" or "Descending"))
                throw new InvalidOperationException(
                    "Invalid sort direction.");

            bool asc = direction == "Ascending";

            switch (rule.AttributeKey)
            {
                case "YearsOfExperience":
                    {
                        var withNullOrder = ordered is null
                            ? workers.OrderBy(w => w.YearsOfExperience == null)
                            : ordered.ThenBy(w => w.YearsOfExperience == null);

                        return asc
                            ? withNullOrder.ThenBy(w => w.YearsOfExperience)
                            : withNullOrder.ThenByDescending(w => w.YearsOfExperience);
                    }

                case "Age":
                    {
                        var withNullOrder = ordered is null
                            ? workers.OrderBy(w => w.DateOfBirth == null)
                            : ordered.ThenBy(w => w.DateOfBirth == null);

                        return asc
                            ? withNullOrder.ThenByDescending(w => w.DateOfBirth)
                            : withNullOrder.ThenBy(w => w.DateOfBirth);
                    }

                case "MaritalStatus":
                    {
                        var withNullOrder = ordered is null
                            ? workers.OrderBy(w => w.MaritalStatusId == null)
                            : ordered.ThenBy(w => w.MaritalStatusId == null);

                        return asc
                            ? withNullOrder.ThenBy(w => w.MaritalStatus!.SortOrder)
                            : withNullOrder.ThenByDescending(w => w.MaritalStatus!.SortOrder);
                    }

                case "NumberOfChildren":
                    {
                        var withNullOrder = ordered is null
                            ? workers.OrderBy(w => w.NumberOfChildren == null)
                            : ordered.ThenBy(w => w.NumberOfChildren == null);

                        return asc
                            ? withNullOrder.ThenBy(w => w.NumberOfChildren)
                            : withNullOrder.ThenByDescending(w => w.NumberOfChildren);
                    }

                default:
                    throw new InvalidOperationException(
                        $"Unsupported sort attribute: {rule.AttributeKey}");
            }
        }
    }
}