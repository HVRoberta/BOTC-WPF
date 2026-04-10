using BOTC.Domain.Events;
using BOTC.Domain.Rooms;

namespace BOTC.Domain.Rooms.Events;

/// <summary>
/// Raised when a game starts in a room.
/// </summary>
public sealed record GameStartedDomainEvent(
    RoomId RoomId,
    RoomCode RoomCode,
    DateTime OccurredAtUtc) : DomainEvent(OccurredAtUtc);


