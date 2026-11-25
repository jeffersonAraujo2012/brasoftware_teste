namespace Api.Contracts;

public sealed class CreateUserRequest
{
    public string? FullName { get; init; }
    public string? Email { get; init; }
    public string? Cpf { get; init; }
    public string? Status { get; init; } = "active";
}
