using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infra.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    internal const string EmailIndex = "ux_users_email";
    internal const string CpfIndex = "ux_users_cpf";

    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users", table => table.HasCheckConstraint(
            "ck_users_status", "status IN ('active', 'inactive')"));
        builder.HasKey(user => user.Id).HasName("pk_users");

        builder.Property(user => user.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(user => user.FullName).HasColumnName("full_name").HasMaxLength(User.MaxFullNameLength).IsRequired();
        builder.Property(user => user.Email).HasColumnName("email").HasMaxLength(User.MaxEmailLength).IsRequired();
        builder.Property(user => user.Cpf).HasColumnName("cpf").HasMaxLength(11).IsRequired();
        builder.Property(user => user.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone");
        builder.Property(user => user.Status).HasColumnName("status").HasMaxLength(8)
            .HasConversion(
                status => status == UserStatus.Active ? "active" : "inactive",
                value => value == "active" ? UserStatus.Active : UserStatus.Inactive)
            .IsRequired();

        builder.HasIndex(user => user.Email).IsUnique().HasDatabaseName(EmailIndex);
        builder.HasIndex(user => user.Cpf).IsUnique().HasDatabaseName(CpfIndex);
    }
}
