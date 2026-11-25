namespace Domain.Users;

public static class Cpf
{
    public static bool TryNormalize(string? value, out string normalized)
    {
        normalized = string.Empty;
        if (value is null)
        {
            return false;
        }

        var hasMask = value.Length == 14 && value[3] == '.' && value[7] == '.' && value[11] == '-';
        var digits = hasMask
            ? string.Concat(value.AsSpan(0, 3), value.AsSpan(4, 3), value.AsSpan(8, 3), value.AsSpan(12, 2))
            : value;

        if (digits.Length != 11 || !digits.All(char.IsAsciiDigit) || digits.All(digit => digit == digits[0]))
        {
            return false;
        }

        if (digits[9] - '0' != CheckDigit(digits, 9) || digits[10] - '0' != CheckDigit(digits, 10))
        {
            return false;
        }

        normalized = digits;
        return true;
    }

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
