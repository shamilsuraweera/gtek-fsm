using GTEK.FSM.Backend.Domain.Aggregates;
using GTEK.FSM.Backend.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GTEK.FSM.Backend.Infrastructure.Persistence.Configurations;

public sealed class JobConfiguration : IEntityTypeConfiguration<Job>
{
    public void Configure(EntityTypeBuilder<Job> builder)
    {
        builder.ToTable("Jobs");

        builder.HasKey(x => x.Id)
            .HasName("PK_Jobs");

        builder.HasAlternateKey(x => new { x.TenantId, x.Id })
            .HasName("AK_Jobs_TenantId_Id");

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.ServiceRequestId)
            .IsRequired();

        builder.Property(x => x.AssignmentStatus)
            .HasConversion<byte>()
            .HasColumnType("tinyint")
            .HasDefaultValue(AssignmentStatus.Unassigned)
            .IsRequired();

        builder.Property(x => x.AssignedWorkerUserId)
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

        builder.HasIndex(x => new { x.TenantId, x.ServiceRequestId })
            .HasDatabaseName("IX_Jobs_TenantId_ServiceRequestId");

        builder.HasIndex(x => new { x.TenantId, x.AssignmentStatus })
            .HasDatabaseName("IX_Jobs_TenantId_AssignmentStatus");

        builder.HasIndex(x => new { x.TenantId, x.AssignedWorkerUserId, x.AssignmentStatus })
            .HasDatabaseName("IX_Jobs_TenantId_AssignedWorkerUserId_AssignmentStatus");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Jobs_Tenants_TenantId");

        builder.HasOne<ServiceRequest>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.ServiceRequestId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Jobs_ServiceRequests_TenantId_ServiceRequestId");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.AssignedWorkerUserId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Jobs_Users_TenantId_AssignedWorkerUserId");

        builder.Ignore(x => x.DomainEvents);
    }
}
