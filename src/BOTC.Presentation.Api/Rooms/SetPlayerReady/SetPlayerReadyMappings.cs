using BOTC.Application.Features.Rooms.SetPlayerReady;
using BOTC.Contracts.Rooms;

namespace BOTC.Presentation.Api.Rooms.SetPlayerReady;

internal static class SetPlayerReadyMappings
{
    public static SetPlayerReadyCommand ToCommand(string roomCode, SetPlayerReadyRequest request)
    {
        return new SetPlayerReadyCommand(roomCode, request.PlayerId, request.IsReady);
    }

    public static SetPlayerReadyResponse ToResponse(SetPlayerReadyResult result)
    {
        return new SetPlayerReadyResponse(
            result.RoomCode.Value,
            result.PlayerId.Value.ToString(),
            result.IsReady);
    }
}

