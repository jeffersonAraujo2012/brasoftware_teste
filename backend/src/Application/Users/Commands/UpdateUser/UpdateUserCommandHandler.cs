using Application.Exceptions;
using Application.Interfaces;
using Application.Users.DTOs;
using Domain.ValueObjects;
using MediatR;

namespace Application.Users.Commands.UpdateUser;

internal sealed class UpdateUserCommandHandler(IUserRepository repository)
    : IRequestHandler<UpdateUserCommand, UserDto>
{
    public async Task<UserDto> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new UserNotFoundException(request.Id);
        user.Update(request.FullName!, Email.Create(request.Email), Cpf.Create(request.Cpf), request.Status!.Value);
        await repository.UpdateAsync(user, cancellationToken);
        return UserDto.FromEntity(user);
    }
}
