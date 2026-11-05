using Maydan.Application.DTOs.ServiceRequests;
using Maydan.Application.Interfaces;
using Maydan.Domain.Common;
using Maydan.Domain.Entities;
using Maydan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Maydan.Infrastructure.Repositories;

public class ServiceRequestRepository : IServiceRequestRepository
{
    private readonly MaydanDbContext _context;

    public ServiceRequestRepository(MaydanDbContext context)
    {
        _context = context;
    }

  
    public async Task AddAsync(ServiceRequest serviceRequest, CancellationToken cancellationToken = default) =>
        await _context.Set<ServiceRequest>().AddAsync(serviceRequest, cancellationToken);

    public void Remove(ServiceRequest serviceRequest) => _context.Set<ServiceRequest>().Remove(serviceRequest);

    public Task<ServiceRequest?> GetForUpdateAsync(int id, CancellationToken ct = default)=> _context.ServiceRequests.FirstOrDefaultAsync(r => r.Id == id,ct) ;
    public Task<ServiceRequest?> GetForAssignmentAsync(int id, CancellationToken ct = default) =>_context.ServiceRequests.Include(r=> r.Service).FirstOrDefaultAsync(r=> r.Id== id,ct);

    public Task<ServiceRequestAttendanceInfo?> GetAttendanceInfoAsync(int id, CancellationToken ct = default) => _context.ServiceRequests.AsNoTracking()
        .Where(r => r.Id == id)
        .Select(r => new ServiceRequestAttendanceInfo(
            r.Id,
            r.StatusId,
            r.StartDate,
            r.EndDate,
            r.AttendanceFrequency,
            r.ShiftsCount,
            r.ProductionCompanyId,
            r.AssociationId,
            r.ProjectId,
            r.Association.CityId ?? r.Project.CityId,
            r.Project.LocationManagerUserId,
            r.Service.CalculationType,
            r.ServiceId))
        .FirstOrDefaultAsync(ct);
    public Task<ServiceRequestDetailsDto?> GetDetailsAsync(int id, CancellationToken ct = default) =>
    _context.ServiceRequests.AsNoTracking()
        .Where(r => r.Id == id)
        .Select(r => new ServiceRequestDetailsDto
        {
            Id = r.Id,
            ProductionCompanyId = r.ProductionCompanyId,   // NEW
            ServiceId = r.ServiceId,
            ServiceNameEn = r.Service.NameEn,
            ServiceNameAr = r.Service.NameAr,
            ProjectId = r.ProjectId,
            ProjectNameEn = r.Project.ProjectNameEn,
            ProjectNameAr = r.Project.ProjectNameAr,
            AssociationId = r.AssociationId,
            AssociationNameEn = r.Association.EnglishName,
            AssociationNameAr = r.Association.ArabicName,
            CityId = r.Association.CityId,
            StartDate = r.StartDate,
            EndDate = r.EndDate,
            ShiftsCount = r.ShiftsCount,
            RequestedWorkersCount = r.RequestedWorkersCount,
            SelectedWorkersCount = r.SelectedWorkersCount,
            AttendanceFrequency = r.AttendanceFrequency,
            UnitPriceSnapshot = r.UnitPriceSnapshot,
            ExpectedTotalAmount = r.ExpectedTotalAmount,
            StatusId = r.StatusId,
            StatusNameEn = r.Status.EnglishName,
            StatusNameAr = r.Status.ArabicName,
            RejectionReason = r.RejectionReason,
            CreatedAt = r.CreatedAt,
            CancelledAt = r.CancelledAt,
            AdditionalRequirements = r.AdditionalRequirements,
            AssignedWorkers = r.ServiceRequestWorkers.Select(srw => new AssignedWorkerDto(
                srw.Worker.Id,
                (srw.Worker.FirstName + " " + srw.Worker.LastName).Trim(),
                srw.Worker.PhoneNumber, srw.Worker.Nationality, srw.Worker.YearsOfExperience,
                srw.Worker.Documents.Select(d => d.FilePath).ToList())).ToList()
        })
        .FirstOrDefaultAsync(ct);
    public Task<List<ServiceRequestDto>> ListSummariesByProductionCompanyAsync(int id, CancellationToken ct = default) =>
    _context.ServiceRequests.AsNoTracking()
        .Where(s => s.ProductionCompanyId == id)
        .Select(s => new ServiceRequestDto
        {
            Id = s.Id,
            StatusId = s.StatusId,
            StatusNameEn = s.Status.EnglishName,
            StatusNameAr = s.Status.ArabicName
        })
        .ToListAsync(ct);
    public Task<List<ServiceRequestDto>> ListSummariesByAssociationAsync(int id, CancellationToken ct = default) =>
    _context.ServiceRequests.AsNoTracking()
        .Where(s => s.AssociationId == id)
        .Select(s => new ServiceRequestDto
        {
            Id = s.Id,
            StatusId = s.StatusId,
            StatusNameEn = s.Status.EnglishName,
            StatusNameAr = s.Status.ArabicName
        })
        .ToListAsync(ct);

