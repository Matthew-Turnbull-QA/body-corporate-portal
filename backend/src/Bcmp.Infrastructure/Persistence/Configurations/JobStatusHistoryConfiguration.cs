using Bcmp.Domain.Jobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bcmp.Infrastructure.Persistence.Configurations;

public sealed class JobStatusHistoryConfiguration : IEntityTypeConfiguration<JobStatusHistory>
{
    public void Configure(EntityTypeBuilder<JobStatusHistory> builder)
    {
        builder.ToTable("JobStatusHistory");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.JobId)
            .IsRequired();

        builder.HasIndex(h => h.JobId);

        builder.Property(h => h.FromStatus)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(h => h.ToStatus)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(h => h.Note)
            .HasMaxLength(4000);

        builder.Property(h => h.ChangedByUserId)
            .IsRequired();

        builder.Property(h => h.ChangedAtUtc)
            .IsRequired();

        builder.Property(h => h.NoteEditedByUserId);

        builder.Property(h => h.NoteEditedAtUtc);
    }
}
