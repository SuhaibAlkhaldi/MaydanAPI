using Maydan.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                .OrderBy(r => r.PriorityOrder)
                .ThenBy(r => r.Id)
                .ToList();

            // Apply all filters before sorting.
            foreach (var rule in activeRules
                .Where(r => r.RuleType?.Code == "Filter"))
            {
                workers = ApplyFilter(workers, rule);
            }

            IOrderedQueryable<Worker>? ordered = null;

            // Apply sort rules in priority order.
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

                // Workers with missing experience are excluded.
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

            // إضافة معالجة حقول الـ Lookup (مثل Gender و MaritalStatus)
            if (rule.AttributeKey == "Gender" || rule.AttributeKey == "MaritalStatus")
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
                        // Null values are always last.
                        var withNullOrder = ordered is null
                            ? workers.OrderBy(w =>
                                w.YearsOfExperience == null)
                            : ordered.ThenBy(w =>
                                w.YearsOfExperience == null);

                        return asc
                            ? withNullOrder.ThenBy(w =>
                                w.YearsOfExperience)
                            : withNullOrder.ThenByDescending(w =>
                                w.YearsOfExperience);
                    }

                case "FirstName":
                    return ordered is null
                        ? (asc
                            ? workers.OrderBy(w => w.FirstName)
                            : workers.OrderByDescending(w => w.FirstName))
                        : (asc
                            ? ordered.ThenBy(w => w.FirstName)
                            : ordered.ThenByDescending(w => w.FirstName));

                case "LastName":
                    return ordered is null
                        ? (asc
                            ? workers.OrderBy(w => w.LastName)
                            : workers.OrderByDescending(w => w.LastName))
                        : (asc
                            ? ordered.ThenBy(w => w.LastName)
                            : ordered.ThenByDescending(w => w.LastName));

                case "FullName":
                    {
                        var byFirstName = ordered is null
                            ? (asc
                                ? workers.OrderBy(w => w.FirstName)
                                : workers.OrderByDescending(w => w.FirstName))
                            : (asc
                                ? ordered.ThenBy(w => w.FirstName)
                                : ordered.ThenByDescending(w => w.FirstName));

                        return asc
                            ? byFirstName.ThenBy(w => w.LastName)
                            : byFirstName.ThenByDescending(w => w.LastName);
                    }

                default:
                    throw new InvalidOperationException(
                        $"Unsupported sort attribute: {rule.AttributeKey}");
            }
        }
    }
}
