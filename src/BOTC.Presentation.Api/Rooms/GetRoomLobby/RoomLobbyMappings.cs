using BOTC.Application.Features.Rooms.GetRoomLobby;
using BOTC.Contracts.Rooms;
using BOTC.Domain.Rooms;

namespace BOTC.Presentation.Api.Rooms.GetRoomLobby;

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
            result.Players.Select(ToLobbyPlayerResponse).ToArray(),
            ToContractStatus(result.Status));
    }

    private static LobbyPlayerResponse ToLobbyPlayerResponse(LobbyPlayerResult player)
    {
        return new LobbyPlayerResponse(
            player.PlayerId.Value.ToString(),
            player.Name,
            ToIsHost(player.Role),
            player.IsReady);
    }

    private static bool ToIsHost(PlayerRole role)
    {
        return role switch
        {
            PlayerRole.Host => true,
            PlayerRole.Player => false,
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unsupported room player role.")
        };
    }

    private static RoomStatusContract ToContractStatus(RoomStatus status)
    {
        return status switch
        {
            RoomStatus.WaitingForPlayers => RoomStatusContract.WaitingForPlayers,
            RoomStatus.InProgress => RoomStatusContract.InProgress,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unsupported room status.")
        };
    }
}

