using Maydan.Application.DTOs.Common;
using Maydan.Application.DTOs.WorkerSuggestions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maydan.Application.Interfaces
{
    public interface IWorkerSuggestionRuleService
    {
        Task<ApiResponse<IReadOnlyList<WorkerSuggestionRuleDto>>> GetAllAsync(
            int currentUserId,
            CancellationToken cancellationToken = default);

        Task<ApiResponse<IReadOnlyList<WorkerSuggestionRuleDto>>> GetDeletedAsync(
            int currentUserId,
            CancellationToken cancellationToken = default);

        Task<ApiResponse<WorkerSuggestionRuleDto>> GetByIdAsync(
            int currentUserId,
            int id,
            CancellationToken cancellationToken = default);

        Task<ApiResponse<WorkerSuggestionRuleDto>> CreateAsync(
            int currentUserId,
            CreateWorkerSuggestionRuleRequest request,
            CancellationToken cancellationToken = default);

        Task<ApiResponse<WorkerSuggestionRuleDto>> UpdateAsync(
            int currentUserId,
            int id,
            UpdateWorkerSuggestionRuleRequest request,
            CancellationToken cancellationToken = default);

        Task<ApiResponse<bool>> DeleteAsync(
            int currentUserId,
            int id,
            CancellationToken cancellationToken = default);

        Task<ApiResponse<WorkerSuggestionRuleDto>> RestoreAsync(
            int currentUserId,
            int id,
            CancellationToken cancellationToken = default);

        // Reorder rules
        Task<ApiResponse<bool>> ReorderAsync(
            int currentUserId,
            ReorderWorkerSuggestionRulesRequest request,
            CancellationToken cancellationToken = default);


        Task<ApiResponse<IReadOnlyList<WorkerSuggestionAttributeDto>>>
            GetAttributesAsync(
                int currentUserId,
                CancellationToken cancellationToken = default);

    }
}
