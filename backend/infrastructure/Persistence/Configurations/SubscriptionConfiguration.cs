using GTEK.FSM.Backend.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GTEK.FSM.Backend.Infrastructure.Persistence.Configurations;

public sealed class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ToTable("Subscriptions");

        builder.HasKey(x => x.Id)
            .HasName("PK_Subscriptions");

        builder.HasAlternateKey(x => new { x.TenantId, x.Id })
            .HasName("AK_Subscriptions_TenantId_Id");

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.PlanCode)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.StartsOnUtc)
            .HasPrecision(3)
            .IsRequired();

        builder.Property(x => x.EndsOnUtc)
            .HasPrecision(3)
            .IsRequired(false);

        builder.HasIndex(x => x.TenantId)
            .HasDatabaseName("IX_Subscriptions_TenantId");

        builder.HasIndex(x => new { x.TenantId, x.PlanCode })
            .HasDatabaseName("IX_Subscriptions_TenantId_PlanCode");

        builder.HasIndex(x => new { x.TenantId, x.StartsOnUtc })
            .HasDatabaseName("IX_Subscriptions_TenantId_StartsOnUtc");

        builder.HasIndex(x => new { x.TenantId, x.EndsOnUtc })
            .HasDatabaseName("IX_Subscriptions_TenantId_EndsOnUtc");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Subscriptions_Tenants_TenantId");

        builder.Ignore(x => x.DomainEvents);
    }
}
