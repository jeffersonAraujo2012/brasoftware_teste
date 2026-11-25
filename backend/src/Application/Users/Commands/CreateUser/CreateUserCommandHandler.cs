using Application.Interfaces;
using Application.Users.DTOs;
using Domain.Entities;
using Domain.ValueObjects;
using MediatR;

namespace Application.Users.Commands.CreateUser;

internal sealed class CreateUserCommandHandler(IUserRepository repository, TimeProvider timeProvider)
    : IRequestHandler<CreateUserCommand, UserDto>
{
    public async Task<UserDto> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        // Match the precision persisted by PostgreSQL so reads preserve the creation response.
        var createdAt = now.AddTicks(-(now.Ticks % TimeSpan.TicksPerMicrosecond));
        var user = User.Create(Guid.NewGuid(), request.FullName!, Email.Create(request.Email),
            Cpf.Create(request.Cpf), createdAt, request.Status!.Value);
        await repository.AddAsync(user, cancellationToken);
        return UserDto.FromEntity(user);
    }
}
