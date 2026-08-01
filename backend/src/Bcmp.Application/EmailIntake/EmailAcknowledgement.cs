namespace Bcmp.Application.EmailIntake;

public sealed record EmailAcknowledgement(
    string ToEmail,
    IReadOnlyList<string> BccEmails,
    string Subject,
    string Body);
