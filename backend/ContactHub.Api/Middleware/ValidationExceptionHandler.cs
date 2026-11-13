using ContactHub.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ContactHub.Api.Middleware;

/// <summary>Maps FluentValidation and domain validation failures to 400 ValidationProblem.</summary>
public class ValidationExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        IDictionary<string, string[]> errors;
        switch (exception)
        {
            case ValidationException ve:
                errors = ve.Errors
                    .GroupBy(e => ValidationKeys.ToCamelCase(e.PropertyName))
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray());
                break;
            case DomainValidationException de:
                errors = de.Errors;
                break;
            default:
                return false;
        }

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        var written = await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ValidationProblemDetails(errors)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "One or more validation errors occurred.",
            },
        });

        return written;
    }
}
