using BOTC.Application.Features.Rooms.CreateRoom;
using BOTC.Contracts.Rooms;

namespace BOTC.Presentation.Api.Rooms.CreateRoom;

internal static class CreateRoomMappings
{
    public static CreateRoomCommand ToCommand(CreateRoomRequest request)
    {
        return new CreateRoomCommand(request.HostUserId, request.HostName);
    }

    public static CreateRoomResponse ToResponse(CreateRoomResult result)
    {
        return new CreateRoomResponse(
            result.RoomId.Value.ToString(),
            result.RoomCode.Value,
            result.HostPlayerId.Value.ToString(),
            result.CreatedAtUtc);
    }
}
