using Bcmp.Application.Properties;
using Bcmp.Domain.Properties;
using Microsoft.EntityFrameworkCore;

namespace Bcmp.Infrastructure.Persistence;

public sealed class PropertyRepository(AppDbContext dbContext) : IPropertyRepository
{
    public async Task<Property?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await dbContext.Properties.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<Property?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        => await dbContext.Properties.FirstOrDefaultAsync(p => p.Name.ToLower() == name.ToLower(), cancellationToken);

    public async Task<IReadOnlyList<Property>> GetAllAsync(CancellationToken cancellationToken = default)
        => await dbContext.Properties.OrderBy(p => p.Name).ToListAsync(cancellationToken);

    public async Task AddAsync(Property property, CancellationToken cancellationToken = default)
    {
        dbContext.Properties.Add(property);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Property property, CancellationToken cancellationToken = default)
    {
        dbContext.Properties.Update(property);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
