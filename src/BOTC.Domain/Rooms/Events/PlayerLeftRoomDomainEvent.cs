using BOTC.Domain.Events;
using BOTC.Domain.Rooms.Players;

namespace BOTC.Domain.Rooms.Events;

/// <summary>
/// Raised when a player leaves a room.
/// <see cref="IsRoomDeleted"/> is true when the last player left and the room was removed.
/// </summary>
public sealed record PlayerLeftRoomDomainEvent(
    RoomId RoomId,
    RoomCode RoomCode,
    PlayerId PlayerId,
    PlayerId? NewHostPlayerId,
    bool IsRoomDeleted,
    DateTime OccurredAtUtc) : DomainEvent(OccurredAtUtc);


