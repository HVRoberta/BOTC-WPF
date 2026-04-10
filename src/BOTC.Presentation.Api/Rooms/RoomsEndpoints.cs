using BOTC.Application.Features.Rooms.CreateRoom;
using BOTC.Application.Features.Rooms.GetRoomLobby;
using BOTC.Application.Features.Rooms.JoinRoom;
using BOTC.Application.Features.Rooms.LeaveRoom;
using BOTC.Application.Features.Rooms.SetPlayerReady;
using BOTC.Application.Features.Rooms.StartGame;
using BOTC.Contracts.Rooms;
using BOTC.Presentation.Api.Rooms.CreateRoom;
using BOTC.Presentation.Api.Rooms.GetRoomLobby;
using BOTC.Presentation.Api.Rooms.JoinRoom;
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

        endpoints.MapPost("/api/rooms/{roomCode}/leave", LeaveRoomAsync)
            .WithName("LeaveRoom")
            .WithTags("Rooms")
            .Produces<LeaveRoomResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        endpoints.MapGet("/api/rooms/{roomCode}/lobby", GetRoomLobbyAsync)
            .WithName("GetRoomLobby")
            .WithTags("Rooms")
            .Produces<GetRoomLobbyResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        endpoints.MapPost("/api/rooms/{roomCode}/ready", SetPlayerReadyAsync)
            .WithName("SetPlayerReady")
            .WithTags("Rooms")
            .Produces<SetPlayerReadyResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        endpoints.MapPost("/api/rooms/{roomCode}/start", StartGameAsync)
            .WithName("StartGame")
            .WithTags("Rooms")
            .Produces<StartGameResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return endpoints;
    }

    private static async Task<IResult> CreateRoomAsync(
        CreateRoomRequest request,
        CreateRoomHandler handler,
        CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.HostName))
        {
            return Results.BadRequest(new ProblemDetails
            {
                Title = "Invalid create room request.",
                Detail = "HostName is required.",
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
        JoinRoomRequest? request,
        JoinRoomHandler handler,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.BadRequest(new ProblemDetails
            {
                Title = "Invalid join room request.",
                Detail = "Name is required.",
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

    private static async Task<IResult> LeaveRoomAsync(
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

    private static async Task<IResult> SetPlayerReadyAsync(
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

    private static async Task<IResult> StartGameAsync(
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
