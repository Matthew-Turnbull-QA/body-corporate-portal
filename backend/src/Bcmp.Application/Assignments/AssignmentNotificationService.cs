using Bcmp.Application.Jobs;

namespace Bcmp.Application.Assignments;

public sealed class AssignmentNotificationService(
    IAssignmentNotificationRepository assignmentNotificationRepository,
    IJobRepository jobRepository) : IAssignmentNotificationService
{
    public async Task<IReadOnlyList<AssignmentNotificationDto>> GetForUserAsync(
        Guid userId,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        var notifications = await assignmentNotificationRepository.GetForRecipientAsync(
            userId,
            Math.Clamp(take, 1, 200),
            cancellationToken);
        var jobs = await jobRepository.GetAllAsync(cancellationToken);
        var jobNumbers = jobs.ToDictionary(job => job.Id, job => job.JobNumber);

        return notifications
            .Select(notification => AssignmentNotificationDto.FromDomain(
                notification,
                notification.JobId is Guid jobId ? jobNumbers.GetValueOrDefault(jobId) : null))
            .ToList();
    }
}
