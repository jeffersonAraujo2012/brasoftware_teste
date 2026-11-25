using Application.Users;

namespace Application.Tests.Users;

public sealed class CreateUserValidatorTests
{
    private static readonly CreateUserCommand Valid =
        new("Sample Person", "person@example.test", "012.345.678-90", "active");

    [Theory]
    [InlineData("FullName", "   ")]
    [InlineData("Email", "no-at-sign")]
    [InlineData("Cpf", "012.345.678-91")]
    [InlineData("Status", "pending")]
    [InlineData("Status", null)]
    public void InvalidFieldIsReported(string field, string? value)
    {
        var command = field switch
        {
            "FullName" => Valid with { FullName = value },
            "Email" => Valid with { Email = value },
            "Cpf" => Valid with { Cpf = value },
            _ => Valid with { Status = value }
        };

        var result = new CreateUserValidator().Validate(command);

        Assert.Contains(result.Errors, error => error.PropertyName == field);
    }

    [Fact]
    public void OverLimitFieldsAreReportedAfterTrimming()
    {
        var command = Valid with
        {
            FullName = $" {new string('x', 151)} ",
            Email = $" {new string('a', 70)}@{new string('b', 180)}.test "
        };

        var result = new CreateUserValidator().Validate(command);

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

        var result = new CreateUserValidator().Validate(command);

        Assert.True(result.IsValid);
    }
}
