using BOTC.Domain.Events;

namespace BOTC.Domain.Rooms.Events;

/// <summary>
/// Raised when a game starts in a room.
/// </summary>
public sealed record GameStartedDomainEvent(
    RoomId RoomId,
    RoomCode RoomCode,
    DateTime OccurredAtUtc) : DomainEvent(OccurredAtUtc);


