using Domain.Exceptions;
using Domain.ValueObjects;

namespace Application.Tests.ValueObjects;

public sealed class CpfTests
{
    [Theory]
    [InlineData("012.345.678-90")]
    [InlineData("01234567890")]
    public void CreateNormalizesValidCpfToDigits(string input)
    {
        Assert.Equal("01234567890", Cpf.Create(input).Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("111.111.111-11")]
    [InlineData("012.345.678-91")]
    [InlineData("012/345/678-90")]
    [InlineData("０1234567890")]
    public void CreateRejectsInvalidCpf(string? input)
    {
        Assert.False(Cpf.IsValid(input));
        Assert.Throws<DomainException>(() => Cpf.Create(input));
    }

    [Fact]
    public void CpfsWithSameDigitsAreEqual()
    {
        Assert.Equal(Cpf.Create("012.345.678-90"), Cpf.Create("01234567890"));
    }
}
