using Maydan.Application.Interfaces;
using Maydan.Domain.Entities;
using Maydan.Domain.Enums;
using Maydan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Maydan.Infrastructure.Repositories;

public class AssociationProjectSupervisorRepository : IAssociationProjectSupervisorRepository
{
    private readonly MaydanDbContext _context;

    public AssociationProjectSupervisorRepository(MaydanDbContext context)
    {
        _context = context;
    }

    public Task<List<AssociationProjectSupervisor>> GetAllAsync(int? projectId, int? associationId, CancellationToken cancellationToken = default)
    {
  
        var query = _context.AssociationProjectSupervisors
            .AsNoTracking()
            .Select(s => new AssociationProjectSupervisor
            {
                Id = s.Id,
                Project = new Project
                {
                    Id = s.Project.Id,
                    ProjectNameEn = s.Project.ProjectNameEn,
                    ProjectNameAr = s.Project.ProjectNameAr,
                    StartDate = s.Project.StartDate,
                    EndDate = s.Project.EndDate,
                    ProjectTypeId = s.Project.ProjectTypeId,
                    ProducerUserId = s.Project.ProducerUserId,
                    LocationManagerUserId = s.Project.LocationManagerUserId,
                    WorkPermitImagePath = s.Project.WorkPermitImagePath,
                    ProductionCompanyId = s.Project.ProductionCompanyId
                },
                User = new User
                {
                    UserId = s.User.UserId,
                    FirstNameAr = s.User.FirstNameAr,
                    LastNameAr = s.User.LastNameAr,
                    FirstNameEn = s.User.FirstNameEn,
                    LastNameEn = s.User.LastNameEn,
                    Email = s.User.Email,
                    PhoneNumber = s.User.PhoneNumber,                    
                },
                CreatedAt = s.CreatedAt,
                CreatedBy = s.CreatedBy,
                IsActive = s.IsActive
            })
            //.Include(s => s.Project)
            //.Include(s => s.User)
            .AsQueryable();

        if (projectId is not null)
        {
            query = query.Where(s => s.ProjectId == projectId);
        }

        if (associationId is not null)
        {
            query = query.Where(s => s.User.EntityType == EntityType.Association && s.User.EntityId == associationId);
        }

        return query.OrderByDescending(s => s.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task<AssociationProjectSupervisor?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await _context.AssociationProjectSupervisors
            .Include(s => s.Project)
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task AddAsync(AssociationProjectSupervisor supervisor, CancellationToken cancellationToken = default) =>
        await _context.AssociationProjectSupervisors.AddAsync(supervisor, cancellationToken);

    public Task<bool> Remove(AssociationProjectSupervisor supervisor )
    {
        _context.AssociationProjectSupervisors.Remove(supervisor);
        return Task.FromResult(true);
    }
}
