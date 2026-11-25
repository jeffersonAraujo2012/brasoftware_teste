using Domain.Users;

namespace Application.Users;

public sealed record UserDetails(Guid Id, string FullName, string Email, string Cpf, DateTime CreatedAt, string Status)
{
    public static UserDetails From(User user) => new(
        user.Id, user.FullName, user.Email, user.Cpf, user.CreatedAt,
        user.Status == UserStatus.Active ? "active" : "inactive");
}
