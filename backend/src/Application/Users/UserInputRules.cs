using System.Linq.Expressions;
using System.Net.Mail;
using Domain.Users;
using FluentValidation;

namespace Application.Users;

internal static class UserInputRules
{
    internal static void Configure<T>(
        AbstractValidator<T> validator,
        Expression<Func<T, string?>> fullName,
        Expression<Func<T, string?>> email,
        Expression<Func<T, string?>> cpf,
        Expression<Func<T, string?>> status)
    {
        validator.RuleFor(fullName).Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(value => value!.Trim().Length <= User.MaxFullNameLength)
            .WithMessage($"Full name must be at most {User.MaxFullNameLength} characters.");

        validator.RuleFor(email).Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(value => value!.Trim().Length <= User.MaxEmailLength)
            .WithMessage($"Email must be at most {User.MaxEmailLength} characters.")
            .Must(IsEmailAddress)
            .WithMessage("Email must be a valid address.");

        validator.RuleFor(cpf)
            .Must(value => Cpf.TryNormalize(value, out _))
            .WithMessage("CPF must be valid.");

        validator.RuleFor(status)
            .Must(value => value is "active" or "inactive")
            .WithMessage("Status must be active or inactive.");
    }

    internal static UserStatus ParseStatus(string status) => status switch
    {
        "active" => UserStatus.Active,
        "inactive" => UserStatus.Inactive,
        _ => throw new ArgumentException("Status must be active or inactive.", nameof(status))
    };

    private static bool IsEmailAddress(string? value)
    {
        var address = value?.Trim();
        return MailAddress.TryCreate(address, out var parsed)
            && string.Equals(address, parsed.Address, StringComparison.Ordinal);
    }
}
