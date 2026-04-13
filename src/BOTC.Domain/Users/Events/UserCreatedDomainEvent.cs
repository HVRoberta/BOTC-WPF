using BOTC.Domain.Events;

namespace BOTC.Domain.Users.Events;

/// <summary>
/// Raised when a user is created.
/// </summary>
public sealed record UserCreatedDomainEvent(
    UserId UserId,
    string Username,
    string NickName,
    DateTime OccurredAtUtc) : DomainEvent(OccurredAtUtc);