using GTEK.FSM.Backend.Domain.Aggregates;
using GTEK.FSM.Backend.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GTEK.FSM.Backend.Infrastructure.Persistence.Configurations;

public sealed class ServiceRequestConfiguration : IEntityTypeConfiguration<ServiceRequest>
{
    public void Configure(EntityTypeBuilder<ServiceRequest> builder)
    {
        builder.ToTable("ServiceRequests");

        builder.HasKey(x => x.Id)
            .HasName("PK_ServiceRequests");

        builder.HasAlternateKey(x => new { x.TenantId, x.Id })
            .HasName("AK_ServiceRequests_TenantId_Id");

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.CustomerUserId)
            .IsRequired();

        builder.Property(x => x.Title)
            .HasMaxLength(180)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<byte>()
            .HasColumnType("tinyint")
            .HasDefaultValue(ServiceRequestStatus.New)
            .IsRequired();

        builder.Property(x => x.ActiveJobId)
            .IsRequired(false);

        builder.HasIndex(x => new { x.TenantId, x.Status })
            .HasDatabaseName("IX_ServiceRequests_TenantId_Status");

        builder.HasIndex(x => new { x.TenantId, x.CustomerUserId })
            .HasDatabaseName("IX_ServiceRequests_TenantId_CustomerUserId");

        builder.HasIndex(x => new { x.TenantId, x.ActiveJobId })
            .IsUnique()
            .HasFilter("[ActiveJobId] IS NOT NULL")
            .HasDatabaseName("UQ_ServiceRequests_TenantId_ActiveJobId");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_ServiceRequests_Tenants_TenantId");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.CustomerUserId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_ServiceRequests_Users_TenantId_CustomerUserId");

        builder.HasOne<Job>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.ActiveJobId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_ServiceRequests_Jobs_TenantId_ActiveJobId");

        builder.Ignore(x => x.DomainEvents);
    }
}
