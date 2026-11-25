using Application.Users.Commands.CreateUser;
using Domain.Enums;

namespace Application.Tests.Users.Commands.CreateUser;

public sealed class CreateUserCommandValidatorTests
{
    private static readonly CreateUserCommand Valid =
        new("Sample Person", "person@example.test", "012.345.678-90", UserStatus.Active);

    private readonly CreateUserCommandValidator _validator = new();

    [Theory]
    [InlineData("FullName", null)]
    [InlineData("FullName", "   ")]
    [InlineData("Email", null)]
    [InlineData("Email", "no-at-sign")]
    [InlineData("Cpf", null)]
    [InlineData("Cpf", "012.345.678-91")]
    public void InvalidTextFieldReportsSingleError(string field, string? value)
    {
        var command = field switch
        {
            "FullName" => Valid with { FullName = value },
            "Email" => Valid with { Email = value },
            _ => Valid with { Cpf = value }
        };

        var result = _validator.Validate(command);

        Assert.Single(result.Errors, error => error.PropertyName == field);
    }

    [Theory]
    [InlineData(null)]
    [InlineData((UserStatus)42)]
    public void InvalidStatusIsReported(UserStatus? status)
    {
        var result = _validator.Validate(Valid with { Status = status });

        Assert.Contains(result.Errors, error => error.PropertyName == "Status");
    }

    [Fact]
    public void OverLimitFieldsAreReportedAfterTrimming()
    {
        var command = Valid with
        {
            FullName = $" {new string('x', 151)} ",
            Email = $" {new string('a', 70)}@{new string('b', 180)}.test "
        };

        var result = _validator.Validate(command);

        Assert.Contains(result.Errors, error => error.PropertyName == "FullName");
        Assert.Contains(result.Errors, error => error.PropertyName == "Email");
    }

    [Fact]
    public void ValidFieldsAreCheckedAfterTrimming()
    {
        var command = Valid with
        {
            FullName = $" {new string('x', 150)} ",
            Email = " PERSON@EXAMPLE.TEST "
        };

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }
}
