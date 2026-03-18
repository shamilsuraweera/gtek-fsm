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

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Subscriptions_Tenants_TenantId");

        builder.Ignore(x => x.DomainEvents);
    }
}
