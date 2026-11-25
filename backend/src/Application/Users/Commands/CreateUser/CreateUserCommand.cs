using Application.Users.DTOs;
using Domain.Enums;
using MediatR;

namespace Application.Users.Commands.CreateUser;

public sealed record CreateUserCommand(string? FullName, string? Email, string? Cpf, UserStatus? Status)
    : IRequest<UserDto>;
