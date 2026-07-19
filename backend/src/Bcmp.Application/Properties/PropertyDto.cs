using Bcmp.Domain.Properties;

namespace Bcmp.Application.Properties;

public sealed record PropertyDto(
    Guid Id,
    string Name,
    string AddressLine1,
    string Suburb,
    string State,
    string Postcode,
    DateTimeOffset CreatedAtUtc)
{
    public static PropertyDto FromDomain(Property property) => new(
        property.Id,
        property.Name,
        property.AddressLine1,
        property.Suburb,
        property.State,
        property.Postcode,
        property.CreatedAtUtc);
}
