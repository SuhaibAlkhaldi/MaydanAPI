namespace Maydan.Application.DTOs.Lookups;

public record LookupItemDto(
    int Id,
    string Code,
    string ArabicName,
    string EnglishName);
