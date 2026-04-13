using BOTC.Application.Features.Rooms.GetRoomLobby;
using BOTC.Domain.Rooms;

public interface IRoomLobbyQueryService
{
    Task<GetRoomLobbyResult?> GetByRoomCodeAsync(RoomCode roomCode, CancellationToken cancellationToken);
}