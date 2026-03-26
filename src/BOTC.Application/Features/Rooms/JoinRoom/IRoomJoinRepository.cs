using BOTC.Domain.Rooms;

namespace BOTC.Application.Features.Rooms.JoinRoom;

public interface IRoomJoinRepository
{
    Task<Room?> GetByCodeAsync(RoomCode roomCode, CancellationToken cancellationToken);

    Task<bool> TrySaveAsync(Room room, CancellationToken cancellationToken);
}

