using Bcmp.Domain.Properties;

namespace Bcmp.Application.Properties;

public sealed class PropertyService(IPropertyRepository propertyRepository, TimeProvider timeProvider) : IPropertyService
{
    public async Task<IReadOnlyList<PropertyDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var properties = await propertyRepository.GetAllAsync(cancellationToken);
        return properties.Select(PropertyDto.FromDomain).ToList();
    }

    public async Task<PropertyDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var property = await propertyRepository.GetByIdAsync(id, cancellationToken);
        return property is null ? null : PropertyDto.FromDomain(property);
    }

    public async Task<PropertyDto> AddPropertyAsync(
        string name,
        string addressLine1,
        string suburb,
        string state,
        string postcode,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = NormalizeName(name);
        var existing = await propertyRepository.GetByNameAsync(normalizedName, cancellationToken);
        if (existing is not null)
        {
            throw new InvalidOperationException($"A property with name '{normalizedName}' already exists.");
        }

        var property = Property.Create(Guid.NewGuid(), name, addressLine1, suburb, state, postcode, timeProvider.GetUtcNow());
        await propertyRepository.AddAsync(property, cancellationToken);
        return PropertyDto.FromDomain(property);
    }

    public async Task<PropertyDto> UpdatePropertyAsync(
        Guid id,
        string name,
        string addressLine1,
        string suburb,
        string state,
        string postcode,
        CancellationToken cancellationToken = default)
    {
        var property = await propertyRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Property '{id}' was not found.");

        var updated = property with
        {
            Name = name.Trim(),
            AddressLine1 = addressLine1.Trim(),
            Suburb = suburb.Trim(),
            State = state.Trim().ToUpperInvariant(),
            Postcode = postcode.Trim(),
        };

        await propertyRepository.UpdateAsync(updated, cancellationToken);
        return PropertyDto.FromDomain(updated);
    }

    private static string NormalizeName(string name) => name.Trim().ToLowerInvariant();
}
