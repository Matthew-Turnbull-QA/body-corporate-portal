using Bcmp.Domain.Jobs;

namespace Bcmp.Application.Jobs;

public interface IJobService
{
    Task<IReadOnlyList<JobDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<JobDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// The single entry point for job creation regardless of how the job originated
    /// (manual entry today; a future email-ingestion path calls this same method
    /// with <see cref="JobSource.Email"/> instead of adding a parallel creation path).
    /// </summary>
    Task<JobDto> CreateJobAsync(
        Guid propertyId,
        string title,
        string? description,
        JobSource source,
        Guid createdByUserId,
        CancellationToken cancellationToken = default);

    Task<JobDto> UpdateStatusAsync(Guid id, JobStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Assigns (or, with a null <paramref name="trusteeUserId"/>, clears) the job's assigned trustee.
    /// </summary>
    /// <exception cref="KeyNotFoundException">No job or trustee with the given id exists.</exception>
    /// <exception cref="ArgumentException"><paramref name="trusteeUserId"/> refers to a user who is not a Trustee.</exception>
    Task<JobDto> AssignTrusteeAsync(Guid id, Guid? trusteeUserId, CancellationToken cancellationToken = default);
}
