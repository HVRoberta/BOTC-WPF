using BOTC.Application.Features.Rooms.StartGame;
using BOTC.Contracts.Rooms;
using BOTC.Presentation.Api.Rooms.Common;

namespace BOTC.Presentation.Api.Rooms.StartGame;

public static class StartGameEndpoint
{
    public static RouteGroupBuilder MapStartGameEndpoint(this RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);
        
        group.MapPost("/api/rooms/{roomCode}/start", HandleAsync)
            .WithName("StartGame")
            .WithTags("Rooms")
            .Produces<StartGameResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        
        return group;
    }
    
    private static async Task<IResult> HandleAsync(
        string roomCode,
        StartGameRequest request,
        StartGameHandler handler,
        CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.StarterPlayerId))
        {
            return RoomProblemResults.BadRequest("Invalid start game request.", "StarterPlayerId is required.");
        }

        try
        {
            var command = StartGameMappings.ToCommand(roomCode, request);
            var result = await handler.HandleAsync(command, cancellationToken);
            var response = StartGameMappings.ToResponse(result);

            return Results.Ok(response);
        }
        catch (RoomStartGameRoomNotFoundException exception)
        {
            return RoomProblemResults.NotFound("Room not found.", exception.Message);
        }
        catch (RoomStartGameConflictException exception)
        {
            return RoomProblemResults.Conflict("Unable to start game.", exception.Message);
        }
        catch (ArgumentException exception)
        {
            return RoomProblemResults.BadRequest("Invalid start game request.", exception.Message);
        }
    }
}