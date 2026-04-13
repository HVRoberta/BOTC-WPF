using BOTC.Application.Features.Rooms.LeaveRoom;
using BOTC.Contracts.Rooms;
using Microsoft.AspNetCore.Mvc;

namespace BOTC.Presentation.Api.Rooms.LeaveRoom;

public static class LeaveRoomEndpoint
{
    public static RouteGroupBuilder MapLeaveRoomEndpoint(this RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapPost("/{roomCode}/leave", HandleAsync)
            .WithName("LeaveRoom")
            .WithTags("Rooms")
            .Produces<LeaveRoomResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);
        
        return group;
    }
    
    private static async Task<IResult> HandleAsync(
        string roomCode,
        LeaveRoomRequest request,
        LeaveRoomHandler handler,
        CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.PlayerId))
        {
            return Results.BadRequest(new ProblemDetails
            {
                Title = "Invalid leave room request.",
                Detail = "PlayerId is required.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        try
        {
            var command = LeaveRoomMappings.ToCommand(roomCode, request);
            var result = await handler.HandleAsync(command, cancellationToken);
            var response = LeaveRoomMappings.ToResponse(result);

            return Results.Ok(response);
        }
        catch (RoomLeaveRoomNotFoundException exception)
        {
            return Results.NotFound(new ProblemDetails
            {
                Title = "Room not found.",
                Detail = exception.Message,
                Status = StatusCodes.Status404NotFound
            });
        }
        catch (RoomLeavePlayerNotFoundException exception)
        {
            return Results.NotFound(new ProblemDetails
            {
                Title = "Player not found.",
                Detail = exception.Message,
                Status = StatusCodes.Status404NotFound
            });
        }
        catch (RoomLeaveConflictException exception)
        {
            return Results.Conflict(new ProblemDetails
            {
                Title = "Unable to leave room.",
                Detail = exception.Message,
                Status = StatusCodes.Status409Conflict
            });
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(new ProblemDetails
            {
                Title = "Invalid leave room request.",
                Detail = exception.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }
}