using Domain.Enums;

namespace Api.Requests;

public sealed record UpdateUserRequest(string? FullName, string? Email, string? Cpf, UserStatus? Status);
