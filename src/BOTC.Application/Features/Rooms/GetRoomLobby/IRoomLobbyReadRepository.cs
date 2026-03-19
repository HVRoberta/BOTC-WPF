using BOTC.Domain.Rooms;

namespace BOTC.Application.Features.Rooms.GetRoomLobby;

public interface IRoomLobbyReadRepository
{
    Task<GetRoomLobbyResult?> GetByRoomCodeAsync(RoomCode roomCode, CancellationToken cancellationToken);
}

