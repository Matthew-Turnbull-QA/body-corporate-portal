using Bcmp.Domain.Assignments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bcmp.Infrastructure.Persistence.Configurations;

public sealed class AssignmentNotificationConfiguration : IEntityTypeConfiguration<AssignmentNotification>
{
    public void Configure(EntityTypeBuilder<AssignmentNotification> builder)
    {
        builder.ToTable("AssignmentNotifications");

        builder.HasKey(notification => notification.Id);

        builder.Property(notification => notification.RecipientUserId)
            .IsRequired();

        builder.HasIndex(notification => notification.RecipientUserId);

        builder.Property(notification => notification.JobId);

        builder.HasIndex(notification => notification.JobId);

        builder.Property(notification => notification.Type)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(40);

        builder.Property(notification => notification.Subject)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(notification => notification.Message)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(notification => notification.CreatedAtUtc)
            .IsRequired();

        builder.Property(notification => notification.EmailSentAtUtc);

        builder.Property(notification => notification.EmailFailureReason)
            .HasMaxLength(1000);
    }
}
