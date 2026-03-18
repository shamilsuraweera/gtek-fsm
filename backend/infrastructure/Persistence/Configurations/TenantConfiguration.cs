using GTEK.FSM.Backend.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GTEK.FSM.Backend.Infrastructure.Persistence.Configurations;

public sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants");

        builder.HasKey(x => x.Id)
            .HasName("PK_Tenants");

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.Code)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(x => x.ActiveSubscriptionId)
            .IsRequired(false);

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnType("datetime2(3)")
            .HasDefaultValueSql("GETUTCDATE()")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.UpdatedAtUtc)
            .HasColumnType("datetime2(3)")
            .HasDefaultValueSql("GETUTCDATE()")
            .ValueGeneratedOnAddOrUpdate();

        builder.Property(x => x.IsDeleted)
            .HasColumnType("bit")
            .HasDefaultValue(false)
            .IsRequired();

        builder.HasIndex(x => x.Code)
            .IsUnique()
            .HasDatabaseName("UQ_Tenants_Code");

        builder.HasIndex(x => x.ActiveSubscriptionId)
            .HasDatabaseName("IX_Tenants_ActiveSubscriptionId");

        builder.HasOne<Subscription>()
            .WithMany()
            .HasForeignKey(x => new { x.Id, x.ActiveSubscriptionId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Tenants_Subscriptions_Id_ActiveSubscriptionId");

        builder.Ignore(x => x.DomainEvents);
    }
}
