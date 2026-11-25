using Api.IntegrationTests.Fixtures;
using Application.Exceptions;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using Infra.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Api.IntegrationTests;

[Collection("PostgreSQL")]
public sealed class PostgresPersistenceTests(PostgresFixture postgres)
{
    private static readonly DateTime CreatedAt = new(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task MigrationCreatesUsersWithUtcTimestamp()
    {
        await using var factory = await ApiFactory.CreateAsync(postgres);
        var user = User.Create(Guid.NewGuid(), "Sample Person", Email.Create("person@example.test"),
            Cpf.Create("01234567890"), CreatedAt, UserStatus.Inactive);

        await InsertAsync(factory, user);

        await using var scope = factory.Services.CreateAsyncScope();
        var reader = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var reloaded = await reader.GetByIdAsync(user.Id, CancellationToken.None);

        Assert.NotNull(reloaded);
        Assert.Equal("01234567890", reloaded.Cpf.Value);
        Assert.Equal(CreatedAt, reloaded.CreatedAt);
        Assert.Equal(DateTimeKind.Utc, reloaded.CreatedAt.Kind);
        Assert.Equal(UserStatus.Inactive, reloaded.Status);
    }

    [Theory]
    [InlineData("PERSON@EXAMPLE.TEST", "12345678909", "email")]
    [InlineData("other@example.test", "012.345.678-90", "cpf")]
    public async Task UniqueIndexesReportConflictField(string email, string cpf, string expectedField)
    {
        await using var factory = await ApiFactory.CreateAsync(postgres);
        await InsertAsync(factory, User.Create(Guid.NewGuid(), "Sample Person", Email.Create("person@example.test"),
            Cpf.Create("01234567890"), CreatedAt, UserStatus.Active));
        var candidate = User.Create(Guid.NewGuid(), "Another Person", Email.Create(email), Cpf.Create(cpf),
            CreatedAt, UserStatus.Active);

        var error = await Assert.ThrowsAsync<UserConflictException>(() => InsertAsync(factory, candidate));

        Assert.Equal(expectedField, error.Field);
    }

    [Fact]
    public async Task MigrationRejectsInvalidStatusAtDatabase()
    {
        await using var factory = await ApiFactory.CreateAsync(postgres);
        var user = User.Create(Guid.NewGuid(), "Sample Person", Email.Create("person@example.test"),
            Cpf.Create("01234567890"), CreatedAt, UserStatus.Active);
        await InsertAsync(factory, user);

        await using var scope = factory.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<UserManagementDbContext>();
        var error = await Assert.ThrowsAsync<PostgresException>(() =>
            database.Database.ExecuteSqlInterpolatedAsync($"UPDATE users SET status = 'pending' WHERE id = {user.Id}"));

        Assert.Equal(PostgresErrorCodes.CheckViolation, error.SqlState);
    }

    [Fact]
    public async Task UpdateOfConcurrentlyDeletedUserReportsNotFound()
    {
        await using var factory = await ApiFactory.CreateAsync(postgres);
        var user = User.Create(Guid.NewGuid(), "Sample Person", Email.Create("person@example.test"),
            Cpf.Create("01234567890"), CreatedAt, UserStatus.Active);
        await InsertAsync(factory, user);

        await using var reader = factory.Services.CreateAsyncScope();
        var repository = reader.ServiceProvider.GetRequiredService<IUserRepository>();
        var original = await repository.GetByIdAsync(user.Id, CancellationToken.None);
        Assert.NotNull(original);

        await using var writer = factory.Services.CreateAsyncScope();
        var deletingRepository = writer.ServiceProvider.GetRequiredService<IUserRepository>();
        var deleted = await deletingRepository.GetByIdAsync(user.Id, CancellationToken.None);
        Assert.NotNull(deleted);
        await deletingRepository.DeleteAsync(deleted, CancellationToken.None);

        original.Update("Changed Name", original.Email, original.Cpf, UserStatus.Inactive);

        await Assert.ThrowsAsync<UserNotFoundException>(() => repository.UpdateAsync(original, CancellationToken.None));
    }

    private static async Task InsertAsync(ApiFactory factory, User user)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        await repository.AddAsync(user, CancellationToken.None);
    }
}
