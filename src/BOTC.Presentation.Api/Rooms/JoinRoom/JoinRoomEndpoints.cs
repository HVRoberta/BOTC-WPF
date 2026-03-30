using BOTC.Application.Features.Rooms.JoinRoom;
using BOTC.Contracts.Rooms;
using BOTC.Presentation.Api.Rooms.Realtime;

namespace BOTC.Presentation.Api.Rooms;

public static class JoinRoomEndpoints
{
    public static IEndpointRouteBuilder MapJoinRoomEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/rooms/{roomCode}/join", JoinRoomAsync)
            .WithName("JoinRoom")
            .WithTags("Rooms")
            .Produces<JoinRoomResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return endpoints;
    }

    private static async Task<IResult> JoinRoomAsync(
        string roomCode,
        JoinRoomRequest request,
        JoinRoomHandler handler,
        IRoomLobbyNotifier roomLobbyNotifier,
        CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.DisplayName))
        {
            return RoomProblemResults.BadRequest("Invalid join room request.", "DisplayName is required.");
        }

        try
        {
            var command = JoinRoomMappings.ToCommand(roomCode, request);
            var result = await handler.HandleAsync(command, cancellationToken);
            var response = JoinRoomMappings.ToResponse(result);

            await roomLobbyNotifier.NotifyLobbyUpdatedAsync(response.RoomCode, CancellationToken.None);

            return Results.Ok(response);
        }
        catch (RoomJoinNotFoundException exception)
        {
            return RoomProblemResults.NotFound("Room not found.", exception.Message);
        }
        catch (RoomJoinConflictException exception)
        {
            return RoomProblemResults.Conflict("Unable to join room.", exception.Message);
        }
        catch (ArgumentException exception)
        {
            return RoomProblemResults.BadRequest("Invalid join room request.", exception.Message);
        }
    }
}
