using Maydan.Application.DTOs.Common;
using Maydan.Application.DTOs.Lookups;
using Maydan.Application.Interfaces;

namespace Maydan.Application.Services;

public class LookupService : ILookupService
{
    private readonly IUnitOfWork _unitOfWork;

    public LookupService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<List<LookupItemDto>>> GetByCategoryAsync(string categoryCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(categoryCode))
        {
            return ApiResponse<List<LookupItemDto>>.FailureResponse("Category code is required.", "رمز الفئة مطلوب.");
        }

        var items = await _unitOfWork.Lookups.GetItemsByCategoryCodeAsync(categoryCode.Trim(), cancellationToken);
        if (items.Count == 0)
        {
            return ApiResponse<List<LookupItemDto>>.FailureResponse("Lookup category not found.", "فئة القيم غير موجودة.");
        }

        var dtos = items
            .Select(i => new LookupItemDto(i.Id, i.Code, i.ArabicName, i.EnglishName))
            .ToList();

        return ApiResponse<List<LookupItemDto>>.SuccessResponse(dtos, "Lookup items retrieved successfully.", "تم جلب القيم بنجاح.");
    }
}
