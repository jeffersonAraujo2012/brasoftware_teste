using Domain.Enums;
using Domain.Exceptions;
using Domain.ValueObjects;

namespace Domain.Entities;

public sealed class User
{
    public const int MaxFullNameLength = 150;

    private User()
    {
    }

    public Guid Id { get; private set; }
    public string FullName { get; private set; } = string.Empty;
    public Email Email { get; private set; } = null!;
    public Cpf Cpf { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public UserStatus Status { get; private set; }

    public static User Create(Guid id, string fullName, Email email, Cpf cpf, DateTime createdAt, UserStatus status)
    {
        if (id == Guid.Empty)
        {
            throw new DomainException("User ID must not be empty.");
        }

        if (createdAt.Kind != DateTimeKind.Utc)
        {
            throw new DomainException("Creation date must be in UTC.");
        }

        var user = new User { Id = id, CreatedAt = createdAt };
        user.Update(fullName, email, cpf, status);
        return user;
    }

    public void Update(string fullName, Email email, Cpf cpf, UserStatus status)
    {
        ArgumentNullException.ThrowIfNull(email);
        ArgumentNullException.ThrowIfNull(cpf);
        var normalizedName = fullName.Trim();

        if (normalizedName.Length is 0 or > MaxFullNameLength)
        {
            throw new DomainException($"Full name is required and must be at most {MaxFullNameLength} characters.");
        }

        if (!Enum.IsDefined(status))
        {
            throw new DomainException("Status is invalid.");
        }

        FullName = normalizedName;
        Email = email;
        Cpf = cpf;
        Status = status;
    }
}
