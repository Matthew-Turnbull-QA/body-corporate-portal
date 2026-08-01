using Bcmp.Domain.Jobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bcmp.Infrastructure.Persistence.Configurations;

public sealed class JobConfiguration : IEntityTypeConfiguration<Job>
{
    public void Configure(EntityTypeBuilder<Job> builder)
    {
        builder.ToTable("Jobs");

        builder.HasKey(j => j.Id);

        builder.Property(j => j.JobNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(j => j.JobNumber).IsUnique();

        builder.Property(j => j.PropertyId)
            .IsRequired(false);

        builder.HasIndex(j => j.PropertyId);

        builder.Property(j => j.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(j => j.Description)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(j => j.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(j => j.Source)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(j => j.CreatedByUserId)
            .IsRequired();

        builder.Property(j => j.CreatedAtUtc)
            .IsRequired();

        builder.Property(j => j.UpdatedAtUtc)
            .IsRequired();

        builder.Property(j => j.AssignedTrusteeUserId);

        builder.HasIndex(j => j.AssignedTrusteeUserId);

        builder.Property(j => j.AssignmentSource)
            .HasConversion<string>()
            .HasMaxLength(40);

        builder.Property(j => j.AssignmentRuleId);

        builder.HasIndex(j => j.AssignmentRuleId);
    }
}
