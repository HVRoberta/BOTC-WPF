using BOTC.Domain.Events;

namespace BOTC.Domain.Rooms.Events;

/// <summary>
/// Raised when a room is created.
/// </summary>
public sealed record RoomCreatedDomainEvent(
    RoomId RoomId,
    RoomCode RoomCode,
    PlayerId HostPlayerId,
    DateTime OccurredAtUtc) : DomainEvent(OccurredAtUtc);


