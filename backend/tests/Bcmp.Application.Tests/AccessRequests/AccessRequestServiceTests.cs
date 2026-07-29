using Bcmp.Application.AccessRequests;
using Bcmp.Application.Auth;
using Bcmp.Application.Tests.TestDoubles;
using Bcmp.Application.Users;
using Bcmp.Domain.AccessRequests;
using Bcmp.Domain.Users;
using FluentAssertions;
using NSubstitute;

namespace Bcmp.Application.Tests.AccessRequests;

[TestFixture]
public class AccessRequestServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private IAccessRequestRepository _accessRequestRepository = null!;
    private IUserRepository _userRepository = null!;
    private IPasswordHasher _passwordHasher = null!;
    private AccessRequestService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _accessRequestRepository = Substitute.For<IAccessRequestRepository>();
        _userRepository = Substitute.For<IUserRepository>();
        _passwordHasher = Substitute.For<IPasswordHasher>();
        _sut = new AccessRequestService(
            _accessRequestRepository,
            _userRepository,
            _passwordHasher,
            new FixedTimeProvider(Now));
    }

    [Test]
    public async Task SubmitAsync_WithNewEmail_CreatesPendingRequestWithoutCreatingUser()
    {
        _accessRequestRepository.GetPendingByEmailAsync("person@example.com").Returns((AccessRequest?)null);
        _userRepository.GetByEmailAsync("person@example.com").Returns((User?)null);

        var result = await _sut.SubmitAsync(
            "Person@Example.com",
            "Pat Person",
            "0123456789",
            "Unit 4",
            AccessRequestRelationship.Owner,
            "Need access");

        result.Email.Should().Be("person@example.com");
        result.Status.Should().Be(AccessRequestStatus.Pending);
        await _accessRequestRepository.Received(1).AddAsync(Arg.Is<AccessRequest>(request => request.Email == "person@example.com"), Arg.Any<CancellationToken>());
        await _userRepository.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SubmitAsync_WithDisabledExistingUser_CreatesReactivationRequest()
    {
        var user = User.Create(Guid.NewGuid(), "person@example.com", "Pat Person", UserRole.Trustee, Now) with { IsEnabled = false };
        _accessRequestRepository.GetPendingByEmailAsync(user.Email).Returns((AccessRequest?)null);
        _userRepository.GetByEmailAsync(user.Email).Returns(user);

        var result = await _sut.SubmitAsync(
            user.Email,
            "Pat Person",
            "0123456789",
            "Unit 4",
            AccessRequestRelationship.Owner,
            "Please reactivate me");

        result.ExistingUserId.Should().Be(user.Id);
        result.ExistingUserIsEnabled.Should().BeFalse();
        await _accessRequestRepository.Received(1).AddAsync(
            Arg.Is<AccessRequest>(request => request.ExistingUserId == user.Id),
            Arg.Any<CancellationToken>());
        await _userRepository.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SubmitAsync_WithEnabledExistingUser_CreatesAdminReviewRequest()
    {
        var user = User.Create(Guid.NewGuid(), "person@example.com", "Pat Person", UserRole.Trustee, Now);
        _accessRequestRepository.GetPendingByEmailAsync(user.Email).Returns((AccessRequest?)null);
        _userRepository.GetByEmailAsync(user.Email).Returns(user);

        var result = await _sut.SubmitAsync(
            user.Email,
            "Pat Person",
            "0123456789",
            "Unit 4",
            AccessRequestRelationship.Owner,
            "I cannot get in");

        result.ExistingUserId.Should().Be(user.Id);
        result.ExistingUserIsEnabled.Should().BeTrue();
        await _accessRequestRepository.Received(1).AddAsync(
            Arg.Is<AccessRequest>(request => request.ExistingUserId == user.Id),
            Arg.Any<CancellationToken>());
        await _userRepository.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SubmitAsync_WithExistingPendingRequest_Throws()
    {
        var existing = AccessRequest.Create(
            Guid.NewGuid(),
            "person@example.com",
            "Pat Person",
            "0123456789",
            "Unit 4",
            AccessRequestRelationship.Owner,
            null,
            Now);
        _accessRequestRepository.GetPendingByEmailAsync(existing.Email).Returns(existing);

        var act = async () => await _sut.SubmitAsync(
            existing.Email,
            "Pat Person",
            "0123456789",
            "Unit 4",
            AccessRequestRelationship.Owner,
            null);

        await act.Should().ThrowAsync<InvalidOperationException>();
        await _accessRequestRepository.DidNotReceive().AddAsync(Arg.Any<AccessRequest>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ApproveAsync_WithPendingRequest_CreatesUserAndMarksApproved()
    {
        var request = AccessRequest.Create(
            Guid.NewGuid(),
            "person@example.com",
            "Pat Person",
            "0123456789",
            "Unit 4",
            AccessRequestRelationship.Owner,
            null,
            Now);
        var reviewerId = Guid.NewGuid();
        _accessRequestRepository.GetByIdAsync(request.Id).Returns(request);
        _userRepository.GetByEmailAsync(request.Email).Returns((User?)null);
        _passwordHasher.HashPassword("password123").Returns("hashed-password");

        var result = await _sut.ApproveAsync(
            request.Id,
            UserRole.Trustee,
            [UserPermission.LoadJobs],
            "password123",
            reviewerId,
            "Approved");

        result.Status.Should().Be(AccessRequestStatus.Approved);
        result.ReviewedByUserId.Should().Be(reviewerId);
        await _userRepository.Received(1).AddAsync(
            Arg.Is<User>(user =>
                user.Email == request.Email
                && user.PasswordHash == "hashed-password"
                && user.Permissions == UserPermission.LoadJobs),
            Arg.Any<CancellationToken>());
        await _accessRequestRepository.Received(1).UpdateAsync(
            Arg.Is<AccessRequest>(updated => updated.Status == AccessRequestStatus.Approved && updated.ApprovedUserId.HasValue),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ApproveAsync_WithExistingDisabledUser_ReactivatesExistingUser()
    {
        var user = User.Create(Guid.NewGuid(), "person@example.com", "Pat Person", UserRole.Trustee, Now) with { IsEnabled = false };
        var request = AccessRequest.Create(
            Guid.NewGuid(),
            user.Email,
            "Pat Person",
            "0123456789",
            "Unit 4",
            AccessRequestRelationship.Owner,
            null,
            Now,
            user.Id);
        _accessRequestRepository.GetByIdAsync(request.Id).Returns(request);
        _userRepository.GetByIdAsync(user.Id).Returns(user);

        var result = await _sut.ApproveAsync(
            request.Id,
            UserRole.Trustee,
            [UserPermission.LoadJobs],
            password: null,
            reviewedByUserId: Guid.NewGuid(),
            reviewNote: null);

        result.Status.Should().Be(AccessRequestStatus.Approved);
        result.ApprovedUserId.Should().Be(user.Id);
        await _userRepository.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await _userRepository.Received(1).UpdateAsync(
            Arg.Is<User>(updated => updated.Id == user.Id && updated.IsEnabled && updated.Permissions == UserPermission.LoadJobs),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RejectAsync_WithPendingRequest_MarksRejectedWithoutCreatingUser()
    {
        var request = AccessRequest.Create(
            Guid.NewGuid(),
            "person@example.com",
            "Pat Person",
            "0123456789",
            "Unit 4",
            AccessRequestRelationship.Owner,
            null,
            Now);
        var reviewerId = Guid.NewGuid();
        _accessRequestRepository.GetByIdAsync(request.Id).Returns(request);

        var result = await _sut.RejectAsync(request.Id, reviewerId, "Not eligible");

        result.Status.Should().Be(AccessRequestStatus.Rejected);
        await _userRepository.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await _accessRequestRepository.Received(1).UpdateAsync(
            Arg.Is<AccessRequest>(updated => updated.Status == AccessRequestStatus.Rejected && updated.ReviewNote == "Not eligible"),
            Arg.Any<CancellationToken>());
    }
}
