using Domain.Entities;
using Domain.Enums;

namespace Application.Users.DTOs;

public sealed record UserDto(Guid Id, string FullName, string Email, string Cpf, DateTime CreatedAt, UserStatus Status)
{
    public static UserDto FromEntity(User user)
        => new(user.Id, user.FullName, user.Email.Value, user.Cpf.Value, user.CreatedAt, user.Status);
}
