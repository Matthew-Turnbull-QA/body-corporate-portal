using Bcmp.Domain.Assignments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bcmp.Infrastructure.Persistence.Configurations;

public sealed class AssignmentRuleConfiguration : IEntityTypeConfiguration<AssignmentRule>
{
    public void Configure(EntityTypeBuilder<AssignmentRule> builder)
    {
        builder.ToTable("AssignmentRules");

        builder.HasKey(rule => rule.Id);

        builder.Property(rule => rule.Name)
            .IsRequired()
            .HasMaxLength(120);

        builder.Property(rule => rule.IsEnabled)
            .IsRequired();

        builder.Property(rule => rule.Priority)
            .IsRequired();

        builder.HasIndex(rule => rule.Priority);

        builder.Property(rule => rule.TargetTrusteeUserId)
            .IsRequired();

        builder.HasIndex(rule => rule.TargetTrusteeUserId);

        builder.Property(rule => rule.PropertyId);

        builder.HasIndex(rule => rule.PropertyId);

        builder.Property(rule => rule.JobSource)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(rule => rule.Keywords)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(rule => rule.CreatedAtUtc)
            .IsRequired();

        builder.Property(rule => rule.UpdatedAtUtc)
            .IsRequired();
    }
}
