using BOTC.Domain.Events;

namespace BOTC.Domain.Rooms.Events;

/// <summary>
/// Raised when a player successfully joins a room.
/// </summary>
public sealed record PlayerJoinedRoomDomainEvent(
    RoomId RoomId,
    RoomCode RoomCode,
    PlayerId PlayerId,
    DateTime OccurredAtUtc) : DomainEvent(OccurredAtUtc);
    