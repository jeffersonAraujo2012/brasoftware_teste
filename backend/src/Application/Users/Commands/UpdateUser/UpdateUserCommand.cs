using Application.Users.DTOs;
using Domain.Enums;
using MediatR;

namespace Application.Users.Commands.UpdateUser;

public sealed record UpdateUserCommand(Guid Id, string? FullName, string? Email, string? Cpf, UserStatus? Status)
    : IRequest<UserDto>;
