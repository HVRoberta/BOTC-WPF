using BOTC.Domain.Events;
using BOTC.Domain.Rooms.Players;

namespace BOTC.Domain.Rooms.Events;

/// <summary>
/// Raised when a room is created.
/// </summary>
public sealed record RoomCreatedDomainEvent(
    RoomId RoomId,
    RoomCode RoomCode,
    PlayerId HostPlayerId,
    DateTime OccurredAtUtc) : DomainEvent(OccurredAtUtc);


