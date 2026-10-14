using Maydan.Domain.Entities;

namespace Maydan.Application.Interfaces;

public interface IProjectTypeRepository
{
    Task<bool> ExistsAsync(
        int projectTypeId,
        CancellationToken cancellationToken = default);

    Task<List<ProjectType>> GetAllAsync(
        CancellationToken cancellationToken = default);
}
