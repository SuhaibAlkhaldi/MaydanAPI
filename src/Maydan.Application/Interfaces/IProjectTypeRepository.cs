using Maydan.Domain.Entities;

namespace Maydan.Application.Interfaces;

public interface IProjectTypeRepository
{

    public interface IProjectTypeRepository
    {
        Task<bool> ExistsAsync(
            int projectTypeId,
            CancellationToken cancellationToken = default);

        Task<List<Maydan.Domain.Entities.ProjectType>> GetAllAsync(
            CancellationToken cancellationToken = default);
    }

    Task<bool> ExistsAsync(
        int projectTypeId,
        CancellationToken cancellationToken = default);


    Task<List<ProjectType>> GetAllAsync(
        CancellationToken cancellationToken = default);
}
