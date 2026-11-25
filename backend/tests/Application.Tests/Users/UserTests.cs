using Domain.Users;

namespace Application.Tests.Users;

public sealed class UserTests
{
    private static readonly Guid Id = Guid.Parse("c42c5805-cab8-4723-84e4-ee1ab50b36c9");
    private static readonly DateTime CreatedAt = new(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void CreateNormalizesProfile()
    {
        var user = User.Create(Id, "  Sample Person  ", "  PERSON@EXAMPLE.TEST  ",
            "012.345.678-90", CreatedAt, UserStatus.Active);

        Assert.Equal("Sample Person", user.FullName);
        Assert.Equal("person@example.test", user.Email);
        Assert.Equal("01234567890", user.Cpf);
    }

    [Fact]
    public void UpdateKeepsIdentityAndCreationDate()
    {
        var user = User.Create(Id, "Sample Person", "person@example.test",
            "01234567890", CreatedAt, UserStatus.Active);

        user.Update(" New Name ", " NEW@EXAMPLE.TEST ", "529.982.247-25", UserStatus.Inactive);

        Assert.Equal(Id, user.Id);
        Assert.Equal(CreatedAt, user.CreatedAt);
        Assert.Equal(DateTimeKind.Utc, user.CreatedAt.Kind);
        Assert.Equal("New Name", user.FullName);
        Assert.Equal("new@example.test", user.Email);
        Assert.Equal("52998224725", user.Cpf);
        Assert.Equal(UserStatus.Inactive, user.Status);
    }

    [Fact]
    public void CreateRejectsEmptyIdAndNonUtcTime()
    {
        Assert.Throws<ArgumentException>(() =>
            User.Create(Guid.Empty, "Sample Person", "person@example.test", "01234567890",
                CreatedAt, UserStatus.Active));
        Assert.Throws<ArgumentException>(() =>
            User.Create(Id, "Sample Person", "person@example.test", "01234567890",
                DateTime.SpecifyKind(CreatedAt, DateTimeKind.Unspecified), UserStatus.Active));
    }

    [Fact]
    public void UpdateRejectsInvalidProfileWithoutChangingUser()
    {
        var user = User.Create(Id, "Sample Person", "person@example.test",
            "01234567890", CreatedAt, UserStatus.Active);

        Assert.Throws<ArgumentException>(() =>
            user.Update("New Name", "new@example.test", "01234567891", UserStatus.Inactive));
        Assert.Throws<ArgumentException>(() =>
            user.Update(new string('x', 151), "new@example.test", "52998224725", UserStatus.Inactive));
        Assert.Throws<ArgumentException>(() =>
            user.Update("New Name", new string('a', 255), "52998224725", UserStatus.Inactive));
        Assert.Throws<ArgumentException>(() =>
            user.Update("New Name", "new@example.test", "52998224725", (UserStatus)42));

        Assert.Equal("Sample Person", user.FullName);
        Assert.Equal("person@example.test", user.Email);
        Assert.Equal("01234567890", user.Cpf);
        Assert.Equal(UserStatus.Active, user.Status);
    }
}
