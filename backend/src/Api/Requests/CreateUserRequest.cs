using Domain.Enums;

namespace Api.Requests;

public sealed class CreateUserRequest
{
    public string? FullName { get; init; }
    public string? Email { get; init; }
    public string? Cpf { get; init; }
    public UserStatus? Status { get; init; } = UserStatus.Active;
}
