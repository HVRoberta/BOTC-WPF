using BOTC.Presentation.Api.Rooms.CreateRoom;
using BOTC.Presentation.Api.Rooms.GetRoomLobby;
using BOTC.Presentation.Api.Rooms.JoinRoom;
using BOTC.Presentation.Api.Rooms.LeaveRoom;
using BOTC.Presentation.Api.Rooms.SetPlayerReady;
using BOTC.Presentation.Api.Rooms.StartGame;

namespace BOTC.Presentation.Api.Rooms;

public static class RoomsEndpoints
{
    public static IEndpointRouteBuilder MapRoomsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints
            .MapGroup("/api/rooms")
            .WithTags("Rooms");

        group.MapCreateRoomEndpoint();
        group.MapGetRoomLobbyEndpoint();
        group.MapJoinRoomEndpoint();
        group.MapLeaveRoomEndpoint();
        group.MapSetPlayerReadyEndpoint();
        group.MapStartGameEndpoint();

        return endpoints;
    }
}