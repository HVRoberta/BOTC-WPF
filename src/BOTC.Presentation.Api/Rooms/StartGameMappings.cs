using BOTC.Application.Features.Rooms.StartGame;
using BOTC.Contracts.Rooms;
using BOTC.Domain.Rooms;

namespace BOTC.Presentation.Api.Rooms;

internal static class StartGameMappings
{
    public static StartGameCommand ToCommand(string roomCode, StartGameRequest request)
    {
        return new StartGameCommand(roomCode, request.StarterPlayerId);
    }

    public static StartGameResponse ToResponse(StartGameResult result)
    {
        return new StartGameResponse(
            result.RoomCode.Value,
            result.StarterPlayerId.Value.ToString(),
            result.IsStarted,
            ToBlockedReasonContract(result.BlockedReason),
            ToContractStatus(result.RoomStatus));
    }

    private static StartGameBlockedReasonContract? ToBlockedReasonContract(RoomStartGameBlockedReason? blockedReason)
    {
        return blockedReason switch
        {
            null => null,
            RoomStartGameBlockedReason.StartedByNonHost => StartGameBlockedReasonContract.StartedByNonHost,
            RoomStartGameBlockedReason.RoomIsNotWaitingForPlayers => StartGameBlockedReasonContract.RoomIsNotWaitingForPlayers,
            RoomStartGameBlockedReason.NotEnoughPlayers => StartGameBlockedReasonContract.NotEnoughPlayers,
            RoomStartGameBlockedReason.NonHostPlayersNotReady => StartGameBlockedReasonContract.NonHostPlayersNotReady,
            RoomStartGameBlockedReason.StarterPlayerNotFound => StartGameBlockedReasonContract.StarterPlayerNotFound,
            _ => throw new ArgumentOutOfRangeException(nameof(blockedReason), blockedReason, "Unsupported blocked reason.")
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

