using Maydan.Application.DTOs.Common;
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

    public async Task<ApiResponse<Worker?>> GetByIdAsync(int workerId, CancellationToken cancellationToken = default)
    {
        var worker = await _context.Workers.FirstOrDefaultAsync(w => w.Id == workerId, cancellationToken);
        if (worker is null)
        {
            return ApiResponse<Worker?>.FailureResponse("العامل غير موجود.", "Worker not found.");
        }
        return ApiResponse<Worker?>.SuccessResponse(worker, "تم جلب العامل بنجاح.", "Worker retrieved successfully.");
    }

    public async Task<ApiResponse<Worker?>> GetByIdWithServicesAsync(int workerId, CancellationToken cancellationToken = default)
    {
        var worker = await _context.Workers.Include(w => w.WorkerServices).FirstOrDefaultAsync(w => w.Id == workerId, cancellationToken);
        if (worker is null)
        {
            return ApiResponse<Worker?>.FailureResponse("العامل غير موجود.", "Worker not found.");
        }
        return ApiResponse<Worker?>.SuccessResponse(worker, "تم جلب العامل مع الخدمات بنجاح.", "Worker with services retrieved successfully.");
    }

    public async Task<ApiResponse<Worker?>> GetByIdIncludingDeletedAsync(int workerId, CancellationToken cancellationToken = default)
    {
        var worker = await _context.Workers.IgnoreQueryFilters().FirstOrDefaultAsync(w => w.Id == workerId, cancellationToken);
        if (worker is null)
        {
            return ApiResponse<Worker?>.FailureResponse("العامل غير موجود.", "Worker not found.");
        }
        return ApiResponse<Worker?>.SuccessResponse(worker, "تم جلب العامل بنجاح.", "Worker retrieved successfully.");
    }

    public async Task<ApiResponse<Worker?>> GetByCivilIdHashAsync(string civilIdHash, CancellationToken cancellationToken = default)
    {
        var worker = await _context.Workers.FirstOrDefaultAsync(w => w.CivilIdHash == civilIdHash, cancellationToken);
        if (worker is null)
        {
            return ApiResponse<Worker?>.FailureResponse("العامل غير موجود.", "Worker not found.");
        }
        return ApiResponse<Worker?>.SuccessResponse(worker, "تم جلب العامل بنجاح.", "Worker retrieved successfully.");
    }

    public async Task<ApiResponse<List<WorkerSummaryDto>>> GetByAssociationIdAsync(int associationId, CancellationToken cancellationToken = default)
    {
        var items = await _context.Workers
            .Where(w => w.AssociationId == associationId)
            .Select(w => new WorkerSummaryDto(
                w.Id,
                w.FirstName,
                w.LastName,
                w.PhoneNumber!,
                w.CivilId,
                w.WorkerServices.Select(ws => ws.Service.NameEn).ToList(),
                w.WorkerServices.Select(ws => ws.Service.NameAr).ToList(),
                w.YearsOfExperience,
                w.Association != null ? w.Association.ArabicName ?? w.Association.EnglishName : null
            ))
            .ToListAsync(cancellationToken);

        return ApiResponse<List<WorkerSummaryDto>>.SuccessResponse(items, "تم جلب عمال الجمعية بنجاح.", "Association workers retrieved successfully.");
    }

    public async Task<ApiResponse<List<Worker>>> GetDeletedByAssociationIdAsync(int associationId, CancellationToken cancellationToken = default)
    {
        var items = await _context.Workers.IgnoreQueryFilters()
            .Include(w => w.Association)
            .Include(w => w.WorkerServices)
                .ThenInclude(ws => ws.Service)
            .Where(w => w.IsDeleted && w.AssociationId == associationId)
            .ToListAsync(cancellationToken);

        return ApiResponse<List<Worker>>.SuccessResponse(items, "تم جلب العمال المحذوفين بنجاح.", "Deleted workers retrieved successfully.");
    }

    public async Task<ApiResponse<bool>> AddAsync(Worker worker, CancellationToken cancellationToken = default)
    {
        await _context.Workers.AddAsync(worker, cancellationToken);
        return ApiResponse<bool>.SuccessResponse(true, "تمت إضافة العامل بنجاح.", "Worker added successfully.");
    }

    public async Task<ApiResponse<(List<WorkerSummaryDto> Items, int TotalCount)>> GetAllProjectedAsync(
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
                w.PhoneNumber!,
                w.CivilId,
                w.WorkerServices.Select(ws => ws.Service.NameEn).ToList(),
                w.WorkerServices.Select(ws => ws.Service.NameAr).ToList(),
                w.YearsOfExperience,
                w.Association != null ? w.Association.ArabicName ?? w.Association.EnglishName : null
            ))
            .ToListAsync(cancellationToken);

        return ApiResponse<(List<WorkerSummaryDto> Items, int TotalCount)>.SuccessResponse((items, totalCount), "تم جلب بيانات العمال بنجاح.", "Workers retrieved successfully.");
    }

    public async Task<ApiResponse<WorkerDto?>> GetByIdProjectedAsync(int workerId, CancellationToken cancellationToken = default)
    {
        var workerDto = await _context.Workers
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

        if (workerDto is null)
        {
            return ApiResponse<WorkerDto?>.FailureResponse("العامل غير موجود.", "Worker not found.");
        }

        return ApiResponse<WorkerDto?>.SuccessResponse(workerDto, "تم جلب بيانات العامل بنجاح.", "Worker retrieved successfully.");
    }

    public async Task<ApiResponse<List<Worker>>> QueryAsync(
        bool isDeleted,
        int? associationId = null,
        bool? orderByYearsOfExperienceAscending = null,
        bool? orderByNameEnAscending = null,
        bool? orderByNameArAscending = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Workers
            .Include(w => w.Association)
            .Include(w => w.City)
            .Include(w => w.WorkerServices)
                .ThenInclude(ws => ws.Service)
            .Where(w => w.IsDeleted == isDeleted);

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
            query = orderByNameArAscending.Value
                ? query.OrderBy(w => w.FirstName).ThenBy(w => w.LastName)
                : query.OrderByDescending(w => w.FirstName).ThenByDescending(w => w.LastName);
        }

        var results = await query.ToListAsync(cancellationToken);
        return ApiResponse<List<Worker>>.SuccessResponse(results, "تم استعلام العمال بنجاح.", "Workers queried successfully.");
    }

    public Task<ApiResponse<bool>> Remove(Worker worker, CancellationToken cancellationToken = default)
    {
        _context.Workers.Remove(worker);
        return Task.FromResult(ApiResponse<bool>.SuccessResponse(true, "تمت إزالة العامل بنجاح.", "Worker removed successfully."));
    }
}