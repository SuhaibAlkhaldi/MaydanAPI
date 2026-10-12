using Maydan.Domain.Common;
using Maydan.Domain.Enums;

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

    public Gender? Gender { get; set; }
    public MaritalStatus? MaritalStatus { get; set; }
    public string? Nationality { get; set; }

    public int? CountryId { get; set; }
    public Country? Country { get; set; }
    public int? CityId { get; set; }
    public City? City { get; set; }

    public int? YearsOfExperience { get; set; }

    public int AssociationId { get; set; }
    public Association Association { get; set; } = null!;

    public string QrCode { get; set; } = string.Empty;
    // Deliberately excluded: DailyWageAmount — belongs on the future ProjectWorker
    // assignment, not the worker themselves (deferred with the Service Request system).

    public ICollection<WorkerServiceLink> WorkerServices { get; set; } = new List<WorkerServiceLink>();
}
