using Application.Abstractions;
using Domain.Users;
using FluentValidation;
using MediatR;

namespace Application.Users;

public sealed record CreateUserCommand(string? FullName, string? Email, string? Cpf, string? Status)
    : IRequest<UserDetails>;

public sealed class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserValidator() => UserInputRules.Configure(this,
        command => command.FullName, command => command.Email, command => command.Cpf, command => command.Status);
}

internal sealed class CreateUserHandler(IUserRepository repository, TimeProvider timeProvider)
    : IRequestHandler<CreateUserCommand, UserDetails>
{
    public async Task<UserDetails> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var user = User.Create(Guid.NewGuid(), request.FullName!, request.Email!, request.Cpf!,
            timeProvider.GetUtcNow().UtcDateTime, UserInputRules.ParseStatus(request.Status!));
        await repository.AddAsync(user, cancellationToken);
        return UserDetails.From(user);
    }
}
