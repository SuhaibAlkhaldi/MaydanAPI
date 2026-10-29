using Maydan.Domain.Common;

namespace Maydan.Domain.Entities;

public class Worker : SharedEntities
{
    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string LastName { get; set; } = string.Empty;


    public string CivilId { get; set; } = string.Empty;

    public string CivilIdHash { get; set; } = string.Empty;

    public DateTime? DateOfBirth { get; set; }
    public string? PhoneNumber { get; set; }

    public int? GenderId { get; set; }
    public LookupItem? Gender { get; set; }

    public int? MaritalStatusId { get; set; }
    public LookupItem? MaritalStatus { get; set; }

    public string? Nationality { get; set; }

    public int? YearsOfExperience { get; set; }

    public int AssociationId { get; set; }
    public Association Association { get; set; } = null!;

    public string QrCode { get; set; } = string.Empty;

    public ICollection<WorkerServiceLink> WorkerServices { get; set; } = new List<WorkerServiceLink>();
    public ICollection<ServiceRequestWorker> ServiceRequestWorkers {  get; set; } = new HashSet<ServiceRequestWorker>();


    public ICollection<WorkerDocument> Documents { get; set; } = new List<WorkerDocument>();
    public ICollection<WorkerProjectLink> WorkerProjectLinks { get; set; }
}
