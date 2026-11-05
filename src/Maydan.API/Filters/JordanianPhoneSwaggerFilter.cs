
using Maydan.API.Models.Workers;
using Maydan.Application.DTOs.Auth;
using Maydan.Application.DTOs.Onboarding;
using Maydan.Application.DTOs.UserManagement;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Maydan.API.Filters;

public class JordanianPhoneSwaggerFilter : ISchemaFilter, IOperationFilter
{
    public void Apply(
        OpenApiSchema schema,
        SchemaFilterContext context)
    {
        var type = context.Type;

        if (type == typeof(CreateEntityUserDto) ||
            type == typeof(OnboardAssociationAdminDto) ||
            type == typeof(CreateWorkerRequest) ||
            type == typeof(UpdateWorkerRequest))
        {
            SetPhoneExample(schema, "phoneNumber", "+962");
        }

        if (type == typeof(RegisterProductionCompanyDto))
        {
            SetPhoneExample(schema, "mobileCountryCode", "+962");
            SetPhoneExample(schema, "mobileNumber", "");
        }
    }

    public void Apply(
        OpenApiOperation operation,
        OperationFilterContext context)
    {
        var routeValues = context.ApiDescription.ActionDescriptor.RouteValues;

        if (!routeValues.TryGetValue("controller", out var controller) ||
            !string.Equals(controller, "Workers",
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var method = context.ApiDescription.HttpMethod;

        if (method != "POST" && method != "PUT")
            return;

        if (operation.RequestBody?.Content is null)
            return;

        foreach (var content in operation.RequestBody.Content)
        {
            if (!content.Key.Contains("form",
                    StringComparison.OrdinalIgnoreCase))
                continue;

            SetPhoneExample(
                content.Value.Schema,
                "phoneNumber",
                "+962");
        }
    }

    private static void SetPhoneExample(
        OpenApiSchema? schema,
        string propertyName,
        string example)
    {
        if (schema?.Properties is null)
            return;

        var property = schema.Properties
            .FirstOrDefault(p =>
                string.Equals(
                    p.Key,
                    propertyName,
                    StringComparison.OrdinalIgnoreCase))
            .Value;

        if (property is not null)
        {
            property.Example = new OpenApiString(example);
        }
    }
}
