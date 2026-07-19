using Bcmp.Domain.Properties;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bcmp.Infrastructure.Persistence.Configurations;

public sealed class PropertyConfiguration : IEntityTypeConfiguration<Property>
{
    public void Configure(EntityTypeBuilder<Property> builder)
    {
        builder.ToTable("Properties");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(p => p.Name).IsUnique();

        builder.Property(p => p.AddressLine1)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Suburb)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.State)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(p => p.Postcode)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(p => p.CreatedAtUtc)
            .IsRequired();
    }
}
