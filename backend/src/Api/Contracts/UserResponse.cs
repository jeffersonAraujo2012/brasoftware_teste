using Application.Users;

namespace Api.Contracts;

public sealed record UserResponse(Guid Id, string FullName, string Email, string Cpf, DateTime CreatedAt, string Status)
{
    internal static UserResponse From(UserDetails user)
        => new(user.Id, user.FullName, user.Email, user.Cpf, user.CreatedAt, user.Status);
}
