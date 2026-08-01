using Bcmp.Application.EmailIntake;
using Bcmp.Application.Jobs;
using Bcmp.Application.Tests.TestDoubles;
using Bcmp.Application.Users;
using Bcmp.Domain.EmailIntake;
using Bcmp.Domain.Jobs;
using Bcmp.Domain.Users;
using FluentAssertions;
using NSubstitute;

namespace Bcmp.Application.Tests.EmailIntake;

[TestFixture]
public class EmailIntakeServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private IEmailInboxClient _inboxClient = null!;
    private IEmailAcknowledgementSender _acknowledgementSender = null!;
    private IEmailIntakeMessageRepository _messageRepository = null!;
    private IJobService _jobService = null!;
    private IUserRepository _userRepository = null!;
    private EmailIntakeService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _inboxClient = Substitute.For<IEmailInboxClient>();
        _acknowledgementSender = Substitute.For<IEmailAcknowledgementSender>();
        _messageRepository = Substitute.For<IEmailIntakeMessageRepository>();
        _jobService = Substitute.For<IJobService>();
        _userRepository = Substitute.For<IUserRepository>();
        _sut = new EmailIntakeService(
            _inboxClient,
            _acknowledgementSender,
            _messageRepository,
            _jobService,
            _userRepository,
            new EmailIntakeOptions
            {
                Enabled = true,
                SystemUserEmail = "email-intake@system.local",
                FolderName = "BCMP-Intake",
                MaxMessagesPerPoll = 10,
            },
            new FixedTimeProvider(Now));
    }

    [Test]
    public async Task PollOnceAsync_WithNewMessage_CreatesJobAndSendsAcknowledgement()
    {
        var message = NewInboxMessage();
        var systemUser = User.Create(Guid.NewGuid(), "email-intake@system.local", "Email Intake", Now, isSystem: true);
        var assignedTrustee = User.Create(Guid.NewGuid(), "terry@example.com", "Terry Smith", Now);
        var otherTrustee = User.Create(Guid.NewGuid(), "alex@example.com", "Alex Jones", Now);
        _inboxClient.FetchUnreadAsync("BCMP-Intake", 10).Returns([message]);
        _messageRepository.ExistsByDedupeKeyAsync(message.ProviderMessageKey, message.MessageId).Returns(false);
        _userRepository.GetByEmailAsync(systemUser.Email).Returns(systemUser);
        _userRepository.GetAllAsync().Returns([systemUser, assignedTrustee, otherTrustee]);
        _jobService.CreateJobAsync(
                null,
                "Leaking pipe",
                Arg.Any<string>(),
                JobSource.Email,
                systemUser.Id,
                Arg.Any<CancellationToken>())
            .Returns(new JobDto(
                Guid.NewGuid(),
                "BCMP-000123",
                null,
                null,
                "Leaking pipe",
                "Email body",
                JobStatus.Open,
                JobSource.Email,
                systemUser.Id,
                Now,
                Now,
                assignedTrustee.Id,
                "Terry Smith",
                null,
                null,
                null));

        var result = await _sut.PollOnceAsync();

        result.Created.Should().Be(1);
        await _acknowledgementSender.Received(1).SendAsync(
            Arg.Is<EmailAcknowledgement>(ack =>
                ack.ToEmail == "resident@example.com"
                && ack.Subject == "Body Corporate request received - Job #BCMP-000123"
                && ack.Body.Contains("It has been assigned to Trustee Terry Smith.")
                && ack.BccEmails.SequenceEqual(new[] { "terry@example.com", "alex@example.com" })),
            Arg.Any<CancellationToken>());
        await _messageRepository.Received(1).AddAsync(
            Arg.Is<EmailIntakeMessage>(record => record.Status == EmailIntakeMessageStatus.Created),
            Arg.Any<CancellationToken>());
        await _inboxClient.Received(1).MarkAsSeenAsync(message, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task PollOnceAsync_WithDuplicateMessage_SkipsJobCreation()
    {
        var message = NewInboxMessage();
        _inboxClient.FetchUnreadAsync("BCMP-Intake", 10).Returns([message]);
        _messageRepository.ExistsByDedupeKeyAsync(message.ProviderMessageKey, message.MessageId).Returns(true);

        var result = await _sut.PollOnceAsync();

        result.DuplicatesSkipped.Should().Be(1);
        await _jobService.DidNotReceive().CreateJobAsync(
            Arg.Any<Guid?>(),
            Arg.Any<string>(),
            Arg.Any<string?>(),
            Arg.Any<JobSource>(),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
        await _inboxClient.Received(1).MarkAsSeenAsync(message, Arg.Any<CancellationToken>());
    }

    private static EmailInboxMessage NewInboxMessage() => new(
        "BCMP-Intake:1:100",
        "BCMP-Intake",
        1,
        100,
        "message-1@example.com",
        "resident@example.com",
        "Resident Name",
        "Leaking pipe",
        "Water is leaking in the bathroom.",
        Now,
        []);
}
