using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace TeamFlow.Api.Middleware;

public sealed class GlobalExceptionHandlerMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionHandlerMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            var correlationId = context.Items["X-Correlation-ID"]?.ToString() ?? "unknown";

            logger.LogError(
                ex,
                "Unhandled exception for {Method} {Path} [CorrelationId={CorrelationId}]",
                context.Request.Method,
                context.Request.Path,
                correlationId);

            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/problem+json";

            var problemDetails = new ProblemDetails
            {
                Status = (int)HttpStatusCode.InternalServerError,
                Title = "An unexpected error occurred",
                Detail = context.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment()
                    ? ex.Message
                    : "An internal server error has occurred. Please try again later.",
                Instance = context.Request.Path,
                Extensions =
                {
                    ["correlationId"] = correlationId
                }
            };

            await context.Response.WriteAsJsonAsync(problemDetails, context.RequestAborted);
        }
    }
}

public static class GlobalExceptionHandlerMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
        => app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
}
