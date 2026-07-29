using Bcmp.Domain.AccessRequests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bcmp.Infrastructure.Persistence.Configurations;

public sealed class AccessRequestConfiguration : IEntityTypeConfiguration<AccessRequest>
{
    public void Configure(EntityTypeBuilder<AccessRequest> builder)
    {
        builder.ToTable("AccessRequests");

        builder.HasKey(request => request.Id);

        builder.Property(request => request.Email)
            .IsRequired()
            .HasMaxLength(320);

        builder.HasIndex(request => request.Email);
        builder.HasIndex(request => new { request.Email, request.Status });
        builder.HasIndex(request => request.ExistingUserId);

        builder.Property(request => request.DisplayName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(request => request.PhoneNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(request => request.PropertyOrUnit)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(request => request.Relationship)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(request => request.Message)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(request => request.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(request => request.CreatedAtUtc)
            .IsRequired();

        builder.Property(request => request.ReviewNote)
            .HasMaxLength(2000);
    }
}
