using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Domain.ValueObjects;

namespace Application.Tests.Entities;

public sealed class UserTests
{
    private static readonly Guid Id = Guid.Parse("c42c5805-cab8-4723-84e4-ee1ab50b36c9");
    private static readonly DateTime CreatedAt = new(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);
    private static readonly Email OriginalEmail = Email.Create("person@example.test");
    private static readonly Cpf OriginalCpf = Cpf.Create("01234567890");

    [Fact]
    public void CreateTrimsFullName()
    {
        var user = User.Create(Id, "  Sample Person  ", OriginalEmail, OriginalCpf, CreatedAt, UserStatus.Active);

        Assert.Equal("Sample Person", user.FullName);
        Assert.Equal(OriginalEmail, user.Email);
        Assert.Equal(OriginalCpf, user.Cpf);
    }

    [Fact]
    public void UpdateKeepsIdentityAndCreationDate()
    {
        var user = User.Create(Id, "Sample Person", OriginalEmail, OriginalCpf, CreatedAt, UserStatus.Active);

        user.Update(" New Name ", Email.Create("new@example.test"), Cpf.Create("529.982.247-25"), UserStatus.Inactive);

        Assert.Equal(Id, user.Id);
        Assert.Equal(CreatedAt, user.CreatedAt);
        Assert.Equal(DateTimeKind.Utc, user.CreatedAt.Kind);
        Assert.Equal("New Name", user.FullName);
        Assert.Equal("new@example.test", user.Email.Value);
        Assert.Equal("52998224725", user.Cpf.Value);
        Assert.Equal(UserStatus.Inactive, user.Status);
    }

    [Fact]
    public void CreateRejectsEmptyIdAndNonUtcTime()
    {
        Assert.Throws<DomainException>(() =>
            User.Create(Guid.Empty, "Sample Person", OriginalEmail, OriginalCpf, CreatedAt, UserStatus.Active));
        Assert.Throws<DomainException>(() =>
            User.Create(Id, "Sample Person", OriginalEmail, OriginalCpf,
                DateTime.SpecifyKind(CreatedAt, DateTimeKind.Unspecified), UserStatus.Active));
    }

    [Fact]
    public void UpdateRejectsInvalidProfileWithoutChangingUser()
    {
        var user = User.Create(Id, "Sample Person", OriginalEmail, OriginalCpf, CreatedAt, UserStatus.Active);
        var newEmail = Email.Create("new@example.test");
        var newCpf = Cpf.Create("52998224725");

        Assert.Throws<DomainException>(() => user.Update("   ", newEmail, newCpf, UserStatus.Inactive));
        Assert.Throws<DomainException>(() => user.Update(new string('x', 151), newEmail, newCpf, UserStatus.Inactive));
        Assert.Throws<DomainException>(() => user.Update("New Name", newEmail, newCpf, (UserStatus)42));

        Assert.Equal("Sample Person", user.FullName);
        Assert.Equal(OriginalEmail, user.Email);
        Assert.Equal(OriginalCpf, user.Cpf);
        Assert.Equal(UserStatus.Active, user.Status);
    }
}
