using Maydan.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maydan.Application.Interfaces
{
    public interface IWorkerSuggestionRuleRepository
    {
        Task<IReadOnlyList<WorkerSuggestionRule>> GetAllAsync(
            CancellationToken cancellationToken = default);


        Task<IReadOnlyList<WorkerSuggestionRule>> GetDeletedAsync(
            CancellationToken cancellationToken = default);



        Task<IReadOnlyList<WorkerSuggestionRule>> GetActiveOrderedAsync(
            CancellationToken cancellationToken = default);

        Task<WorkerSuggestionRule?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default);


        Task<WorkerSuggestionRule?> GetDeletedByIdAsync(
            int id,
            CancellationToken cancellationToken = default);

        Task AddAsync(
            WorkerSuggestionRule rule,
            CancellationToken cancellationToken = default);

        void Update(WorkerSuggestionRule rule);

        void Delete(WorkerSuggestionRule rule);


        Task<LookupItem?> GetActiveLookupItemAsync(
            int itemId,
            string categoryCode,
            CancellationToken cancellationToken = default);

    }
}
