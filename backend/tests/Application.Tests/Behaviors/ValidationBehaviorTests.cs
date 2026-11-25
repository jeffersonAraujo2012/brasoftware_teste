using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Tests.Behaviors;

public sealed class ValidationBehaviorTests
{
    [Fact]
    public async Task InvalidRequestDoesNotInvokeHandler()
    {
        var probe = new Probe();
        var services = new ServiceCollection();
        services.AddApplication();
        services.AddSingleton<IRequestHandler<ProbeCommand, string>>(probe);
        services.AddSingleton<IValidator<ProbeCommand>>(new ValueValidator());
        services.AddSingleton<IValidator<ProbeCommand>>(new CodeValidator());
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var error = await Assert.ThrowsAsync<ValidationException>(() => sender.Send(new ProbeCommand("", "")));

        Assert.Equal(new[] { "Code", "Value" }, error.Errors.Select(failure => failure.PropertyName).Order());
        Assert.Equal(0, probe.Calls);

        Assert.Equal("valid", await sender.Send(new ProbeCommand("valid", "A")));
        Assert.Equal(1, probe.Calls);
    }

    private sealed record ProbeCommand(string Value, string Code) : IRequest<string>;

    private sealed class ValueValidator : AbstractValidator<ProbeCommand>
    {
        public ValueValidator() => RuleFor(command => command.Value).NotEmpty();
    }

    private sealed class CodeValidator : AbstractValidator<ProbeCommand>
    {
        public CodeValidator() => RuleFor(command => command.Code).NotEmpty();
    }

    private sealed class Probe : IRequestHandler<ProbeCommand, string>
    {
        public int Calls { get; private set; }

        public Task<string> Handle(ProbeCommand command, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(command.Value);
        }
    }
}
