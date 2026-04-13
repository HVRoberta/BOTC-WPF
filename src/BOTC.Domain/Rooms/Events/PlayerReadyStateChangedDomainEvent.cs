using BOTC.Domain.Events;

namespace BOTC.Domain.Rooms.Events;

/// <summary>
/// Raised when a player's ready state changes.
/// </summary>
public sealed record PlayerReadyStateChangedDomainEvent(
    RoomId RoomId,
    RoomCode RoomCode,
    PlayerId PlayerId,
    bool IsReady,
    DateTime OccurredAtUtc) : DomainEvent(OccurredAtUtc);


