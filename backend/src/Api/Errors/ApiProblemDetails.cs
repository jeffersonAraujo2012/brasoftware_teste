using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace Api.Errors;

internal static class ApiProblemDetails
{
    internal const string MediaType = "application/problem+json";

    internal static string TraceId(HttpContext context)
        => Activity.Current?.Id ?? context.TraceIdentifier;

    internal static ObjectResult FromModelState(ActionContext context)
    {
        var failures = context.ModelState
            .Where(entry => entry.Value is { Errors.Count: > 0 })
            .Select(entry => new KeyValuePair<string, string>(entry.Key, "The value is invalid."));
        var problem = Validation(context.HttpContext, failures);
        return new BadRequestObjectResult(problem) { ContentTypes = { MediaType } };
    }

    internal static ValidationProblemDetails Validation(
        HttpContext context, IEnumerable<KeyValuePair<string, string>> failures)
    {
        var errors = failures.GroupBy(failure => NormalizeKey(failure.Key))
            .ToDictionary(group => group.Key, group => group.Select(failure => failure.Value).Distinct().ToArray());
        return new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "One or more validation errors occurred.",
            Extensions = { ["traceId"] = TraceId(context) }
        };
    }

    internal static ProblemDetails Create(HttpContext context, int status, string title)
        => new()
        {
            Status = status,
            Title = title,
            Extensions = { ["traceId"] = TraceId(context) }
        };

    private static string NormalizeKey(string key)
    {
        if (key is "" or "$" || string.Equals(key, "request", StringComparison.OrdinalIgnoreCase))
        {
            return "body";
        }

        return JsonNamingPolicy.CamelCase.ConvertName(key[(key.LastIndexOf('.') + 1)..]);
    }
}
