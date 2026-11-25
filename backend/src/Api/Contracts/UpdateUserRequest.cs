namespace Api.Contracts;

public sealed record UpdateUserRequest(string? FullName, string? Email, string? Cpf, string? Status);
