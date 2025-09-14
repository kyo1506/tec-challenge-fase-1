using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Fcg.Identity.Api.Extensions;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    private readonly ILogger _logger = logger;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        _logger.LogError(exception, "An error has occurred");

        const int statusCode = StatusCodes.Status500InternalServerError;

        var responseError = new Root<ProblemDetails>
        {
            Success = false,
            StatusCode = statusCode,
            Data = new ProblemDetails
            {
                Status = statusCode,
                Title = "Internal Server Error",
                Instance = httpContext.Request.Path,
            },
        };

        httpContext.Response.StatusCode = statusCode;

        await httpContext.Response.WriteAsJsonAsync(responseError, cancellationToken);

        return true;
    }
}
