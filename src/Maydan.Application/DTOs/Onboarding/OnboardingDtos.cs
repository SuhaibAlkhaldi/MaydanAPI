namespace Maydan.Application.DTOs.Onboarding;

// Entity onboarding Stage 2 (2026-09-22): Bayt-AlUrdon picking an existing, admin-less Association
// and creating its first admin. No FirstNameAr/LastNameAr workaround like Stage 1's — this is
// filled in directly by Bayt-AlUrdon staff, not a self-registering outsider, so real Arabic name
// fields are required the same way CreateEntityUserDto already requires them.
public class OnboardAssociationAdminDto
{
    public string FirstNameEn { get; set; } = string.Empty;
    public string LastNameEn { get; set; } = string.Empty;
    public string FirstNameAr { get; set; } = string.Empty;
    public string LastNameAr { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string InitialPassword { get; set; } = string.Empty;
}

public record AssociationWithoutAdminDto(
    int AssociationId,
    string EnglishName,
    string ArabicName,
    int? CityId);
