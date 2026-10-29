using Maydan.Application.DTOs.Workers;
using Maydan.Application.Interfaces;
using Maydan.Domain.Common;
using Maydan.Domain.Entities;
using Maydan.Domain.Enums;
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
        _context.Workers
            .Include(w => w.WorkerServices)
            .Include(w => w.Documents)
            .FirstOrDefaultAsync(w => w.Id == workerId, cancellationToken);

    public Task<Worker?> GetByIdIncludingDeletedAsync(int workerId, CancellationToken cancellationToken = default) =>
        _context.Workers.IgnoreQueryFilters()
            .Include(w => w.WorkerServices)
            .Include(w => w.Documents)
            .FirstOrDefaultAsync(w => w.Id == workerId, cancellationToken);

    public Task<Worker?> GetByCivilIdHashAsync(string civilIdHash, CancellationToken cancellationToken = default) =>
        _context.Workers.FirstOrDefaultAsync(w => w.CivilIdHash == civilIdHash, cancellationToken);

    public Task<Worker?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default) =>
        _context.Workers.FirstOrDefaultAsync(w => w.PhoneNumber == phoneNumber, cancellationToken);

    public Task<List<Worker>> GetByAssociationIdAsync(int associationId, CancellationToken cancellationToken = default) =>
        _context.Workers.Where(w => w.AssociationId == associationId).ToListAsync(cancellationToken);

    public Task<List<Worker>> GetDeletedByAssociationIdAsync(int associationId, CancellationToken cancellationToken = default) =>
        _context.Workers.IgnoreQueryFilters()
            .Where(w => w.IsDeleted && w.AssociationId == associationId)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Worker worker, CancellationToken cancellationToken = default) =>
        await _context.Workers.AddAsync(worker, cancellationToken);

    public void Remove(Worker worker)
    {
        // Documents/WorkerServices have a required, Restrict FK to Worker. Soft-deleting the
        // worker alone leaves them pointing at a "deleted" parent without being deleted
        // themselves, which EF's change tracker rejects as a severed required relationship
        // (removing them from worker.Documents/worker.WorkerServices isn't enough either — that
        // only clears the in-memory collection, it doesn't mark the entities themselves for
        // deletion). They must be loaded (callers use GetByIdWithServicesAsync) and explicitly
        // removed from their own DbSet so MaydanDbContext.SaveChangesAsync's soft-delete
        // conversion applies to them the same way it does to the worker.
        foreach (var document in worker.Documents)
        {
            _context.WorkerDocuments.Remove(document);
        }

        foreach (var link in worker.WorkerServices)
        {
            _context.WorkerServiceLinks.Remove(link);
        }

        _context.Workers.Remove(worker);
    }

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
                w.GenderId,
                w.Gender == null ? null : w.Gender.EnglishName,
                w.MaritalStatusId,
                w.MaritalStatus == null ? null : w.MaritalStatus.EnglishName,
                w.Nationality,
                w.Association.City.CountryId,
                w.Association.City.Country.EnglishName,
                w.Association.CityId,
                w.Association.City.EnglishName,
                w.PhoneNumber,
                w.YearsOfExperience,
                w.AssociationId,
                w.Association.EnglishName,
                w.QrCode,
                w.WorkerServices.Select(ws => ws.ServiceId).ToList(),
                w.WorkerServices.Select(ws => ws.Service.NameEn).ToList(),
                w.Documents.Select(d => d.FilePath).ToList(),
                w.IsActive))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<List<WorkerDto>> GetAvailableWorkersForRequestAsync(
     int associationId,
     int serviceId,
     DateTime startDate,
     DateTime endDate,
     CancellationToken cancellationToken = default)
    {
//Make sure if the service require uploading documents
var serviceRequiresDocs = await _context.Set<Service>()
            .Where(s => s.Id == serviceId)
            .Select(s => s.RequiresDocuments)
            .FirstOrDefaultAsync(cancellationToken);

       
        return await _context.Workers
            .AsNoTracking()
            .Where(w => w.AssociationId == associationId)
            .Where(w => w.WorkerServices.Any(ws => ws.ServiceId == serviceId))
            .Where(w => !serviceRequiresDocs || w.Documents.Any())
            .Where(w => !w.ServiceRequestWorkers.Any(srw =>
                srw.ServiceRequest.StatusId == LookupConstants.ServiceRequestStatus.InProgress &&
                srw.ServiceRequest.StartDate <= endDate &&
                srw.ServiceRequest.EndDate >= startDate))
            .Select(w => new WorkerDto(
                w.Id,
                w.FirstName,
                w.MiddleName,
                w.LastName,
                w.CivilId,
                w.DateOfBirth,
                w.GenderId,
                w.Gender == null ? null : w.Gender.EnglishName,
                w.MaritalStatusId,
                w.MaritalStatus == null ? null : w.MaritalStatus.EnglishName,
                w.Nationality,
                w.Association.City.CountryId,
                w.Association.City.Country.EnglishName,
                w.Association.CityId,
                w.Association.City.EnglishName,
                w.PhoneNumber,
                w.YearsOfExperience,
                w.AssociationId,
                w.Association.EnglishName,
                w.QrCode,
                w.WorkerServices.Select(ws => ws.ServiceId).ToList(),
                w.WorkerServices.Select(ws => ws.Service.NameEn).ToList(),
                w.Documents.Select(d => d.FilePath).ToList(),
                w.IsActive
            ))
            .ToListAsync(cancellationToken);
    }
    public Task<bool> IsWorkerBusyAsync(int workerId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default) =>
    _context.Workers.AnyAsync(w => w.Id == workerId &&
        w.ServiceRequestWorkers.Any(srw =>
            srw.ServiceRequest.StatusId == LookupConstants.ServiceRequestStatus.InProgress &&
            srw.ServiceRequest.StartDate <= endDate &&
            srw.ServiceRequest.EndDate >= startDate), cancellationToken);
}
