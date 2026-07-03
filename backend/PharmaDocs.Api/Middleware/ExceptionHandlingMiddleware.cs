using PharmaDocs.Api.Common.Exceptions;

namespace PharmaDocs.Api.Middleware;

/// <summary>
/// Vangt uitzonderingen centraal op en zet ze om naar een consistente ProblemDetails-JSON.
/// Verwachte <see cref="AppException"/>'s krijgen hun eigen status; de rest wordt 500.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (AppException ex)
        {
            await WriteProblem(context, ex.StatusCode, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Onverwachte fout tijdens {Path}", context.Request.Path);
            await WriteProblem(context, StatusCodes.Status500InternalServerError,
                "Er is een onverwachte fout opgetreden.");
        }
    }

    private static Task WriteProblem(HttpContext context, int statusCode, string detail)
    {
        context.Response.StatusCode = statusCode;
        return context.Response.WriteAsJsonAsync(new
        {
            status = statusCode,
            detail
        });
    }
}
