using Domain.Exceptions;
using Domain.ValueObjects;

namespace Application.Tests.ValueObjects;

public sealed class EmailTests
{
    [Fact]
    public void CreateTrimsAndLowercasesAddress()
    {
        Assert.Equal("person@example.test", Email.Create("  PERSON@Example.TEST  ").Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    [InlineData("no-at-sign")]
    [InlineData("Person <person@example.test>")]
    public void CreateRejectsInvalidAddress(string? input)
    {
        Assert.False(Email.IsValid(input));
        Assert.Throws<DomainException>(() => Email.Create(input));
    }

    [Fact]
    public void CreateEnforcesMaximumLengthAfterTrimming()
    {
        var atLimit = $"{new string('a', 64)}@{new string('b', 184)}.test";
        Assert.Equal(Email.MaxLength, atLimit.Length);

        Assert.True(Email.IsValid($"  {atLimit}  "));
        Assert.False(Email.IsValid($"a{atLimit}"));
    }
}
