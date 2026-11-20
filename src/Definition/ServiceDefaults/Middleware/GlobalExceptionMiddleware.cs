using Microsoft.Extensions.Logging;
using Npgsql;
using Perigon.AspNetCore.Models;
using Share.Exceptions;

namespace ServiceDefaults.Middleware;

public class GlobalExceptionMiddleware(RequestDelegate next, Localizer localizer, ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (PostgresException ex) when (!context.Response.HasStarted && ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            await WriteErrorAsync(context, StatusCodes.Status409Conflict, localizer.Get(Localizer.ConflictResource));
        }
        catch (BusinessException ex) when (!context.Response.HasStarted)
        {
            await WriteErrorAsync(context, ex.StatusCodes, localizer.Get(ex.LanguageKey));
        }
        catch (Exception ex) when (!context.Response.HasStarted && !context.RequestAborted.IsCancellationRequested)
        {
            logger.LogError(ex, "Unhandled request failure. Trace identifier: {TraceIdentifier}", context.TraceIdentifier);
            await WriteErrorAsync(context, StatusCodes.Status500InternalServerError, "Internal server error.");
        }
    }

    private static Task WriteErrorAsync(HttpContext context, int status, string detail)
    {
        context.Response.StatusCode = status;
        return context.Response.WriteAsJsonAsync(
            new ErrorResult(detail, context.TraceIdentifier, status: status),
            MiddlewareJsonContext.Default.ErrorResult);
    }
}
