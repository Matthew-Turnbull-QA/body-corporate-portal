using Bcmp.Application.Jobs;
using Bcmp.Application.Users;
using Bcmp.Domain.EmailIntake;
using Bcmp.Domain.Jobs;
using Bcmp.Domain.Users;

namespace Bcmp.Application.EmailIntake;

public sealed class EmailIntakeService(
    IEmailInboxClient inboxClient,
    IEmailAcknowledgementSender acknowledgementSender,
    IEmailIntakeMessageRepository messageRepository,
    IJobService jobService,
    IUserRepository userRepository,
    EmailIntakeOptions options,
    TimeProvider timeProvider) : IEmailIntakeService
{
    public async Task<EmailIntakePollResult> PollOnceAsync(CancellationToken cancellationToken = default)
    {
        if (!options.Enabled)
        {
            return new EmailIntakePollResult(0, 0, 0, 0);
        }

        var maxMessages = Math.Max(1, options.MaxMessagesPerPoll);
        var messages = await inboxClient.FetchUnreadAsync(options.FolderName, maxMessages, cancellationToken);
        var created = 0;
        var duplicates = 0;
        var failed = 0;

        foreach (var message in messages)
        {
            var exists = await messageRepository.ExistsByDedupeKeyAsync(
                message.ProviderMessageKey,
                message.MessageId,
                cancellationToken);
            if (exists)
            {
                duplicates++;
                await inboxClient.MarkAsSeenAsync(message, cancellationToken);
                continue;
            }

            var result = await ProcessMessageAsync(message, cancellationToken);
            if (result == EmailIntakeMessageStatus.Created)
            {
                created++;
            }
            else
            {
                failed++;
            }

            await inboxClient.MarkAsSeenAsync(message, cancellationToken);
        }

        return new EmailIntakePollResult(messages.Count, created, duplicates, failed);
    }

    public async Task<IReadOnlyList<EmailIntakeMessageDto>> GetRecentMessagesAsync(
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        var messages = await messageRepository.GetRecentAsync(Math.Clamp(take, 1, 200), cancellationToken);
        return messages.Select(EmailIntakeMessageDto.FromDomain).ToList();
    }

    private async Task<EmailIntakeMessageStatus> ProcessMessageAsync(
        EmailInboxMessage message,
        CancellationToken cancellationToken)
    {
        var processedAt = timeProvider.GetUtcNow();
        Guid? jobId = null;

        try
        {
            var systemUser = await GetSystemUserAsync(cancellationToken);
            var job = await jobService.CreateJobAsync(
                null,
                BuildTitle(message),
                BuildDescription(message),
                JobSource.Email,
                systemUser.Id,
                cancellationToken);
            jobId = job.Id;

            var trustees = (await userRepository.GetAllAsync(cancellationToken))
                .Where(user => user.IsEnabled && !user.IsSystem)
                .ToList();
            var acknowledgement = BuildAcknowledgement(message, job, trustees);
            await acknowledgementSender.SendAsync(acknowledgement, cancellationToken);

            await messageRepository.AddAsync(CreateIntakeRecord(
                message,
                processedAt,
                EmailIntakeMessageStatus.Created,
                job.Id));
            return EmailIntakeMessageStatus.Created;
        }
        catch (Exception ex)
        {
            await messageRepository.AddAsync(CreateIntakeRecord(
                message,
                processedAt,
                EmailIntakeMessageStatus.Failed,
                jobId,
                ex.Message));
            return EmailIntakeMessageStatus.Failed;
        }
    }

    private async Task<User> GetSystemUserAsync(CancellationToken cancellationToken)
    {
        var normalizedEmail = User.NormalizeEmail(options.SystemUserEmail);
        var user = await userRepository.GetByEmailAsync(normalizedEmail, cancellationToken)
            ?? throw new InvalidOperationException($"Email intake system user '{normalizedEmail}' was not found.");

        if (!user.IsSystem)
        {
            throw new InvalidOperationException($"Configured email intake user '{normalizedEmail}' is not a system user.");
        }

        return user;
    }

    private static string BuildTitle(EmailInboxMessage message)
    {
        var subject = string.IsNullOrWhiteSpace(message.Subject) ? "(No subject)" : message.Subject.Trim();
        return subject.Length <= 200 ? subject : subject[..200];
    }

    private static string BuildDescription(EmailInboxMessage message)
    {
        var attachments = message.AttachmentFileNames.Count == 0
            ? "None"
            : string.Join(", ", message.AttachmentFileNames);
        var sender = string.IsNullOrWhiteSpace(message.SenderDisplayName)
            ? message.SenderEmail
            : $"{message.SenderDisplayName} <{message.SenderEmail}>";

        var description = $"""
        Email request received from: {sender}
        Received: {message.ReceivedAtUtc:u}
        Subject: {message.Subject ?? "(No subject)"}
        Attachments: {attachments}

        {message.BodyText}
        """;

        return description.Length <= 4000 ? description : description[..4000];
    }

    private static EmailAcknowledgement BuildAcknowledgement(
        EmailInboxMessage message,
        JobDto job,
        IReadOnlyList<User> trustees)
    {
        var assignedTrusteeName = job.AssignedTrusteeName ?? "the assigned trustee";
        var subject = $"Body Corporate request received - Job #{job.JobNumber}";
        var body = $"""
        Hi,

        Your Body Corporate request has been logged as Job #{job.JobNumber}.

        It has been assigned to Trustee {assignedTrusteeName}. We aim to respond within 24 hours.

        Regards,
        Rietvlei Body Corporate
        """;

        return new EmailAcknowledgement(
            message.SenderEmail,
            trustees.Select(trustee => trustee.Email).ToList(),
            subject,
            body);
    }

    private static EmailIntakeMessage CreateIntakeRecord(
        EmailInboxMessage message,
        DateTimeOffset processedAt,
        EmailIntakeMessageStatus status,
        Guid? jobId = null,
        string? failureReason = null)
    {
        return EmailIntakeMessage.Create(
            Guid.NewGuid(),
            message.ProviderMessageKey,
            message.MessageId,
            message.SenderEmail,
            message.SenderDisplayName,
            message.Subject,
            message.ReceivedAtUtc,
            processedAt,
            status,
            jobId,
            failureReason);
    }
}
