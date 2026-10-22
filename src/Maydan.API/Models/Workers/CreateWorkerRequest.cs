using Maydan.Domain.Enums;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Maydan.API.Models.Workers;

public class CreateWorkerRequest
{
    public int? AssociationId { get; set; }

    [Required]
    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }

    [Required]
    public string LastName { get; set; } = string.Empty;

    [Required]
    public string CivilId { get; set; } = string.Empty;

    [Required]
    public DateTime DateOfBirth { get; set; }

    [Required]
    public Gender Gender { get; set; }

    [Required]
    public MaritalStatus MaritalStatus { get; set; }

    [Required]
    public string Nationality { get; set; } = string.Empty;

    [Required]
    public int CountryId { get; set; }

    [Required]
    public int CityId { get; set; }

    [Required]
    public string PhoneNumber { get; set; } = string.Empty;

    public List<int> ServiceIds { get; set; } = new();

    [Required]
    public int YearsOfExperience { get; set; }

    // Required (1-4 files) only when ServiceIds includes a service flagged
    // Service.RequiresDocuments — enforced in WorkerService, not here, since this controller
    // has no way to know which services require them. No notion of document "type" (see
    // WorkerDocument's own comment).
    public List<IFormFile> Documents { get; set; } = new();
}
