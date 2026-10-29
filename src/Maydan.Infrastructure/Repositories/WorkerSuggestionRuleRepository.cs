using Maydan.Application.Interfaces;
using Maydan.Domain.Entities;
using Maydan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maydan.Infrastructure.Repositories
{
    public class WorkerSuggestionRuleRepository
     : IWorkerSuggestionRuleRepository
    {
        private readonly MaydanDbContext _context;

        public WorkerSuggestionRuleRepository(
            MaydanDbContext context)
        {
            _context = context;
        }

        // Get all rules, including inactive ones.
        // Soft-deleted rules are excluded by the global filter.
        public async Task<IReadOnlyList<WorkerSuggestionRule>>
            GetAllAsync(
                CancellationToken cancellationToken = default)
        {
            return await QueryWithLookups()
                .AsNoTracking()
                .OrderBy(x => x.PriorityOrder)
                .ThenBy(x => x.Id)
                .ToListAsync(cancellationToken);
        }


        public async Task<IReadOnlyList<WorkerSuggestionRule>> GetDeletedAsync(
            CancellationToken cancellationToken = default)
        {
            return await QueryWithLookups()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(x => x.IsDeleted)
                .OrderByDescending(x => x.DeletedAt)
                .ThenBy(x => x.Id)
                .ToListAsync(cancellationToken);
        }


        // Get active rules ordered by priority.
        public async Task<IReadOnlyList<WorkerSuggestionRule>>
            GetActiveOrderedAsync(
                CancellationToken cancellationToken = default)
        {
            return await QueryWithLookups()
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.PriorityOrder)
                .ThenBy(x => x.Id)
                .ToListAsync(cancellationToken);
        }

        // Get rule by ID with change tracking enabled.
        public async Task<WorkerSuggestionRule?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            return await QueryWithLookups()
                .FirstOrDefaultAsync(
                    x => x.Id == id,
                    cancellationToken);
        }


        public async Task<WorkerSuggestionRule?> GetDeletedByIdAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            return await _context.WorkerSuggestionRules
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(
                    x => x.Id == id && x.IsDeleted,
                    cancellationToken);
        }


        // Add a new rule.
        public async Task AddAsync(
            WorkerSuggestionRule rule,
            CancellationToken cancellationToken = default)
        {
            await _context.WorkerSuggestionRules
                .AddAsync(rule, cancellationToken);
        }

        // Update an existing rule.
        public void Update(WorkerSuggestionRule rule)
        {
            _context.WorkerSuggestionRules.Update(rule);
        }

        // Delete rule using the project's soft-delete mechanism.
        public void Delete(WorkerSuggestionRule rule)
        {
            _context.WorkerSuggestionRules.Remove(rule);
        }

        // Shared query with Lookup navigation properties.
        private IQueryable<WorkerSuggestionRule> QueryWithLookups()
        {
            return _context.WorkerSuggestionRules
                .Include(x => x.RuleType)
                .Include(x => x.Operator)
                .Include(x => x.SortDirection)
                .Include(x => x.ComparisonLookupItem);
        }


        public async Task<LookupItem?> GetActiveLookupItemAsync(
            int itemId,
            string categoryCode,
            CancellationToken cancellationToken = default)
        {
            return await _context.LookupItems
                .AsNoTracking()
                .Where(x =>
                    x.Id == itemId &&
                    x.IsActive &&
                    x.Category.IsActive &&
                    x.Category.Code == categoryCode)
                .FirstOrDefaultAsync(cancellationToken);
        }

    }
}
