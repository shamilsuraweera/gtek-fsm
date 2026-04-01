using GTEK.FSM.Backend.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GTEK.FSM.Backend.Infrastructure.Persistence.Configurations;

public sealed class WorkerProfileConfiguration : IEntityTypeConfiguration<WorkerProfile>
{
    public void Configure(EntityTypeBuilder<WorkerProfile> builder)
    {
        builder.ToTable("WorkerProfiles");

        builder.HasKey(x => x.Id)
            .HasName("PK_WorkerProfiles");

        builder.HasAlternateKey(x => new { x.TenantId, x.Id })
            .HasName("AK_WorkerProfiles_TenantId_Id");

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.WorkerCode)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.DisplayName)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(x => x.InternalRating)
            .HasColumnType("decimal(3,1)")
            .IsRequired();

        builder.Property(x => x.SkillTagsSerialized)
            .HasColumnName("SkillTags")
            .HasMaxLength(1024)
            .IsRequired();

        builder.Property(x => x.AvailabilityStatus)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.IsActive)
            .HasColumnType("bit")
            .HasDefaultValue(true)
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

        builder.HasIndex(x => x.TenantId)
            .HasDatabaseName("IX_WorkerProfiles_TenantId");

        builder.HasIndex(x => new { x.TenantId, x.WorkerCode })
            .IsUnique()
            .HasDatabaseName("UQ_WorkerProfiles_TenantId_WorkerCode");

        builder.HasIndex(x => new { x.TenantId, x.DisplayName })
            .HasDatabaseName("IX_WorkerProfiles_TenantId_DisplayName");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_WorkerProfiles_Tenants_TenantId");
    }
}
