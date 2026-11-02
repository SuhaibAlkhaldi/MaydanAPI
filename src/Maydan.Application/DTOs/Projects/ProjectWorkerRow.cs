namespace Maydan.Application.DTOs.Projects;

public sealed record ProjectWorkerRow(
    int WorkerProjectLinkId,
    int WorkerId,
    string FirstName,
    string LastName,
    string? CivilId,
    string? PhoneNumber,
    bool IsWorkerActive,
    decimal DailyWageAmount,
    int ServiceRequestId,
    string? ServiceNameEn,
    string? ServiceNameAr,
    decimal? ServicePrice,
    string? AssociationName,
    DateTime CreatedDate,
    DateTime ServiceStartDate,
    DateTime ServiceEndDate,
    int ServiceRequestStatusId);