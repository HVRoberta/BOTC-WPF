using BOTC.Domain.Rooms;

namespace BOTC.Application.Features.Rooms.LeaveRoom;

public interface IRoomLeaveRepository
{
    Task<Room?> GetByCodeAsync(RoomCode roomCode, CancellationToken cancellationToken);

    /// <summary>
    /// Tries to persist room changes.
    /// Returns <c>false</c> only for conflicting persistence constraints.
    /// Throws <see cref="RoomLeaveSaveRoomMissingException"/> when the room no longer exists.
    /// </summary>
    Task<bool> TrySaveAsync(Room room, CancellationToken cancellationToken);

    Task<bool> TryDeleteAsync(RoomId roomId, CancellationToken cancellationToken);
}

