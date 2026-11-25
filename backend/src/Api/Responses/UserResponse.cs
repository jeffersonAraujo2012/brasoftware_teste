using Application.Users.DTOs;
using Domain.Enums;

namespace Api.Responses;

public sealed record UserResponse(Guid Id, string FullName, string Email, string Cpf, DateTime CreatedAt, UserStatus Status)
{
    internal static UserResponse From(UserDto user)
        => new(user.Id, user.FullName, user.Email, user.Cpf, user.CreatedAt, user.Status);
}
