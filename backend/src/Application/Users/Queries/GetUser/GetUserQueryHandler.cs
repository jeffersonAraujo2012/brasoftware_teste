using Application.Exceptions;
using Application.Interfaces;
using Application.Users.DTOs;
using MediatR;

namespace Application.Users.Queries.GetUser;

internal sealed class GetUserQueryHandler(IUserReadService readService) : IRequestHandler<GetUserQuery, UserDto>
{
    public async Task<UserDto> Handle(GetUserQuery request, CancellationToken cancellationToken)
        => await readService.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new UserNotFoundException(request.Id);
}
