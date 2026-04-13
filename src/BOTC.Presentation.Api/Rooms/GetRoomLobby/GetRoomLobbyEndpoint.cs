using BOTC.Application.Features.Rooms.GetRoomLobby;
using BOTC.Contracts.Rooms;
using Microsoft.AspNetCore.Mvc;

namespace BOTC.Presentation.Api.Rooms.GetRoomLobby;

public static class GetRoomLobbyEndpoint
{
    public static RouteGroupBuilder MapGetRoomLobbyEndpoint(this RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);
        
        group.MapGet("/{roomCode}/lobby", HandleAsync)
            .WithName("GetRoomLobby")
            .WithTags("Rooms")
            .Produces<GetRoomLobbyResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return group;
    }
    
    private static async Task<IResult> HandleAsync(
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
            return Results.NotFound(new ProblemDetails
            {
                Title = "Room not found.",
                Detail = exception.Message,
                Status = StatusCodes.Status404NotFound
            });
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(new ProblemDetails
            {
                Title = "Invalid room code.",
                Detail = exception.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }
}