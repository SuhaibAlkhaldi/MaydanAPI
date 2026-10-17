
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

    public async Task<Worker?> GetByIdAsync(
        int workerId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Workers
            .FirstOrDefaultAsync(w => w.Id == workerId, cancellationToken);
    }

    public async Task<Worker?> GetByIdWithServicesAsync(
        int workerId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Workers
            .AsNoTracking()
            .Where(w => w.Id == workerId)
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
                CountryId = w.CountryId,
                CityId = w.CityId,
                PhoneNumber = w.PhoneNumber,
                YearsOfExperience = w.YearsOfExperience,
                AssociationId = w.AssociationId,
                QrCode = w.QrCode,
                IsActive = w.IsActive,
                IsDeleted = w.IsDeleted,
                WorkerServices = w.WorkerServices
                    .Select(ws => new WorkerServiceLink
                    {
                        Service = new Service
                        {
                            NameEn = ws.Service.NameEn,
                            NameAr = ws.Service.NameAr
                        }
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Worker?> GetByIdIncludingDeletedAsync(
        int workerId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Workers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(w => w.Id == workerId, cancellationToken);
    }

    public async Task<Worker?> GetByCivilIdHashAsync(
        string civilIdHash,
        CancellationToken cancellationToken = default)
    {
        return await _context.Workers
            .FirstOrDefaultAsync(
                w => w.CivilIdHash == civilIdHash,
                cancellationToken);
    }

    public async Task<List<WorkerSummaryDto>> GetByAssociationIdAsync(
        int associationId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Workers
            .Where(w => w.AssociationId == associationId)
            .Select(w => new WorkerSummaryDto(
                w.Id,
                w.FirstName,
                w.LastName,
                w.PhoneNumber!,
                w.CivilId,
                w.WorkerServices
                    .Select(ws => ws.Service.NameEn)
                    .ToList(),
                w.WorkerServices
                    .Select(ws => ws.Service.NameAr)
                    .ToList(),
                w.YearsOfExperience,
                w.Association != null
                    ? w.Association.ArabicName ?? w.Association.EnglishName
                    : null
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Worker>> GetDeletedByAssociationIdAsync(
        int associationId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Workers
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(w => w.IsDeleted && w.AssociationId == associationId)
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
                QrCode = w.QrCode,
                IsActive = w.IsActive,
                IsDeleted = w.IsDeleted,
                Association = new Association
                  
                    {
                        ArabicName = w.Association.ArabicName,
                        EnglishName = w.Association.EnglishName
                    },
                WorkerServices = w.WorkerServices
                    .Select(ws => new WorkerServiceLink
                    {
                        Service = new Service
                        {
                            NameEn = ws.Service.NameEn,
                            NameAr = ws.Service.NameAr
                        }
                    })
                    .ToList()
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> AddAsync(
        Worker worker,
        CancellationToken cancellationToken = default)
    {
        await _context.Workers.AddAsync(worker, cancellationToken);
        return true;
    }

    public async Task<(List<WorkerSummaryDto> Items, int TotalCount)>
        GetAllProjectedAsync(
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
            query = query.Where(
                w => w.AssociationId == associationId.Value);
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
            query = query.Where(w =>
                w.WorkerServices.Any(
                    ws => ws.ServiceId == serviceId.Value));
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
                w.PhoneNumber!,
                w.CivilId,
                w.WorkerServices
                    .Select(ws => ws.Service.NameEn)
                    .ToList(),
                w.WorkerServices
                    .Select(ws => ws.Service.NameAr)
                    .ToList(),
                w.YearsOfExperience,
                w.Association != null
                    ? w.Association.ArabicName ?? w.Association.EnglishName
                    : null
            ))
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<WorkerDto?> GetByIdProjectedAsync(
        int workerId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Workers
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
                w.Country != null
                    ? w.Country.EnglishName
                    : null,
                w.CityId,
                w.City != null
                    ? w.City.EnglishName
                    : null,
                w.PhoneNumber,
                w.YearsOfExperience,
                w.AssociationId,
                w.Association.EnglishName,
                w.QrCode,
                w.WorkerServices
                    .Select(ws => ws.ServiceId)
                    .ToList(),
                w.WorkerServices
                    .Select(ws => ws.Service.NameEn)
                    .ToList(),
                w.IsActive
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<Worker>> QueryAsync(
        bool isDeleted,
        int? associationId = null,
        bool? orderByYearsOfExperienceAscending = null,
        bool? orderByNameEnAscending = null,
        bool? orderByNameArAscending = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Workers
            .AsNoTracking()
            .Where(w => w.IsDeleted == isDeleted);

        if (associationId.HasValue)
        {
            query = query.Where(
                w => w.AssociationId == associationId.Value);
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
                ? query.OrderBy(w => w.FirstName)
                    .ThenBy(w => w.LastName)
                : query.OrderByDescending(w => w.FirstName)
                    .ThenByDescending(w => w.LastName);
        }
        else if (orderByNameArAscending.HasValue)
        {
            // Uses the existing name properties because the Arabic
            // name property names are not shown in the provided entity.
            query = orderByNameArAscending.Value
                ? query.OrderBy(w => w.FirstName)
                    .ThenBy(w => w.LastName)
                : query.OrderByDescending(w => w.FirstName)
                    .ThenByDescending(w => w.LastName);
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
                Association = new Association
                    {
                        EnglishName = w.Association.EnglishName,
                        ArabicName = w.Association.ArabicName
                    },
                City = w.City == null
                    ? null
                    : new City
                    {
                        EnglishName = w.City.EnglishName,
                        ArabicName = w.City.ArabicName
                    },
                WorkerServices = w.WorkerServices
                    .Select(ws => new WorkerServiceLink
                    {
                        Service = new Service
                        {
                            NameEn = ws.Service.NameEn,
                            NameAr = ws.Service.NameAr
                        }
                    })
                    .ToList()
            })
            .ToListAsync(cancellationToken);
    }

    public Task<bool> Remove(
        Worker worker,
        CancellationToken cancellationToken = default)
    {
        _context.Workers.Remove(worker);
        return Task.FromResult(true);
    }
}
