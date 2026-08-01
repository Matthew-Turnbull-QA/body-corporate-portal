using Bcmp.Domain.EmailIntake;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bcmp.Infrastructure.Persistence.Configurations;

public sealed class EmailIntakeMessageConfiguration : IEntityTypeConfiguration<EmailIntakeMessage>
{
    public void Configure(EntityTypeBuilder<EmailIntakeMessage> builder)
    {
        builder.ToTable("EmailIntakeMessages");

        builder.HasKey(message => message.Id);

        builder.Property(message => message.ProviderMessageKey)
            .IsRequired()
            .HasMaxLength(500);

        builder.HasIndex(message => message.ProviderMessageKey).IsUnique();

        builder.Property(message => message.MessageId)
            .HasMaxLength(500);

        builder.HasIndex(message => message.MessageId)
            .IsUnique()
            .HasFilter("\"MessageId\" IS NOT NULL");

        builder.Property(message => message.SenderEmail)
            .IsRequired()
            .HasMaxLength(320);

        builder.Property(message => message.SenderDisplayName)
            .HasMaxLength(200);

        builder.Property(message => message.Subject)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(message => message.ReceivedAtUtc)
            .IsRequired();

        builder.Property(message => message.ProcessedAtUtc)
            .IsRequired();

        builder.Property(message => message.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(message => message.JobId);

        builder.HasIndex(message => message.JobId);

        builder.Property(message => message.FailureReason)
            .HasMaxLength(1000);
    }
}
