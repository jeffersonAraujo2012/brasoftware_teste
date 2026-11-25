using Application.Behaviors;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Tests.Behaviors;

public sealed class LoggingBehaviorTests
{
    private const string Cpf = "01234567890";

    [Fact]
    public async Task LogsRequestNameWithoutPayload()
    {
        var logger = new RecordingLogger();
        var behavior = new LoggingBehavior<ProbeRequest, string>(logger);

        var response = await behavior.Handle(new ProbeRequest(Cpf), _ => Task.FromResult("done"), CancellationToken.None);

        Assert.Equal("done", response);
        Assert.Equal(2, logger.Messages.Count);
        Assert.All(logger.Messages, message => Assert.Contains(nameof(ProbeRequest), message));
        Assert.DoesNotContain(logger.Messages, message => message.Contains(Cpf));
    }

    [Fact]
    public async Task LogsFailureAndRethrows()
    {
        var logger = new RecordingLogger();
        var behavior = new LoggingBehavior<ProbeRequest, string>(logger);

        await Assert.ThrowsAsync<InvalidOperationException>(() => behavior.Handle(
            new ProbeRequest(Cpf), _ => throw new InvalidOperationException(Cpf), CancellationToken.None));

        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Warning
            && entry.Message.Contains(nameof(InvalidOperationException)));
        Assert.DoesNotContain(logger.Messages, message => message.Contains(Cpf));
    }

    private sealed record ProbeRequest(string Cpf) : IRequest<string>;

    private sealed class RecordingLogger : ILogger<LoggingBehavior<ProbeRequest, string>>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];
        public IReadOnlyList<string> Messages => Entries.Select(entry => entry.Message).ToArray();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
            => Entries.Add((logLevel, formatter(state, exception)));
    }
}
