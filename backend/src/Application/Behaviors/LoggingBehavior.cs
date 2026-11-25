using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Behaviors;

// Logs only the request type and timing: payloads carry personal data such as CPF and email.
public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var startedAt = Stopwatch.GetTimestamp();
        logger.LogInformation("Handling {RequestName}.", requestName);
        try
        {
            var response = await next(cancellationToken);
            logger.LogInformation("Handled {RequestName} in {ElapsedMilliseconds} ms.",
                requestName, Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds);
            return response;
        }
        catch (Exception exception)
        {
            logger.LogWarning("{RequestName} failed with {ExceptionType} after {ElapsedMilliseconds} ms.",
                requestName, exception.GetType().Name, Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds);
            throw;
        }
    }
}
