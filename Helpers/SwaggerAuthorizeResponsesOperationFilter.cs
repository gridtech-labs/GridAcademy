using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace GridAcademy.Helpers;

public class SwaggerAuthorizeResponsesOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var methodInfo = context.MethodInfo;

        var hasAllowAnonymous = methodInfo.GetCustomAttributes(true).OfType<AllowAnonymousAttribute>().Any()
            || methodInfo.DeclaringType?.GetCustomAttributes(true).OfType<AllowAnonymousAttribute>().Any() == true;

        if (hasAllowAnonymous)
            return;

        var hasAuthorize = methodInfo.GetCustomAttributes(true).OfType<AuthorizeAttribute>().Any()
            || methodInfo.DeclaringType?.GetCustomAttributes(true).OfType<AuthorizeAttribute>().Any() == true;

        if (!hasAuthorize)
            return;

        if (!operation.Responses.ContainsKey("401"))
        {
            operation.Responses.Add("401", new OpenApiResponse
            {
                Description = "Unauthorized (missing, invalid, or expired Bearer token)."
            });
        }

        if (!operation.Responses.ContainsKey("403"))
        {
            operation.Responses.Add("403", new OpenApiResponse
            {
                Description = "Forbidden (authenticated user lacks the required role/permissions)."
            });
        }
    }
}
