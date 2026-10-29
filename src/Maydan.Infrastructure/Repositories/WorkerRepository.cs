using Maydan.Application.DTOs.Workers;
using Maydan.Application.Interfaces;
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

    public Task<Worker?> GetByIdAsync(
        int workerId,
        CancellationToken cancellationToken = default) =>
        _context.Workers.FirstOrDefaultAsync(w => w.Id == workerId, cancellationToken);

    // Return a tracked entity with real WorkerServiceLink IDs and loaded documents.
    // The assignment service needs ServiceId; delete workflows need tracked children.
    public Task<Worker?> GetByIdWithServicesAsync(
        int workerId,
        CancellationToken cancellationToken = default) =>
        _context.Workers
            .Include(w => w.WorkerServices)
                .ThenInclude(ws => ws.Service)
            .Include(w => w.Documents)
            .FirstOrDefaultAsync(w => w.Id == workerId, cancellationToken);

    public Task<Worker?> GetByIdIncludingDeletedAsync(
        int workerId,
        CancellationToken cancellationToken = default) =>
        _context.Workers
            .IgnoreQueryFilters()
            .Include(w => w.WorkerServices)
            .Include(w => w.Documents)
            .FirstOrDefaultAsync(w => w.Id == workerId, cancellationToken);

    public Task<Worker?> GetByCivilIdHashAsync(
        string civilIdHash,
        CancellationToken cancellationToken = default) =>
        _context.Workers.FirstOrDefaultAsync(w => w.CivilIdHash == civilIdHash, cancellationToken);

    public Task<Worker?> GetByPhoneNumberAsync(
        string phoneNumber,
        CancellationToken cancellationToken = default) =>
        _context.Workers.FirstOrDefaultAsync(w => w.PhoneNumber == phoneNumber, cancellationToken);

    // Used to display association workers and their services.
    public Task<List<Worker>> GetByAssociationIdAsync(
        int associationId,
        CancellationToken cancellationToken = default) =>
        _context.Workers
            .Include(w => w.WorkerServices)
                .ThenInclude(ws => ws.Service)
            .Where(w => w.AssociationId == associationId)
            .ToListAsync(cancellationToken);

    // Keep tracking enabled: AssociationService.RestoreAsync modifies the returned workers.
    public Task<List<Worker>> GetDeletedByAssociationIdAsync(
        int associationId,
        CancellationToken cancellationToken = default) =>
        _context.Workers
            .IgnoreQueryFilters()
            .Where(w => w.IsDeleted && w.AssociationId == associationId)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(
        Worker worker,
        CancellationToken cancellationToken = default) =>
        await _context.Workers.AddAsync(worker, cancellationToken);

    // Match the merged IWorkerRepository: Task<bool>, not void.
    // Load children here because some callers supply a worker without populated navigations.
    public async Task<bool> Remove(
        Worker worker,
        CancellationToken cancellationToken = default)
    {
        var trackedWorker = await _context.Workers
            .Include(w => w.WorkerServices)
            .Include(w => w.Documents)
            .FirstOrDefaultAsync(w => w.Id == worker.Id, cancellationToken);

        if (trackedWorker is null)
        {
            return false;
        }

        foreach (var document in trackedWorker.Documents.ToList())
        {
            _context.WorkerDocuments.Remove(document);
        }

        foreach (var link in trackedWorker.WorkerServices.ToList())
        {
            _context.WorkerServiceLinks.Remove(link);
        }

        _context.Workers.Remove(trackedWorker);
        return true;
    }

    public async Task<(List<WorkerSummaryDto> Items, int TotalCount)> GetAllProjectedAsync(
        int? associationId,
        string? search,
        int? serviceId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
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

    public Task<WorkerDto?> GetByIdProjectedAsync(
        int workerId,
        CancellationToken cancellationToken = default) =>
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

    // Keep the master's document requirement, the confirmed IsActive rule,
    // and the WorkerDto return type declared in IWorkerRepository.
    public async Task<List<WorkerDto>> GetAvailableWorkersForRequestAsync(
        int associationId,
        int serviceId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        var serviceRequiresDocs = await _context.Set<Service>()
            .Where(s => s.Id == serviceId)
            .Select(s => s.RequiresDocuments)
            .FirstOrDefaultAsync(cancellationToken);

        return await _context.Workers
            .AsNoTracking()
            .Where(w => w.AssociationId == associationId && w.IsActive)
            .Where(w => w.WorkerServices.Any(ws => ws.ServiceId == serviceId))
            .Where(w => !serviceRequiresDocs || w.Documents.Any())
            .Where(w => !w.ServiceRequestWorkers.Any(srw =>
                srw.ServiceRequest.Status == ServiceRequestStatus.InProgress &&
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
                w.IsActive))
            .ToListAsync(cancellationToken);
    }


    public async Task<List<Worker>> GetByAssociationIdForUpdateAsync(
        int associationId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Workers
            .AsTracking()
            .Where(w => w.AssociationId == associationId)
            .ToListAsync(cancellationToken);
    }


    // Retain the current branch's worker query/sort API.
    public async Task<List<Worker>> QueryAsync(
        bool isDeleted,
        int? associationId = null,
        bool? orderByYearsOfExperienceAscending = null,
        bool? orderByNameEnAscending = null,
        bool? orderByNameArAscending = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Worker> query = isDeleted
            ? _context.Workers.IgnoreQueryFilters()
            : _context.Workers;

        query = query.AsNoTracking().Where(w => w.IsDeleted == isDeleted);

        if (associationId.HasValue)
        {
            query = query.Where(w => w.AssociationId == associationId.Value);
        }

        if (orderByYearsOfExperienceAscending.HasValue)
        {
            query = orderByYearsOfExperienceAscending.Value
                ? query.OrderBy(w => w.YearsOfExperience)
                : query.OrderByDescending(w => w.YearsOfExperience);
        }
        else if (orderByNameEnAscending.HasValue)
        {
            query = orderByNameEnAscending.Value
                ? query.OrderBy(w => w.FirstName).ThenBy(w => w.LastName)
                : query.OrderByDescending(w => w.FirstName).ThenByDescending(w => w.LastName);
        }
        else if (orderByNameArAscending.HasValue)
        {
            // Preserve the existing fallback to FirstName/LastName until
            // distinct Arabic-name properties are confirmed in the entity.
            query = orderByNameArAscending.Value
                ? query.OrderBy(w => w.FirstName).ThenBy(w => w.LastName)
                : query.OrderByDescending(w => w.FirstName).ThenByDescending(w => w.LastName);
        }

        return await query
            .Select(w => new Worker
            {
                Id = w.Id,
                FirstName = w.FirstName,
                MiddleName = w.MiddleName,
                LastName = w.LastName,
                CivilId = w.CivilId,
                DateOfBirth = w.DateOfBirth,
                Gender = w.Gender,
                MaritalStatus = w.MaritalStatus,
                Nationality = w.Nationality,
                PhoneNumber = w.PhoneNumber,
                YearsOfExperience = w.YearsOfExperience,
                AssociationId = w.AssociationId,
                QrCode = w.QrCode,
                IsActive = w.IsActive,
                IsDeleted = w.IsDeleted,
                Association = new Association
                {
                    EnglishName = w.Association.EnglishName,
                    ArabicName = w.Association.ArabicName
                },
                WorkerServices = w.WorkerServices
                    .Select(ws => new WorkerServiceLink
                    {
                        WorkerId = ws.WorkerId,
                        ServiceId = ws.ServiceId,
                        Service = new Service
                        {
                            Id = ws.Service.Id,
                            NameEn = ws.Service.NameEn,
                            NameAr = ws.Service.NameAr
                        }
                    })
                    .ToList()
            })
            .ToListAsync(cancellationToken);
    }
}
