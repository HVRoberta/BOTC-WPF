using BOTC.Application.Features.Rooms.LeaveRoom;
using BOTC.Contracts.Rooms;

namespace BOTC.Presentation.Api.Rooms.LeaveRoom;

internal static class LeaveRoomMappings
{
    public static LeaveRoomCommand ToCommand(string roomCode, LeaveRoomRequest request)
    {
        return new LeaveRoomCommand(roomCode, request.PlayerId);
    }

    public static LeaveRoomResponse ToResponse(LeaveRoomResult result)
    {
        return new LeaveRoomResponse(
            result.RoomCode.Value,
            result.PlayerId.Value.ToString(),
            result.RoomWasRemoved,
            result.NewHostPlayerId?.Value.ToString());
    }
}

