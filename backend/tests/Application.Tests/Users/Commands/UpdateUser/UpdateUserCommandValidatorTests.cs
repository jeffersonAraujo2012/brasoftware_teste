using Application.Users.Commands.UpdateUser;
using Domain.Enums;

namespace Application.Tests.Users.Commands.UpdateUser;

public sealed class UpdateUserCommandValidatorTests
{
    private static readonly UpdateUserCommand Valid = new(Guid.Parse("c42c5805-cab8-4723-84e4-ee1ab50b36c9"),
        "Sample Person", "person@example.test", "012.345.678-90", UserStatus.Inactive);

    private readonly UpdateUserCommandValidator _validator = new();

    [Fact]
    public void ValidCommandHasNoErrors()
    {
        Assert.True(_validator.Validate(Valid).IsValid);
    }

    [Fact]
    public void EveryInvalidFieldIsReported()
    {
        var command = new UpdateUserCommand(Guid.Empty, "", "no-at-sign", "111.111.111-11", null);

        var result = _validator.Validate(command);

        Assert.Equal(new[] { "Cpf", "Email", "FullName", "Id", "Status" },
            result.Errors.Select(error => error.PropertyName).Order());
    }
}
