using Application.Users.DTOs;
using MediatR;

namespace Application.Users.Queries.GetUser;

public sealed record GetUserQuery(Guid Id) : IRequest<UserDto>;
