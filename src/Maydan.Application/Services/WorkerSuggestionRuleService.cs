
using System.Globalization;
using Maydan.Application.DTOs.Common;
using Maydan.Application.DTOs.WorkerSuggestions;
using Maydan.Application.Interfaces;
using Maydan.Application.WorkerSuggestions;
using Maydan.Domain.Entities;
using Maydan.Domain.Enums;

namespace Maydan.Application.Services;

public class WorkerSuggestionRuleService : IWorkerSuggestionRuleService
{
    private readonly IUnitOfWork _unitOfWork;

    private const string ManagePermission = "Manage Worker Suggestion Rules";

    public WorkerSuggestionRuleService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    // Get all non-deleted rules
    public async Task<ApiResponse<IReadOnlyList<WorkerSuggestionRuleDto>>>
        GetAllAsync(int currentUserId, CancellationToken cancellationToken = default)
    {
        var error = await AuthorizeAsync<IReadOnlyList<WorkerSuggestionRuleDto>>(currentUserId, cancellationToken);

        if (error is not null)
            return error;

        var rules = await _unitOfWork.WorkerSuggestionRules
            .GetAllAsync(cancellationToken);

        IReadOnlyList<WorkerSuggestionRuleDto> result =
            rules.Select(MapToDto).ToList();

        return ApiResponse<IReadOnlyList<WorkerSuggestionRuleDto>>.SuccessResponse(result, "Rules retrieved successfully.", "تم جلب القواعد بنجاح.");
    }

    // Get deleted rules
    public async Task<ApiResponse<IReadOnlyList<WorkerSuggestionRuleDto>>>
        GetDeletedAsync(int currentUserId, CancellationToken cancellationToken = default)
    {
        var error = await AuthorizeAsync<IReadOnlyList<WorkerSuggestionRuleDto>>(currentUserId, cancellationToken);

        if (error is not null)
            return error;

        var rules = await _unitOfWork.WorkerSuggestionRules
            .GetDeletedAsync(cancellationToken);

        IReadOnlyList<WorkerSuggestionRuleDto> result =
            rules.Select(MapToDto).ToList();

        return ApiResponse<IReadOnlyList<WorkerSuggestionRuleDto>>.SuccessResponse(result, "Deleted rules retrieved successfully.", "تم جلب القواعد المحذوفة بنجاح.");
    }

    // Get rule by ID
    public async Task<ApiResponse<WorkerSuggestionRuleDto>>
        GetByIdAsync(int currentUserId, int id, CancellationToken cancellationToken = default)
    {
        var error = await AuthorizeAsync<WorkerSuggestionRuleDto>(currentUserId, cancellationToken);

        if (error is not null)
            return error;

        var rule = await _unitOfWork.WorkerSuggestionRules
            .GetByIdAsync(id, cancellationToken);

        if (rule is null)
            return new ApiResponse<WorkerSuggestionRuleDto>(false, "القاعدة غير موجودة.", "Rule not found.", default, 404);

        return ApiResponse<WorkerSuggestionRuleDto>.SuccessResponse(MapToDto(rule), "Rule retrieved successfully.", "تم جلب القاعدة بنجاح.");
    }

