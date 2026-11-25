using System.Diagnostics.CodeAnalysis;
using Domain.Exceptions;

namespace Domain.ValueObjects;

public sealed record Cpf
{
    public const int Length = 11;

    private Cpf(string value) => Value = value;

    public string Value { get; }

    public static Cpf Create(string? value)
        => TryCreate(value, out var cpf) ? cpf : throw new DomainException("CPF is invalid.");

    public static bool IsValid(string? value) => TryCreate(value, out _);

    public static bool TryCreate(string? value, [NotNullWhen(true)] out Cpf? cpf)
    {
        cpf = null;
        if (value is null)
        {
            return false;
        }

        var hasMask = value.Length == 14 && value[3] == '.' && value[7] == '.' && value[11] == '-';
        var digits = hasMask
            ? string.Concat(value.AsSpan(0, 3), value.AsSpan(4, 3), value.AsSpan(8, 3), value.AsSpan(12, 2))
            : value;

        if (digits.Length != Length || !digits.All(char.IsAsciiDigit) || digits.All(digit => digit == digits[0]))
        {
            return false;
        }

        if (digits[9] - '0' != CheckDigit(digits, 9) || digits[10] - '0' != CheckDigit(digits, 10))
        {
            return false;
        }

        cpf = new Cpf(digits);
        return true;
    }

    public override string ToString() => Value;

    private static int CheckDigit(string digits, int length)
    {
        var sum = 0;
        for (var index = 0; index < length; index++)
        {
            sum += (digits[index] - '0') * (length + 1 - index);
        }

        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }
}
