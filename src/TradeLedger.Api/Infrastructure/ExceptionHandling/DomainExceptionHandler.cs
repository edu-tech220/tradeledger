using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TradeLedger.Api.Domain.Exceptions;

namespace TradeLedger.Api.Infrastructure.ExceptionHandling;

/// <summary>Maps domain exceptions to RFC 7807 ProblemDetails instead of leaking a 500.</summary>
public class DomainExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not DomainException domainException)
            return false;

        var statusCode = domainException switch
        {
            EntityNotFoundException => StatusCodes.Status404NotFound,
            InvalidOrderRequestException => StatusCodes.Status400BadRequest,
            InsufficientFundsException => StatusCodes.Status422UnprocessableEntity,
            InsufficientHoldingsException => StatusCodes.Status422UnprocessableEntity,
            InvalidOrderStateException => StatusCodes.Status409Conflict,
            ConcurrencyRetryExhaustedException => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest,
        };

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = statusCode,
            Title = domainException.GetType().Name,
            Detail = domainException.Message,
            Instance = httpContext.Request.Path,
        }, cancellationToken);

        return true;
    }
}