    // Create
    public async Task<ApiResponse<WorkerSuggestionRuleDto>>
        CreateAsync(int currentUserId, CreateWorkerSuggestionRuleRequest request, CancellationToken cancellationToken = default)
    {
        var error = await AuthorizeAsync<WorkerSuggestionRuleDto>(currentUserId, cancellationToken);

        if (error is not null)
            return error;

        if (request is null || request.PriorityOrder <= 0)
            return ApiResponse<WorkerSuggestionRuleDto>.FailureResponse("Priority order must be greater than zero.", "يجب أن تكون أولوية القاعدة أكبر من صفر.");

        var (valid, errorEn, errorAr) = await ValidateRuleAsync(
            request.AttributeKey,
            request.RuleTypeId,
            request.OperatorId,
            request.SortDirectionId,
            request.ComparisonValue,
            request.ComparisonLookupItemId,
            cancellationToken);

        if (valid is null)
            return ApiResponse<WorkerSuggestionRuleDto>.FailureResponse(errorEn!, errorAr!);

        var rule = new WorkerSuggestionRule
        {
            AttributeKey = valid.AttributeKey,
            RuleTypeId = request.RuleTypeId,
            OperatorId = request.OperatorId,
            SortDirectionId = request.SortDirectionId,
            ComparisonValue = valid.ComparisonValue,
            ComparisonLookupItemId = null,
            PriorityOrder = request.PriorityOrder,
            IsActive = request.IsActive,
            CreatedBy = currentUserId
        };

        await _unitOfWork.WorkerSuggestionRules
            .AddAsync(rule, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<WorkerSuggestionRuleDto>.SuccessResponse(MapToDto(rule, valid), "Rule created successfully.", "تم إنشاء القاعدة بنجاح.", 201);
    }

    // Update

    public async Task<ApiResponse<WorkerSuggestionRuleDto>> UpdateAsync(
        int currentUserId,
        int id,
        UpdateWorkerSuggestionRuleRequest request,
        CancellationToken cancellationToken = default)
    {
        var error = await AuthorizeAsync<WorkerSuggestionRuleDto>(
            currentUserId, cancellationToken);

        if (error is not null)
            return error;

        if (request is null)
            return ApiResponse<WorkerSuggestionRuleDto>.FailureResponse(
                "Update request is required.",
                "بيانات التعديل مطلوبة.");

        var rule = await _unitOfWork.WorkerSuggestionRules
            .GetByIdAsync(id, cancellationToken);

        if (rule is null)
            return new ApiResponse<WorkerSuggestionRuleDto>(
                false,
                "القاعدة غير موجودة.",
                "Rule not found.",
                null,
                404);

        // Keep existing values when fields are not provided.
        var attributeKey = request.AttributeKey ?? rule.AttributeKey;
        var ruleTypeId = request.RuleTypeId ?? rule.RuleTypeId;
        var priorityOrder = request.PriorityOrder ?? rule.PriorityOrder;
        var isActive = request.IsActive ?? rule.IsActive;

        if (priorityOrder <= 0)
            return ApiResponse<WorkerSuggestionRuleDto>.FailureResponse(
                "Priority order must be greater than zero.",
                "يجب أن تكون أولوية القاعدة أكبر من صفر.");

        // Determine the resulting rule type.
        var ruleType = await _unitOfWork.WorkerSuggestionRules
            .GetActiveLookupItemAsync(
                ruleTypeId,
                "SuggestionRuleType",
                cancellationToken);

        if (ruleType is null)
            return ApiResponse<WorkerSuggestionRuleDto>.FailureResponse(
                "Invalid rule type.",
                "نوع القاعدة غير صحيح.");

        int? operatorId;
        int? sortDirectionId;
        string? comparisonValue;
        int? comparisonLookupItemId;

        if (ruleType.Code == "Sort")
        {
            if (request.OperatorId.HasValue ||
                request.ComparisonValue is not null ||
                request.ComparisonLookupItemId.HasValue)
            {
                return ApiResponse<WorkerSuggestionRuleDto>.FailureResponse(
                    "Sort rules cannot contain filter values.",
                    "قاعدة الترتيب لا تقبل قيم التصفية.");
            }

            operatorId = null;
            comparisonValue = null;
            comparisonLookupItemId = null;

            sortDirectionId =
                request.SortDirectionId ?? rule.SortDirectionId;
        }
        else if (ruleType.Code == "Filter")
        {
            if (request.SortDirectionId.HasValue)
            {
                return ApiResponse<WorkerSuggestionRuleDto>.FailureResponse(
                    "Filter rules cannot contain a sort direction.",
                    "قاعدة التصفية لا تقبل اتجاه ترتيب.");
            }

            sortDirectionId = null;

            operatorId = request.OperatorId ?? rule.OperatorId;

            comparisonValue =
                request.ComparisonValue ?? rule.ComparisonValue;

            comparisonLookupItemId =
                request.ComparisonLookupItemId
                ?? rule.ComparisonLookupItemId;
        }
        else
        {
            return ApiResponse<WorkerSuggestionRuleDto>.FailureResponse(
                "Unsupported rule type.",
                "نوع القاعدة غير مدعوم.");
        }

        // Validate the merged values.
        var (valid, errorEn, errorAr) = await ValidateRuleAsync(
            attributeKey,
            ruleTypeId,
            operatorId,
            sortDirectionId,
            comparisonValue,
            comparisonLookupItemId,
            cancellationToken);

        if (valid is null)
            return ApiResponse<WorkerSuggestionRuleDto>.FailureResponse(
                errorEn!,
                errorAr!);

        // Apply the validated values.
        rule.AttributeKey = valid.AttributeKey;
        rule.RuleTypeId = ruleTypeId;
        rule.OperatorId = operatorId;
        rule.SortDirectionId = sortDirectionId;
        rule.ComparisonValue = valid.ComparisonValue;
        rule.ComparisonLookupItemId = comparisonLookupItemId;
        rule.PriorityOrder = priorityOrder;
        rule.IsActive = isActive;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<WorkerSuggestionRuleDto>.SuccessResponse(
            MapToDto(rule, valid),
            "Rule updated successfully.",
            "تم تعديل القاعدة بنجاح.");
    }


    // Soft Delete
    public async Task<ApiResponse<bool>> DeleteAsync(int currentUserId, int id, CancellationToken cancellationToken = default)
    {
        var error = await AuthorizeAsync<bool>(
            currentUserId, cancellationToken);

        if (error is not null)
            return error;

        var rule = await _unitOfWork.WorkerSuggestionRules.GetByIdAsync(id, cancellationToken);

        if (rule is null)
            return new ApiResponse<bool>(false, "القاعدة غير موجودة.", "Rule not found.", default, 404);

        _unitOfWork.WorkerSuggestionRules.Delete(rule);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.SuccessResponse(true, "Rule deleted successfully.", "تم حذف القاعدة بنجاح.");
    }

    // Restore deleted rule
    public async Task<ApiResponse<WorkerSuggestionRuleDto>>
        RestoreAsync(
            int currentUserId,
            int id,
            CancellationToken cancellationToken = default)
    {
        var error = await AuthorizeAsync<WorkerSuggestionRuleDto>(
            currentUserId, cancellationToken);

        if (error is not null)
            return error;

        var rule = await _unitOfWork.WorkerSuggestionRules
            .GetDeletedByIdAsync(id, cancellationToken);

        if (rule is null)
            return new ApiResponse<WorkerSuggestionRuleDto>(false, "القاعدة المحذوفة غير موجودة.", "Deleted rule not found.", default, 404);

        var (valid, errorEn, errorAr) = await ValidateRuleAsync(
            rule.AttributeKey,
            rule.RuleTypeId,
            rule.OperatorId,
            rule.SortDirectionId,
            rule.ComparisonValue,
            rule.ComparisonLookupItemId,
            cancellationToken);

        if (valid is null)
            return ApiResponse<WorkerSuggestionRuleDto>.FailureResponse(errorEn!, errorAr!);

        rule.AttributeKey = valid.AttributeKey;
        rule.ComparisonValue = valid.ComparisonValue;
        rule.IsDeleted = false;
        rule.DeletedAt = null;

        // Restore as inactive until reviewed.
        rule.IsActive = false;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<WorkerSuggestionRuleDto>.SuccessResponse(MapToDto(rule, valid), "Rule restored successfully.", "تمت استعادة القاعدة بنجاح.");
    }

    // Reorder all non-deleted rules
    public async Task<ApiResponse<bool>> ReorderAsync(
        int currentUserId,
        ReorderWorkerSuggestionRulesRequest request,
        CancellationToken cancellationToken = default)
    {
        var error = await AuthorizeAsync<bool>(
            currentUserId, cancellationToken);

        if (error is not null)
            return error;

        var allRules = await _unitOfWork.WorkerSuggestionRules
            .GetAllAsync(cancellationToken);

        var ids = request?.OrderedRuleIds;

        if (ids is null ||
            ids.Count != allRules.Count ||
            ids.Distinct().Count() != ids.Count ||
            !ids.OrderBy(x => x).SequenceEqual(
                allRules.Select(x => x.Id).OrderBy(x => x)))
        {
            return ApiResponse<bool>.FailureResponse("OrderedRuleIds must contain every non-deleted rule exactly once.", "يجب إرسال جميع القواعد غير المحذوفة مرة واحدة دون تكرار.");
        }

        for (int i = 0; i < ids.Count; i++)
        {
            var rule = await _unitOfWork.WorkerSuggestionRules
                .GetByIdAsync(ids[i], cancellationToken);

            if (rule is null)
                return new ApiResponse<bool>(false, "إحدى القواعد غير موجودة.", "A rule was not found.", default, 404);

            rule.PriorityOrder = i + 1;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.SuccessResponse(true, "Rules reordered successfully.", "تم إعادة ترتيب القواعد بنجاح.");
    }

    // Validate attribute, rule type, operator and values.
    private async Task<(
        ValidatedRule? Value,
        string? ErrorEn,
        string? ErrorAr)> ValidateRuleAsync(
        string attributeKey,
        int ruleTypeId,
        int? operatorId,
        int? sortDirectionId,
        string? comparisonValue,
        int? comparisonLookupItemId,
        CancellationToken cancellationToken)
    {
        var attribute = WorkerAttributeCatalog.GetAll()
            .FirstOrDefault(x => string.Equals(
                x.Key,
                attributeKey?.Trim(),
                StringComparison.OrdinalIgnoreCase));

        if (attribute is null)
            return (null,
                "Unsupported worker attribute.",
                "معيار العامل غير مدعوم.");

        var ruleType = await _unitOfWork.WorkerSuggestionRules
            .GetActiveLookupItemAsync(
                ruleTypeId,
                "SuggestionRuleType",
                cancellationToken);

        if (ruleType is null)
            return (null,
                "Invalid rule type.",
                "نوع القاعدة غير صحيح.");

        // Sort Rule
        if (ruleType.Code == "Sort")
        {
            if (!attribute.IsSortable)
                return (null,
                    "Sorting is not allowed for this attribute.",
                    "الترتيب غير مسموح لهذا المعيار.");

            if (operatorId.HasValue ||
                !string.IsNullOrWhiteSpace(comparisonValue) ||
                comparisonLookupItemId.HasValue ||
                !sortDirectionId.HasValue)
            {
                return (null,
                    "Sort rules require only a sort direction.",
                    "قاعدة الترتيب تحتاج اتجاه ترتيب فقط.");
            }

            var direction = await _unitOfWork.WorkerSuggestionRules
                .GetActiveLookupItemAsync(
                    sortDirectionId.Value,
                    "SortDirection",
                    cancellationToken);

            if (direction is null ||
                (direction.Code != "Ascending" &&
                 direction.Code != "Descending"))
            {
                return (null,
                    "Invalid sort direction.",
                    "اتجاه الترتيب غير صحيح.");
            }

            return (new ValidatedRule(
                attribute.Key, ruleType, null,
                direction, null), null, null);
        }

        // Filter Rule
        if (ruleType.Code != "Filter" || !attribute.IsFilterable)
            return (null,
                "Filtering is not allowed for this attribute.",
                "التصفية غير مسموحة لهذا المعيار.");

        if (sortDirectionId.HasValue || !operatorId.HasValue)
            return (null,
                "Filter rules require an operator and no sort direction.",
                "قاعدة التصفية تحتاج عملية مقارنة دون اتجاه ترتيب.");

        // Lookup-based employment filters are not enabled.
        if (comparisonLookupItemId.HasValue)
            return (null,
                "Lookup-based filtering is not supported for the enabled attributes.",
                "التصفية حسب خيارات Lookup غير مدعومة للمعايير المفعلة.");

        var op = await _unitOfWork.WorkerSuggestionRules
            .GetActiveLookupItemAsync(
                operatorId.Value,
                "ComparisonOperator",
                cancellationToken);

        if (op is null)
            return (null,
                "Invalid comparison operator.",
                "عملية المقارنة غير صحيحة.");

        var value = comparisonValue?.Trim();

        if (string.IsNullOrWhiteSpace(value) || value.Length > 500)
            return (null,
                "A comparison value of 1 to 500 characters is required.",
                "قيمة المقارنة مطلوبة ويجب ألا تتجاوز 500 حرف.");

        var validOperators = attribute.DataTypeCode switch
        {
            "Number" => new[]
            {
                "Equal", "NotEqual",
                "GreaterThan", "LessThan",
                "GreaterThanOrEqual", "LessThanOrEqual"
            },
            "Boolean" => new[] { "Equal", "NotEqual" },
            "Text" => new[] { "Equal", "NotEqual", "Contains" },
            _ => Array.Empty<string>()
        };

        if (!validOperators.Contains(op.Code))
            return (null,
                "Operator is not supported for this attribute type.",
                "عملية المقارنة غير مدعومة لنوع هذا المعيار.");

        if (attribute.DataTypeCode == "Number" &&
            !decimal.TryParse(
                value,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out _))
        {
            return (null,
                "Comparison value must be numeric.",
                "يجب أن تكون قيمة المقارنة رقمية.");
        }

        if (attribute.DataTypeCode == "Boolean")
        {
            if (!bool.TryParse(value, out var parsed))
                return (null,
                    "Comparison value must be true or false.",
                    "يجب أن تكون قيمة المقارنة true أو false.");

            value = parsed ? "true" : "false";
        }

        return (new ValidatedRule(
            attribute.Key, ruleType, op,
            null, value), null, null);
    }

    // Authorization
    private async Task<ApiResponse<T>?> AuthorizeAsync<T>(
        int currentUserId,
        CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users
            .GetWithPermissionsAsync(
                currentUserId, cancellationToken);

        if (user is null)
            return new ApiResponse<T>(false, "المستخدم الحالي غير موجود.", "Current user not found.", default, 401);

        if (!user.IsActive)
            return new ApiResponse<T>(false, "حساب المستخدم غير فعال.", "User account is inactive.", default, 403);

        var rolePermissions = user.Role?.RolePermissions?
            .Where(x => x.IsActive && x.Permission?.IsActive == true)
            .Select(x => x.Permission.PermissionNameEn)
            ?? Enumerable.Empty<string>();

        var directPermissions = user.UserPermissions?
            .Where(x => x.IsActive && x.Permission?.IsActive == true)
            .Select(x => x.Permission.PermissionNameEn)
            ?? Enumerable.Empty<string>();

        var groupPermissions = user.UserGroups?
            .Where(x => x.Group != null && x.Group.IsActive)
            .SelectMany(x => x.Group.GroupPermissions)
            .Where(x => x.IsActive && x.Permission?.IsActive == true)
            .Select(x => x.Permission.PermissionNameEn)
            ?? Enumerable.Empty<string>();

        var hasPermission = rolePermissions
            .Concat(directPermissions)
            .Concat(groupPermissions)
            .Contains(ManagePermission,
                StringComparer.OrdinalIgnoreCase);

        if (user.EntityType != EntityType.BaytAlUrdon ||
            !hasPermission)
        {
            return new ApiResponse<T>(false, "لا تملك صلاحية إدارة قواعد اقتراح العمال.", "You are not authorized to manage suggestion rules.", default, 403);
        }

        return null;
    }

    private sealed record ValidatedRule(
        string AttributeKey,
        LookupItem RuleType,
        LookupItem? Operator,
        LookupItem? SortDirection,
        string? ComparisonValue);

    private static WorkerSuggestionRuleDto MapToDto(
        WorkerSuggestionRule rule)
    {
        return new WorkerSuggestionRuleDto(
            rule.Id,
            rule.AttributeKey,
            rule.RuleTypeId,
            rule.RuleType?.Code ?? string.Empty,
            rule.OperatorId,
            rule.Operator?.Code,
            rule.SortDirectionId,
            rule.SortDirection?.Code,
            rule.ComparisonValue,
            rule.ComparisonLookupItemId,
            rule.ComparisonLookupItem?.Code,
            rule.PriorityOrder,
            rule.IsActive,
            rule.IsDeleted,
            rule.DeletedAt);
    }

    // Map using already validated Lookup codes.
    private static WorkerSuggestionRuleDto MapToDto(
        WorkerSuggestionRule rule,
        ValidatedRule valid)
    {
        return new WorkerSuggestionRuleDto(
            rule.Id,
            rule.AttributeKey,
            rule.RuleTypeId,
            valid.RuleType.Code,
            rule.OperatorId,
            valid.Operator?.Code,
            rule.SortDirectionId,
            valid.SortDirection?.Code,
            rule.ComparisonValue,
            rule.ComparisonLookupItemId,
            null,
            rule.PriorityOrder,
            rule.IsActive,
            rule.IsDeleted,
            rule.DeletedAt);
    }
}
