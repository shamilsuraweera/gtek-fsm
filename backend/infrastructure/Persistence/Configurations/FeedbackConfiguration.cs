using GTEK.FSM.Backend.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GTEK.FSM.Backend.Infrastructure.Persistence.Configurations;

public sealed class FeedbackConfiguration : IEntityTypeConfiguration<Feedback>
{
    public void Configure(EntityTypeBuilder<Feedback> builder)
    {
        builder.ToTable("Feedback");

        builder.HasKey(x => x.Id)
            .HasName("PK_Feedback");

        builder.HasAlternateKey(x => new { x.TenantId, x.Id })
            .HasName("AK_Feedback_TenantId_Id");

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.JobId)
            .IsRequired();

        builder.Property(x => x.ServiceRequestId)
            .IsRequired();

        builder.Property(x => x.ProvidedByUserId)
            .IsRequired();

        builder.Property(x => x.Source)
            .HasColumnType("int")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Rating)
            .HasColumnType("decimal(3,2)")
            .HasDefaultValue(0m)
            .IsRequired();

        builder.Property(x => x.Comment)
            .HasColumnType("nvarchar(1000)")
            .HasMaxLength(1000)
            .HasDefaultValue(string.Empty)
            .IsRequired();

        builder.Property(x => x.Type)
            .HasColumnType("int")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.IsActionable)
            .HasColumnType("bit")
            .HasDefaultValue(false)
            .IsRequired();

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

        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        // Foreign keys
        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Feedback_Tenants_TenantId");

        builder.HasOne<Job>()
            .WithMany()
            .HasForeignKey(x => x.JobId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Feedback_Jobs_JobId");

        builder.HasOne<ServiceRequest>()
            .WithMany()
            .HasForeignKey(x => x.ServiceRequestId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Feedback_ServiceRequests_ServiceRequestId");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.ProvidedByUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Feedback_Users_ProvidedByUserId");

        // Indexes for common queries
        builder.HasIndex(x => new { x.TenantId, x.CreatedAtUtc })
            .IsDescending(false, true)
            .HasDatabaseName("IX_Feedback_TenantId_CreatedAtUtc");

        builder.HasIndex(x => new { x.TenantId, x.ServiceRequestId })
            .HasDatabaseName("IX_Feedback_TenantId_ServiceRequestId");

        builder.HasIndex(x => new { x.TenantId, x.JobId })
            .HasDatabaseName("IX_Feedback_TenantId_JobId");

        builder.HasIndex(x => new { x.TenantId, x.IsActionable, x.CreatedAtUtc })
            .IsDescending(false, false, true)
            .HasDatabaseName("IX_Feedback_TenantId_IsActionable_CreatedAtUtc");

        builder.HasIndex(x => new { x.TenantId, x.Source, x.CreatedAtUtc })
            .IsDescending(false, false, true)
            .HasDatabaseName("IX_Feedback_TenantId_Source_CreatedAtUtc");
    }
}
