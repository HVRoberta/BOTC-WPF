using BOTC.Application.Features.Rooms.SetPlayerReady;
using BOTC.Contracts.Rooms;
using BOTC.Presentation.Api.Rooms.Common;

namespace BOTC.Presentation.Api.Rooms.SetPlayerReady;

public static class SetPlayerReadyEndpoint
{
    public static RouteGroupBuilder MapSetPlayerReadyEndpoint(this RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);
        
        group.MapPost("/{roomCode}/ready", HandleAsync)
            .WithName("SetPlayerReady")
            .WithTags("Rooms")
            .Produces<SetPlayerReadyResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return group;
    }
    
    private static async Task<IResult> HandleAsync(
        string roomCode,
        SetPlayerReadyRequest request,
        SetPlayerReadyHandler handler,
        CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.PlayerId))
        {
            return RoomProblemResults.BadRequest("Invalid ready request.", "PlayerId is required.");
        }

        try
        {
            var command = SetPlayerReadyMappings.ToCommand(roomCode, request);
            var result = await handler.HandleAsync(command, cancellationToken);
            var response = SetPlayerReadyMappings.ToResponse(result);

            return Results.Ok(response);
        }
        catch (RoomSetPlayerReadyRoomNotFoundException exception)
        {
            return RoomProblemResults.NotFound("Room not found.", exception.Message);
        }
        catch (RoomSetPlayerReadyPlayerNotFoundException exception)
        {
            return RoomProblemResults.NotFound("Player not found.", exception.Message);
        }
        catch (RoomSetPlayerReadyConflictException exception)
        {
            return RoomProblemResults.Conflict("Unable to update player readiness.", exception.Message);
        }
        catch (ArgumentException exception)
        {
            return RoomProblemResults.BadRequest("Invalid ready request.", exception.Message);
        }
    }
}