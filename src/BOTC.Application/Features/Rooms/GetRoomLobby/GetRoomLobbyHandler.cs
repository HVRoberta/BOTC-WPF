using BOTC.Application.Abstractions.Persistence;
using BOTC.Domain.Rooms;

namespace BOTC.Application.Features.Rooms.GetRoomLobby;

public sealed class GetRoomLobbyHandler
{
    private readonly IRoomLobbyQueryService _roomLobbyQueryService;

    public GetRoomLobbyHandler(IRoomLobbyQueryService roomLobbyQueryService)
    {
        _roomLobbyQueryService = roomLobbyQueryService
                                 ?? throw new ArgumentNullException(nameof(roomLobbyQueryService));
    }

    public async Task<GetRoomLobbyResult> HandleAsync(GetRoomLobbyQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var roomCode = new RoomCode(query.RoomCode);
        var result = await _roomLobbyQueryService.GetByRoomCodeAsync(roomCode, cancellationToken);

        if (result is null)
        {
            throw new RoomLobbyNotFoundException(roomCode);
        }

        return result;
    }
}

