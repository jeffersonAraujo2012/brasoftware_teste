using Application.Users;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Api.Errors;

internal sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        ProblemDetails problem;
        switch (exception)
        {
            case ValidationException validation:
                problem = ApiProblemDetails.Validation(context, validation.Errors.Select(
                    failure => new KeyValuePair<string, string>(failure.PropertyName, failure.ErrorMessage)));
                break;
            case UserNotFoundException notFound:
                problem = ApiProblemDetails.Create(context, StatusCodes.Status404NotFound, notFound.Message);
                break;
            case UserConflictException conflict:
                problem = ApiProblemDetails.Create(context, StatusCodes.Status409Conflict,
                    conflict.Message);
                problem.Extensions["field"] = conflict.Field;
                break;
            default:
                logger.LogError("Unhandled API error {ExceptionType} for trace {TraceId}.",
                    exception.GetType().Name, ApiProblemDetails.TraceId(context));
                problem = ApiProblemDetails.Create(context, StatusCodes.Status500InternalServerError,
                    "An unexpected error occurred.");
                break;
        }

        context.Response.StatusCode = problem.Status!.Value;
        await context.Response.WriteAsJsonAsync(problem, problem.GetType(), options: null,
            contentType: ApiProblemDetails.MediaType, cancellationToken: cancellationToken);
        return true;
    }
}
