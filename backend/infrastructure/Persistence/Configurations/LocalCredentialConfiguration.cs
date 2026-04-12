using GTEK.FSM.Backend.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GTEK.FSM.Backend.Infrastructure.Persistence.Configurations;

public sealed class LocalCredentialConfiguration : IEntityTypeConfiguration<LocalCredential>
{
    public void Configure(EntityTypeBuilder<LocalCredential> builder)
    {
        builder.ToTable("LocalCredentials");

        builder.HasKey(x => x.UserId)
            .HasName("PK_LocalCredentials");

        builder.Property(x => x.UserId)
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.Email)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.PasswordHash)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(x => x.Role)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnType("datetime2(3)")
            .HasDefaultValueSql("GETUTCDATE()")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.UpdatedAtUtc)
            .HasColumnType("datetime2(3)")
            .HasDefaultValueSql("GETUTCDATE()")
            .ValueGeneratedOnAddOrUpdate();

        builder.HasIndex(x => x.Email)
            .IsUnique()
            .HasDatabaseName("UQ_LocalCredentials_Email");

        builder.HasIndex(x => new { x.TenantId, x.Role })
            .HasDatabaseName("IX_LocalCredentials_TenantId_Role");

        builder.HasOne<User>()
            .WithOne()
            .HasForeignKey<LocalCredential>(x => new { x.TenantId, x.UserId })
            .HasPrincipalKey<User>(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_LocalCredentials_Users_TenantId_UserId");
    }
}
