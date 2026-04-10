using BOTC.Application.Features.Rooms.JoinRoom;
using BOTC.Contracts.Rooms;

namespace BOTC.Presentation.Api.Rooms.JoinRoom;

internal static class JoinRoomMappings
{
    public static JoinRoomCommand ToCommand(string roomCode, JoinRoomRequest request)
    {
        return new JoinRoomCommand(roomCode, request.UserId, request.Name);
    }

    public static JoinRoomResponse ToResponse(JoinRoomResult result)
    {
        return new JoinRoomResponse(
            result.RoomCode.Value,
            result.PlayerId.Value.ToString(),
            result.Name);
    }
}

