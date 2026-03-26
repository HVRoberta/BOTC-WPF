namespace BOTC.Presentation.Api.Rooms;

public static class RoomsEndpoints
{
    public static IEndpointRouteBuilder MapRoomsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapCreateRoomEndpoint();
        endpoints.MapJoinRoomEndpoint();
        endpoints.MapGetRoomLobbyEndpoint();

        return endpoints;
    }
}
