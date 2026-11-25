using Domain.Users;

namespace Application.Tests.Users;

public sealed class CpfTests
{
    [Theory]
    [InlineData("012.345.678-90")]
    [InlineData("01234567890")]
    public void NormalizeValidCpf(string input)
    {
        Assert.True(Cpf.TryNormalize(input, out string normalized));
        Assert.Equal("01234567890", normalized);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("111.111.111-11")]
    [InlineData("012.345.678-91")]
    [InlineData("012/345/678-90")]
    [InlineData("０1234567890")]
    public void RejectInvalidCpf(string? input)
    {
        Assert.False(Cpf.TryNormalize(input, out _));
    }
}
