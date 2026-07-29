using Bcmp.Domain.Users;

namespace Bcmp.Domain.AccessRequests;

public sealed record AccessRequest
{
    public required Guid Id { get; init; }
    public required string Email { get; init; }
    public required string DisplayName { get; init; }
    public required string PhoneNumber { get; init; }
    public required string PropertyOrUnit { get; init; }
    public required AccessRequestRelationship Relationship { get; init; }
    public required string Message { get; init; }
    public required AccessRequestStatus Status { get; init; }
    public required DateTimeOffset CreatedAtUtc { get; init; }
    public DateTimeOffset? ReviewedAtUtc { get; init; }
    public Guid? ReviewedByUserId { get; init; }
    public Guid? ExistingUserId { get; init; }
    public Guid? ApprovedUserId { get; init; }
    public string? ReviewNote { get; init; }

    public static AccessRequest Create(
        Guid id,
        string email,
        string displayName,
        string phoneNumber,
        string propertyOrUnit,
        AccessRequestRelationship relationship,
        string? message,
        DateTimeOffset createdAtUtc,
        Guid? existingUserId = null)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email cannot be empty.", nameof(email));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Display name cannot be empty.", nameof(displayName));
        }

        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            throw new ArgumentException("Phone number cannot be empty.", nameof(phoneNumber));
        }

        if (string.IsNullOrWhiteSpace(propertyOrUnit))
        {
            throw new ArgumentException("Property or unit cannot be empty.", nameof(propertyOrUnit));
        }

        return new AccessRequest
        {
            Id = id,
            Email = User.NormalizeEmail(email),
            DisplayName = displayName.Trim(),
            PhoneNumber = phoneNumber.Trim(),
            PropertyOrUnit = propertyOrUnit.Trim(),
            Relationship = relationship,
            Message = message?.Trim() ?? string.Empty,
            Status = AccessRequestStatus.Pending,
            CreatedAtUtc = createdAtUtc,
            ExistingUserId = existingUserId,
        };
    }
}
