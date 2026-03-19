using BOTC.Domain.Rooms;

namespace BOTC.Application.Abstractions.Persistence;

public interface IRoomRepository
{
    /// <summary>
    /// Attempts to persist a new room.
    /// Returns false when the room cannot be added because the room code is already taken.
    /// </summary>
    Task<bool> TryAddAsync(Room room, CancellationToken cancellationToken);
}
