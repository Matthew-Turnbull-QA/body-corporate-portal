using Bcmp.Domain.AccessRequests;

namespace Bcmp.Application.AccessRequests;

public interface IAccessRequestRepository
{
    Task<AccessRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<AccessRequest?> GetPendingByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AccessRequest>> GetAllAsync(CancellationToken cancellationToken = default);

    Task AddAsync(AccessRequest request, CancellationToken cancellationToken = default);

    Task UpdateAsync(AccessRequest request, CancellationToken cancellationToken = default);
}
