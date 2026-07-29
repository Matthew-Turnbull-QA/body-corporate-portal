namespace Bcmp.Domain.Users;

[Flags]
public enum UserPermission
{
    None = 0,
    LoadJobs = 1,
    CreateJobs = 2,
    UpdateJobStatus = 4,
    AssignJobs = 8,
}
