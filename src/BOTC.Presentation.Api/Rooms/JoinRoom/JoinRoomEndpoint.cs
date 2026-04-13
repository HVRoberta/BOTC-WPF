using BOTC.Application.Features.Rooms.JoinRoom;
using BOTC.Contracts.Rooms;
using BOTC.Presentation.Api.Rooms.Common;

namespace BOTC.Presentation.Api.Rooms.JoinRoom;

public static class JoinRoomEndpoint
{
    public static RouteGroupBuilder MapJoinRoomEndpoint(this RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapPost("/{roomCode}/join", HandleAsync)
            .WithName("JoinRoom")
            .Produces<JoinRoomResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return group;
    }

    private static async Task<IResult> HandleAsync(
        string roomCode,
        JoinRoomRequest? request,
        JoinRoomHandler handler,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return RoomProblemResults.BadRequest(
                "Invalid join room request.",
                "Request body is required.");
        }

        try
        {
            var command = JoinRoomMappings.ToCommand(roomCode, request);
            var result = await handler.HandleAsync(command, cancellationToken);
            var response = JoinRoomMappings.ToResponse(result);

            return Results.Ok(response);
        }
        catch (RoomJoinNotFoundException exception)
        {
            return RoomProblemResults.NotFound(
                "Room not found.",
                exception.Message);
        }
        catch (RoomJoinConflictException exception)
        {
            return RoomProblemResults.Conflict(
                "Unable to join room.",
                exception.Message);
        }
        catch (ArgumentException exception)
        {
            return RoomProblemResults.BadRequest(
                "Invalid join room request.",
                exception.Message);
        }
    }
}