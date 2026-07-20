namespace Bcmp.Domain.Jobs;

/// <summary>
/// How the job entered the system. Only <see cref="Manual"/> is produced today;
/// the value exists so a future email-ingestion path is a new caller of
/// JobService.CreateJobAsync, not a refactor of it.
/// </summary>
public enum JobSource
{
    Manual,
    Email,
}
