using Application.Exceptions;
using Application.Interfaces;
using MediatR;

namespace Application.Users.Commands.DeleteUser;

internal sealed class DeleteUserCommandHandler(IUserRepository repository) : IRequestHandler<DeleteUserCommand>
{
    public async Task Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        var user = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new UserNotFoundException(request.Id);
        await repository.DeleteAsync(user, cancellationToken);
    }
}
