namespace Bcmp.Application.Properties;

public interface IPropertyService
{
    Task<IReadOnlyList<PropertyDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<PropertyDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PropertyDto> AddPropertyAsync(
        string name,
        string addressLine1,
        string suburb,
        string state,
        string postcode,
        CancellationToken cancellationToken = default);

    Task<PropertyDto> UpdatePropertyAsync(
        Guid id,
        string name,
        string addressLine1,
        string suburb,
        string state,
        string postcode,
        CancellationToken cancellationToken = default);
}
