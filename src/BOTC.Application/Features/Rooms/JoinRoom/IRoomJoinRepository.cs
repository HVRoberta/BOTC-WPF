using BOTC.Domain.Rooms;

namespace BOTC.Application.Features.Rooms.JoinRoom;

public interface IRoomJoinRepository
{
    Task<Room?> GetByCodeAsync(RoomCode roomCode, CancellationToken cancellationToken);

    /// <summary>
    /// Tries to persist room changes.
    /// Returns <c>false</c> only for uniqueness conflicts.
    /// Throws <see cref="RoomJoinSaveRoomMissingException"/> when the room no longer exists.
    /// </summary>
    Task<bool> TrySaveAsync(Room room, CancellationToken cancellationToken);
}
