using System.Diagnostics.CodeAnalysis;
using System.Net.Mail;
using Domain.Exceptions;

namespace Domain.ValueObjects;

public sealed record Email
{
    public const int MaxLength = 254;

    private Email(string value) => Value = value;

    public string Value { get; }

    public static Email Create(string? value)
        => TryCreate(value, out var email) ? email : throw new DomainException("Email is invalid.");

    public static bool IsValid(string? value) => TryCreate(value, out _);

    public static bool TryCreate(string? value, [NotNullWhen(true)] out Email? email)
    {
        email = null;
        var address = value?.Trim();
        if (string.IsNullOrEmpty(address) || address.Length > MaxLength)
        {
            return false;
        }

        // Reject inputs that MailAddress accepts only by reinterpreting them, such as display names.
        if (!MailAddress.TryCreate(address, out var parsed) || !string.Equals(address, parsed.Address, StringComparison.Ordinal))
        {
            return false;
        }

        email = new Email(address.ToLowerInvariant());
        return true;
    }

    public override string ToString() => Value;
}
