using BOTC.Application.Features.Rooms.GetRoomLobby;
using BOTC.Contracts.Rooms;
using BOTC.Domain.Rooms;

namespace BOTC.Presentation.Api.Rooms;

internal static class RoomLobbyMappings
{
    public static GetRoomLobbyQuery ToQuery(string roomCode)
    {
        return new GetRoomLobbyQuery(roomCode);
    }

    public static GetRoomLobbyResponse ToResponse(GetRoomLobbyResult result)
    {
        return new GetRoomLobbyResponse(
            result.RoomCode.Value,
            result.HostDisplayName,
            ToContractStatus(result.Status));
    }

    private static RoomStatusContract ToContractStatus(RoomStatus status)
    {
        return status switch
        {
            RoomStatus.WaitingForPlayers => RoomStatusContract.WaitingForPlayers,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unsupported room status.")
        };
    }
}

