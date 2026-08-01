namespace Bcmp.Application.Jobs;

public interface IJobNumberGenerator
{
    Task<string> GenerateNextAsync(CancellationToken cancellationToken = default);
}
