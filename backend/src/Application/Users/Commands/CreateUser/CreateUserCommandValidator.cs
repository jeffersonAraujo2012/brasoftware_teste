using Domain.Entities;
using Domain.ValueObjects;
using FluentValidation;

namespace Application.Users.Commands.CreateUser;

public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.FullName)
            .NotEmpty()
            .Must(fullName => fullName!.Trim().Length <= User.MaxFullNameLength)
            .WithMessage($"Full name must be at most {User.MaxFullNameLength} characters.");

        RuleFor(x => x.Email)
            .NotEmpty()
            .Must(Email.IsValid)
            .WithMessage($"Email must be a valid address with at most {Email.MaxLength} characters.");

        RuleFor(x => x.Cpf)
            .NotEmpty()
            .Must(Cpf.IsValid)
            .WithMessage("CPF must be valid.");

        RuleFor(x => x.Status)
            .NotNull()
            .IsInEnum();
    }
}
