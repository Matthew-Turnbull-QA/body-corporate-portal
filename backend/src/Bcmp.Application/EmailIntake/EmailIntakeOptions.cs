namespace Bcmp.Application.EmailIntake;

public sealed class EmailIntakeOptions
{
    public const string SectionName = "EmailIntake";

    public bool Enabled { get; init; }
    public string SystemUserEmail { get; init; } = "email-intake@system.local";
    public string FolderName { get; init; } = "INBOX";
    public int PollIntervalSeconds { get; init; } = 300;
    public int MaxMessagesPerPoll { get; init; } = 10;
}
