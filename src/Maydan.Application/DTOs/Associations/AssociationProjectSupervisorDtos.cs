namespace Maydan.Application.DTOs.Associations;

// Association Management, Phase 2e — field names match workforcment's own
// association-project-supervisor.model.ts exactly (System.Text.Json's default camelCase policy
// turns AssociationUserId into associationUserId, etc.). AssociationUserId is a real User.UserId —
// specifically the Association-role user assigned as supervisor — not a reference to any separate
// link-table row; there is no AssociationUser entity in this codebase (see AssociationService.cs's
// own header comment and Phase 2b's investigation). Named AssociationUserId here (not plain
// UserId) only because that's the exact JSON property association-project-supervisor.model.ts
// already expects; the C# property could have been named UserId with a [JsonPropertyName]
// override, but matching the wire name directly keeps this DTO boring to read.
public class AssociationProjectSupervisorDto
{
    public int Id { get; set; }
    public int AssociationUserId { get; set; }
    public int ProjectId { get; set; }

    // Enrichment fields the real list view (project-supervisors.component.ts) actually renders —
    // resolved server-side, same "don't make the frontend do a second round-trip" shape
    // AssociationDto/CityLocationDto already use for their own related-entity names.
    public int? AssociationId { get; set; }
    public string? ProjectName { get; set; }
    public string? AssociationName { get; set; }
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
}

public class CreateAssociationProjectSupervisorDto
{
    public int AssociationUserId { get; set; }
    public int ProjectId { get; set; }
}
public class PagedAssociationProjectSupervisorDto
{
    public List<AssociationProjectSupervisorDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
}