    public Task<List<ServiceRequest>> ListByProjectForDetailsAsync(int projectId, CancellationToken ct = default) =>
        _context.ServiceRequests.AsNoTracking().AsSplitQuery()
            .Where(r => r.ProjectId == projectId)                       // الفلترة بالـ DB مش بالذاكرة
            .Include(r => r.Service)
            .Include(r => r.Association)
            .Include(r => r.ServiceRequestWorkers).ThenInclude(srw => srw.Worker)
            .ToListAsync(ct);
    public Task<List<ServiceRequest>> GetByProductionCompanyIdAsync(int productionCompanyId, CancellationToken cancellationToken = default) =>
        _context.Set<ServiceRequest>().Where(s => s.ProductionCompanyId == productionCompanyId).ToListAsync(cancellationToken);

    public Task<List<ServiceRequest>> GetByAssociationIdAsync(int associationId, CancellationToken cancellationToken = default) =>
        _context.Set<ServiceRequest>().Where(s => s.AssociationId == associationId).ToListAsync(cancellationToken);

    //public Task<List<ServiceRequest>> GetAllAsync(CancellationToken cancellationToken = default) =>
    //   _context.Set<ServiceRequest>()
    //    .Include(sr => sr.Service)
    //    .Include(sr => sr.Association)
    //    .Include(sr => sr.ServiceRequestWorkers)
    //        .ThenInclude(srw => srw.Worker)
    //    .ToListAsync(cancellationToken);

    public Task<List<ServiceRequest>> GetPendingOlderThanAsync(DateTime threshold, CancellationToken cancellationToken = default) =>
        _context.Set<ServiceRequest>()
            .Where(r => r.StatusId == LookupConstants.ServiceRequestStatus.PendingWorkerSelection && !r.ReminderSent && r.CreatedAt <= threshold)
            .Include(r => r.Association)
            .ToListAsync(cancellationToken);

    public async Task AssignToServiceRequestAsync(
    int serviceRequestId,
    int workerId,
    CancellationToken cancellationToken = default)
    {
        var assignment = new ServiceRequestWorker
        {

            ServiceRequestId = serviceRequestId,
            WorkerId = workerId
        };

        await _context.ServiceRequestWorkers.AddAsync(
            assignment,
            cancellationToken);
    }
    public async Task<List<ServiceRequest>> GetByProjectIdAsync(
      int projectId,
      CancellationToken cancellationToken = default)
    {
        return await _context.ServiceRequestWorkers
            .Where(srw => srw.ServiceRequest.ProjectId == projectId)
            .Select(srw => srw.ServiceRequest)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public Task<ServiceRequest?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
     _context.Set<ServiceRequest>()
        .Include(r => r.Status)
         .Include(r => r.Service)
         .Include(r => r.Project)
         .Include(r => r.Association)
         .Include(r => r.ProductionCompany)
         .Include(r => r.ServiceRequestWorkers)
             .ThenInclude(srw => srw.Worker)
                 .ThenInclude(w => w.Documents)
         .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
}
