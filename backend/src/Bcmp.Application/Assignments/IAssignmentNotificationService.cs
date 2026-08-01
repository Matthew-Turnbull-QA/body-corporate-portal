namespace Bcmp.Application.Assignments;

public interface IAssignmentNotificationService
{
    Task<IReadOnlyList<AssignmentNotificationDto>> GetForUserAsync(
        Guid userId,
        int take = 100,
        CancellationToken cancellationToken = default);
}
