using Application.Abstractions;
using FluentValidation;
using MediatR;

namespace Application.Users;

public sealed record DeleteUserCommand(Guid Id) : IRequest;

public sealed class DeleteUserValidator : AbstractValidator<DeleteUserCommand>
{
    public DeleteUserValidator() => RuleFor(command => command.Id).NotEmpty();
}

internal sealed class DeleteUserHandler(IUserRepository repository) : IRequestHandler<DeleteUserCommand>
{
    public async Task Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        var user = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new UserNotFoundException(request.Id);
        await repository.DeleteAsync(user, cancellationToken);
    }
}
