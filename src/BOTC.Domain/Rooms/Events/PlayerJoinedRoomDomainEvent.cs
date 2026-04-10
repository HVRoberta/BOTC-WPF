using BOTC.Domain.Events;
using BOTC.Domain.Rooms.Players;

namespace BOTC.Domain.Rooms.Events;

/// <summary>
/// Raised when a player successfully joins a room.
/// </summary>
public sealed record PlayerJoinedRoomDomainEvent(
    RoomId RoomId,
    RoomCode RoomCode,
    PlayerId PlayerId,
    DateTime OccurredAtUtc) : DomainEvent(OccurredAtUtc);
    