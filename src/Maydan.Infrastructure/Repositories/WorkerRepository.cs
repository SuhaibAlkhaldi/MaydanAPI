using Maydan.Application.DTOs.Workers;
using Maydan.Application.Interfaces;
using Maydan.Domain.Entities;
using Maydan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Maydan.Infrastructure.Repositories;

public class WorkerRepository : IWorkerRepository
{
    private readonly MaydanDbContext _context;

    public WorkerRepository(MaydanDbContext context)
    {
        _context = context;
    }

    public Task<Worker?> GetByIdAsync(int workerId, CancellationToken cancellationToken = default) =>
        _context.Workers.FirstOrDefaultAsync(w => w.Id == workerId, cancellationToken);

    public Task<Worker?> GetByIdWithServicesAsync(int workerId, CancellationToken cancellationToken = default) =>
        _context.Workers.Include(w => w.WorkerServices).FirstOrDefaultAsync(w => w.Id == workerId, cancellationToken);

    public Task<Worker?> GetByIdIncludingDeletedAsync(int workerId, CancellationToken cancellationToken = default) =>
        _context.Workers.IgnoreQueryFilters().FirstOrDefaultAsync(w => w.Id == workerId, cancellationToken);

    public Task<Worker?> GetByCivilIdHashAsync(string civilIdHash, CancellationToken cancellationToken = default) =>
        _context.Workers.FirstOrDefaultAsync(w => w.CivilIdHash == civilIdHash, cancellationToken);

    public Task<List<Worker>> GetByAssociationIdAsync(int associationId, CancellationToken cancellationToken = default) =>
        _context.Workers.Where(w => w.AssociationId == associationId).ToListAsync(cancellationToken);

    public Task<List<Worker>> GetDeletedByAssociationIdAsync(int associationId, CancellationToken cancellationToken = default) =>
        _context.Workers.IgnoreQueryFilters()
            .Where(w => w.IsDeleted && w.AssociationId == associationId)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Worker worker, CancellationToken cancellationToken = default) =>
        await _context.Workers.AddAsync(worker, cancellationToken);

    public void Remove(Worker worker) => _context.Workers.Remove(worker);

    public async Task<(List<WorkerSummaryDto> Items, int TotalCount)> GetAllProjectedAsync(
        int? associationId, string? search, int? serviceId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.Workers.AsQueryable();

        if (associationId.HasValue)
        {
            query = query.Where(w => w.AssociationId == associationId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(w =>
                w.FirstName.Contains(term) ||
                w.LastName.Contains(term) ||
                w.CivilId.Contains(term));
        }

        if (serviceId.HasValue)
        {
            query = query.Where(w => w.WorkerServices.Any(ws => ws.ServiceId == serviceId.Value));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(w => w.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(w => new WorkerSummaryDto(
                w.Id,
                w.FirstName,
                w.LastName,
                w.CivilId,
                w.WorkerServices.Select(ws => ws.Service.NameEn).ToList(),
                w.YearsOfExperience,
                w.PhoneNumber))
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<WorkerDto?> GetByIdProjectedAsync(int workerId, CancellationToken cancellationToken = default) =>
        _context.Workers
            .Where(w => w.Id == workerId)
            .Select(w => new WorkerDto(
                w.Id,
                w.FirstName,
                w.MiddleName,
                w.LastName,
                w.CivilId,
                w.DateOfBirth,
                w.Gender,
                w.MaritalStatus,
                w.Nationality,
                w.CountryId,
                w.Country != null ? w.Country.EnglishName : null,
                w.CityId,
                w.City != null ? w.City.EnglishName : null,
                w.PhoneNumber,
                w.YearsOfExperience,
                w.AssociationId,
                w.Association.EnglishName,
                w.QrCode,
                w.WorkerServices.Select(ws => ws.ServiceId).ToList(),
                w.WorkerServices.Select(ws => ws.Service.NameEn).ToList(),
                w.IsActive))
            .FirstOrDefaultAsync(cancellationToken);
}
