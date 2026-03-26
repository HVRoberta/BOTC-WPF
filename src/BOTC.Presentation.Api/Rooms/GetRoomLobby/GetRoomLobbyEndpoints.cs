using BOTC.Application.Features.Rooms.GetRoomLobby;
using BOTC.Contracts.Rooms;

namespace BOTC.Presentation.Api.Rooms;

public static class GetRoomLobbyEndpoints
{
    public static IEndpointRouteBuilder MapGetRoomLobbyEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/rooms/{roomCode}/lobby", GetRoomLobbyAsync)
            .WithName("GetRoomLobby")
            .WithTags("Rooms")
            .Produces<GetRoomLobbyResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static async Task<IResult> GetRoomLobbyAsync(
        string roomCode,
        GetRoomLobbyHandler handler,
        CancellationToken cancellationToken)
    {
        try
        {
            var query = RoomLobbyMappings.ToQuery(roomCode);
            var result = await handler.HandleAsync(query, cancellationToken);
            var response = RoomLobbyMappings.ToResponse(result);

            return Results.Ok(response);
        }
        catch (RoomLobbyNotFoundException exception)
        {
            return RoomProblemResults.NotFound("Room not found.", exception.Message);
        }
        catch (ArgumentException exception)
        {
            return RoomProblemResults.BadRequest("Invalid room code.", exception.Message);
        }
    }
}
