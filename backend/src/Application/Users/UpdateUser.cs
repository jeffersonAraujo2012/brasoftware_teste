using Application.Abstractions;
using FluentValidation;
using MediatR;

namespace Application.Users;

public sealed record UpdateUserCommand(Guid Id, string? FullName, string? Email, string? Cpf, string? Status)
    : IRequest<UserDetails>;

public sealed class UpdateUserValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        UserInputRules.Configure(this,
            command => command.FullName, command => command.Email, command => command.Cpf, command => command.Status);
    }
}

internal sealed class UpdateUserHandler(IUserRepository repository) : IRequestHandler<UpdateUserCommand, UserDetails>
{
    public async Task<UserDetails> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new UserNotFoundException(request.Id);
        user.Update(request.FullName!, request.Email!, request.Cpf!, UserInputRules.ParseStatus(request.Status!));
        await repository.UpdateAsync(user, cancellationToken);
        return UserDetails.From(user);
    }
}
