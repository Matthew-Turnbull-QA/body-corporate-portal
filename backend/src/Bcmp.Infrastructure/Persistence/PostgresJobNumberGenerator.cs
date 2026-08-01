using Bcmp.Application.Jobs;
using Microsoft.EntityFrameworkCore;

namespace Bcmp.Infrastructure.Persistence;

public sealed class PostgresJobNumberGenerator(AppDbContext dbContext) : IJobNumberGenerator
{
    public async Task<string> GenerateNextAsync(CancellationToken cancellationToken = default)
    {
        var nextValue = await dbContext.Database
            .SqlQueryRaw<long>("SELECT nextval('\"JobNumberSequence\"') AS \"Value\"")
            .SingleAsync(cancellationToken);

        return $"BCMP-{nextValue:000000}";
    }
}
