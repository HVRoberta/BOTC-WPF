using BOTC.Domain.Rooms;

namespace BOTC.Application.Features.Rooms.GetRoomLobby;

public sealed class GetRoomLobbyHandler
{
    private readonly IRoomLobbyReadRepository roomLobbyReadRepository;

    public GetRoomLobbyHandler(IRoomLobbyReadRepository roomLobbyReadRepository)
    {
        this.roomLobbyReadRepository = roomLobbyReadRepository
            ?? throw new ArgumentNullException(nameof(roomLobbyReadRepository));
    }

    public async Task<GetRoomLobbyResult> HandleAsync(GetRoomLobbyQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var roomCode = new RoomCode(query.RoomCode);
        var result = await roomLobbyReadRepository.GetByRoomCodeAsync(roomCode, cancellationToken);

        if (result is null)
        {
            throw new RoomLobbyNotFoundException(roomCode);
        }

        return result;
    }
}

