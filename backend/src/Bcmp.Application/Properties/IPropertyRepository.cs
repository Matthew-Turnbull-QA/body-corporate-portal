using Bcmp.Domain.Properties;

namespace Bcmp.Application.Properties;

public interface IPropertyRepository
{
    Task<Property?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Property?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Property>> GetAllAsync(CancellationToken cancellationToken = default);

    Task AddAsync(Property property, CancellationToken cancellationToken = default);

    Task UpdateAsync(Property property, CancellationToken cancellationToken = default);
}
