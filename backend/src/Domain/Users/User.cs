using CpfNumber = Domain.Users.Cpf;

namespace Domain.Users;

public sealed class User
{
    public const int MaxFullNameLength = 150;
    public const int MaxEmailLength = 254;

    private User()
    {
    }

    public Guid Id { get; private set; }
    public string FullName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string Cpf { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public UserStatus Status { get; private set; }

    public static User Create(Guid id, string fullName, string email, string cpf, DateTime createdAt, UserStatus status)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("User ID must not be empty.", nameof(id));
        }

        if (createdAt.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Creation date must be in UTC.", nameof(createdAt));
        }

        var user = new User { Id = id, CreatedAt = createdAt };
        user.Update(fullName, email, cpf, status);
        return user;
    }

    public void Update(string fullName, string email, string cpf, UserStatus status)
    {
        var normalizedName = fullName.Trim();
        var normalizedEmail = email.Trim().ToLowerInvariant();

        if (normalizedName.Length is 0 or > MaxFullNameLength)
        {
            throw new ArgumentException("Full name is required and must be at most 150 characters.", nameof(fullName));
        }

        if (normalizedEmail.Length is 0 or > MaxEmailLength)
        {
            throw new ArgumentException("Email is required and must be at most 254 characters.", nameof(email));
        }

        if (!CpfNumber.TryNormalize(cpf, out var normalizedCpf))
        {
            throw new ArgumentException("CPF is invalid.", nameof(cpf));
        }

        if (status is not (UserStatus.Active or UserStatus.Inactive))
        {
            throw new ArgumentException("Status is invalid.", nameof(status));
        }

        FullName = normalizedName;
        Email = normalizedEmail;
        Cpf = normalizedCpf;
        Status = status;
    }
}
