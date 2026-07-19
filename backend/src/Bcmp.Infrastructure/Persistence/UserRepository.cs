using Bcmp.Application.Users;
using Bcmp.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Bcmp.Infrastructure.Persistence;

public sealed class UserRepository(AppDbContext dbContext) : IUserRepository
{
    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Users.SingleOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default) =>
        dbContext.Users.SingleOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Users.OrderBy(u => u.DisplayName).ToListAsync(cancellationToken);

    public Task<int> CountEnabledAdministratorsAsync(CancellationToken cancellationToken = default) =>
        dbContext.Users.CountAsync(u => u.Role == UserRole.Administrator && u.IsEnabled, cancellationToken);

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        dbContext.Users.Update(user);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
