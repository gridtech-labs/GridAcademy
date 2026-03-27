using System.Net;
using System.Text.Json;
using System.Web;
using GridAcademy.Common;

namespace GridAcademy.Middleware;

/// <summary>
/// Global error handler — catches any unhandled exception and
/// returns a clean JSON error instead of a raw 500 HTML page.
/// </summary>
public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next   = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await _next(ctx);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception on {Method} {Path}", ctx.Request.Method, ctx.Request.Path);

            // API paths get a JSON error; Razor Pages get an HTML error page
            if (IsApiPath(ctx))
                await WriteJsonErrorAsync(ctx, ex);
            else
                await WriteHtmlErrorAsync(ctx, ex);
        }
    }

    private static bool IsApiPath(HttpContext ctx) =>
        ctx.Request.Path.StartsWithSegments("/api") ||
        ctx.Request.Path.StartsWithSegments("/hangfire");

    private static Task WriteJsonErrorAsync(HttpContext ctx, Exception ex)
    {
        ctx.Response.ContentType = "application/json";

        ctx.Response.StatusCode = ex switch
        {
            UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
            KeyNotFoundException        => (int)HttpStatusCode.NotFound,
            ArgumentException           => (int)HttpStatusCode.BadRequest,
            _                           => (int)HttpStatusCode.InternalServerError
        };

        var response = ApiResponse.Fail(
            ctx.Response.StatusCode == 500 ? "An unexpected error occurred." : ex.Message);

        return ctx.Response.WriteAsync(JsonSerializer.Serialize(response,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }

    private static Task WriteHtmlErrorAsync(HttpContext ctx, Exception ex)
    {
        ctx.Response.ContentType = "text/html";
        ctx.Response.StatusCode  = (int)HttpStatusCode.InternalServerError;

        var msg = HttpUtility.HtmlEncode(ex.Message);
        return ctx.Response.WriteAsync($@"<!DOCTYPE html>
<html><head><title>Error</title>
<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/css/bootstrap.min.css"">
</head><body class=""d-flex align-items-center justify-content-center vh-100 bg-light"">
<div class=""text-center"">
  <h1 class=""display-1 text-danger"">500</h1>
  <p class=""lead"">An unexpected error occurred.</p>
  <p class=""text-muted small"">{msg}</p>
  <a href=""/Admin"" class=""btn btn-primary"">Back to Admin</a>
</div></body></html>");
    }
}
