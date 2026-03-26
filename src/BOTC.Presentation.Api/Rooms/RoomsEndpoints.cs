using BOTC.Application.Features.Rooms.CreateRoom;
using BOTC.Application.Features.Rooms.GetRoomLobby;
using BOTC.Application.Features.Rooms.JoinRoom;
using BOTC.Contracts.Rooms;
using Microsoft.AspNetCore.Mvc;

namespace BOTC.Presentation.Api.Rooms;

public static class RoomsEndpoints
{
    public static IEndpointRouteBuilder MapRoomsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/rooms", CreateRoomAsync)
            .WithName("CreateRoom")
            .WithTags("Rooms")
            .Produces<CreateRoomResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        endpoints.MapPost("/api/rooms/{roomCode}/join", JoinRoomAsync)
            .WithName("JoinRoom")
            .WithTags("Rooms")
            .Produces<JoinRoomResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        endpoints.MapGet("/api/rooms/{roomCode}/lobby", GetRoomLobbyAsync)
            .WithName("GetRoomLobby")
            .WithTags("Rooms")
            .Produces<GetRoomLobbyResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static async Task<IResult> CreateRoomAsync(
        CreateRoomRequest request,
        CreateRoomHandler handler,
        CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.HostDisplayName))
        {
            return Results.BadRequest(new ProblemDetails
            {
                Title = "Invalid create room request.",
                Detail = "HostDisplayName is required.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        try
        {
            var command = CreateRoomMappings.ToCommand(request);
            var result = await handler.HandleAsync(command, cancellationToken);
            var response = CreateRoomMappings.ToResponse(result);

            return Results.Created($"/api/rooms/{response.RoomId}", response);
        }
        catch (RoomCodeGenerationExhaustedException exception)
        {
            return Results.Problem(
                title: "Room code generation temporarily unavailable.",
                detail: exception.Message,
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(new ProblemDetails
            {
                Title = "Invalid create room request.",
                Detail = exception.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
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

    private static async Task<IResult> JoinRoomAsync(
        string roomCode,
        JoinRoomRequest request,
        JoinRoomHandler handler,
        CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.DisplayName))
        {
            return Results.BadRequest(new ProblemDetails
            {
                Title = "Invalid join room request.",
                Detail = "DisplayName is required.",
                Status = StatusCodes.Status400BadRequest
            });
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
            return Results.NotFound(new ProblemDetails
            {
                Title = "Room not found.",
                Detail = exception.Message,
                Status = StatusCodes.Status404NotFound
            });
        }
        catch (RoomJoinConflictException exception)
        {
            return Results.Conflict(new ProblemDetails
            {
                Title = "Unable to join room.",
                Detail = exception.Message,
                Status = StatusCodes.Status409Conflict
            });
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(new ProblemDetails
            {
                Title = "Invalid join room request.",
                Detail = exception.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }
}
