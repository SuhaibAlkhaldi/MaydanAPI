using Maydan.Domain.Enums;

namespace Maydan.Application.DTOs.Services;

public record ServiceDto(
    int Id,
    string NameEn,
    string NameAr,
    CalculationType CalculationType,
    decimal Price,
    bool RequiresDocuments,
    bool IsActive
);

public class CreateServiceDto
{
    public string NameEn { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public CalculationType CalculationType { get; set; }
    public decimal Price { get; set; }
    public bool RequiresDocuments { get; set; }
}

// Partial update: every field is optional — a field left null (not sent) keeps its current
// stored value. Avoids forcing the caller to resend the full service just to change one column,
// and avoids two concurrent edits to different fields clobbering each other.
public class UpdateServiceDto
{
    public string? NameEn { get; set; }
    public string? NameAr { get; set; }
    public CalculationType? CalculationType { get; set; }
    public decimal? Price { get; set; }
    public bool? RequiresDocuments { get; set; }
    public bool? IsActive { get; set; }
}
