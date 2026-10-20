using Maydan.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace Maydan.API.Models.Workers;

// Partial update: every field is optional — a field left out of the form keeps its current
// stored value. See UpdateWorkerDto's own comment for the full rule.
public class UpdateWorkerRequest
{
    public string? FirstName { get; set; }
    public string? MiddleName { get; set; }
    public string? LastName { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public Gender? Gender { get; set; }
    public MaritalStatus? MaritalStatus { get; set; }
    public string? Nationality { get; set; }
    public int? CountryId { get; set; }
    public int? CityId { get; set; }
    public string? PhoneNumber { get; set; }
    public List<int>? ServiceIds { get; set; }
    public int? YearsOfExperience { get; set; }

    // Newly uploaded files to append to the worker's existing documents (not a replace) — see
    // UpdateWorkerDto's own comment. Empty = no new documents this update.
    public List<IFormFile> Documents { get; set; } = new();
}
