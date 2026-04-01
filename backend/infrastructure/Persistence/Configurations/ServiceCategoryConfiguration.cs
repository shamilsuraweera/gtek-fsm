using GTEK.FSM.Backend.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GTEK.FSM.Backend.Infrastructure.Persistence.Configurations;

public sealed class ServiceCategoryConfiguration : IEntityTypeConfiguration<ServiceCategory>
{
    public void Configure(EntityTypeBuilder<ServiceCategory> builder)
    {
        builder.ToTable("ServiceCategories");

        builder.HasKey(x => x.Id)
            .HasName("PK_ServiceCategories");

        builder.HasAlternateKey(x => new { x.TenantId, x.Id })
            .HasName("AK_ServiceCategories_TenantId_Id");

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.Code)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(x => x.SortOrder)
            .HasColumnType("int")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(x => x.IsEnabled)
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

        builder.HasIndex(x => new { x.TenantId, x.Code })
            .IsUnique()
            .HasDatabaseName("UQ_ServiceCategories_TenantId_Code");

        builder.HasIndex(x => new { x.TenantId, x.IsEnabled, x.SortOrder })
            .HasDatabaseName("IX_ServiceCategories_TenantId_IsEnabled_SortOrder");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_ServiceCategories_Tenants_TenantId");
    }
}