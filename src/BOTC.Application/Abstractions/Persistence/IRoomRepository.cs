using BOTC.Application.Features.Rooms.GetRoomLobby;
using BOTC.Domain.Rooms;

namespace BOTC.Application.Abstractions.Persistence;

/// <summary>
/// Defines persistence operations for <see cref="Room"/> aggregates.
/// </summary>
public interface IRoomRepository
{
    /// <summary>
    /// Attempts to persist a new room.
    /// Returns <c>false</c> when the room cannot be added because the room code is already taken.
    /// </summary>
    Task<bool> TryAddAsync(Room room, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves the full <see cref="Room"/> aggregate identified by the given <paramref name="roomCode"/>.
    /// Returns <c>null</c> when no matching room exists.
    /// </summary>
    Task<Room?> GetByCodeAsync(RoomCode roomCode, CancellationToken cancellationToken);

    /// <summary>
    /// Tries to persist room changes.
    /// Returns <c>false</c> only for uniqueness conflicts.
    /// Throws <see cref="RoomJoinSaveRoomMissingException"/> when the room no longer exists.
    /// </summary>
    Task<bool> TrySaveAsync(Room room, CancellationToken cancellationToken);

    /// <summary>
    /// Attempts to delete the room identified by the given <paramref name="roomId"/>.
    /// Returns <c>false</c> when no matching room was found.
    /// </summary>
    Task<bool> TryDeleteAsync(RoomId roomId, CancellationToken cancellationToken);
}
