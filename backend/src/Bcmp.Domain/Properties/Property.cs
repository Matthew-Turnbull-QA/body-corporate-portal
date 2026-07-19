namespace Bcmp.Domain.Properties;

public sealed record Property
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string AddressLine1 { get; init; }
    public required string Suburb { get; init; }
    public required string State { get; init; }
    public required string Postcode { get; init; }
    public required DateTimeOffset CreatedAtUtc { get; init; }

    public static Property Create(
        Guid id,
        string name,
        string addressLine1,
        string suburb,
        string state,
        string postcode,
        DateTimeOffset createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name cannot be empty.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(addressLine1))
        {
            throw new ArgumentException("Address line 1 cannot be empty.", nameof(addressLine1));
        }

        if (string.IsNullOrWhiteSpace(suburb))
        {
            throw new ArgumentException("Suburb cannot be empty.", nameof(suburb));
        }

        if (string.IsNullOrWhiteSpace(state))
        {
            throw new ArgumentException("State cannot be empty.", nameof(state));
        }

        if (string.IsNullOrWhiteSpace(postcode))
        {
            throw new ArgumentException("Postcode cannot be empty.", nameof(postcode));
        }

        return new Property
        {
            Id = id,
            Name = name.Trim(),
            AddressLine1 = addressLine1.Trim(),
            Suburb = suburb.Trim(),
            State = state.Trim().ToUpperInvariant(),
            Postcode = postcode.Trim(),
            CreatedAtUtc = createdAtUtc,
        };
    }
}
