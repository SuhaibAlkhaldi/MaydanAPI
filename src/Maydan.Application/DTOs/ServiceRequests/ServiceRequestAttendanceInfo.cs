using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Maydan.Domain.Enums;
namespace Maydan.Application.DTOs.ServiceRequests;

    public sealed record  ServiceRequestAttendanceInfo
    (
    int Id,
    int StatusId,
    DateTime StartDate,
    DateTime EndDate,
    int AttendanceFrequency,
    int ShiftsCount,
    int ProductionCompanyId,
    int AssociationId,
    int ProjectId,
    int CityId,
    int LocationManagerUserId,
    CalculationType CalculationType,
    int ServiceId);


