using BOTC.Application.Features.Rooms.CreateRoom;
using BOTC.Application.Features.Rooms.GetRoomLobby;
using BOTC.Application.Features.Rooms.JoinRoom;
using BOTC.Application.Features.Rooms.LeaveRoom;
using BOTC.Application.Features.Rooms.SetPlayerReady;
using BOTC.Application.Features.Rooms.StartGame;
using BOTC.Contracts.Rooms;
using BOTC.Domain.Rooms;
using BOTC.Presentation.Api.Rooms;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace BOTC.Presentation.Api.Tests.Rooms;

public sealed class JoinRoomEndpointTests
{
    [Fact]
    public async Task JoinRoom_WhenDisplayNameIsNullOrEmpty_ReturnsBadRequest()
    {
        // Arrange
        var request = new JoinRoomRequest { DisplayName = null };
        var roomCode = "AB12CD";
        var handler = new FakeJoinRoomHandler();
        var notifier = new FakeRoomLobbyNotifier();

        // Act
        var result = await CallJoinRoomEndpoint(roomCode, request, handler, notifier);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
        
        var problemDetails = Assert.IsType<ProblemDetails>(badRequestResult.Value);
        Assert.Contains("DisplayName", problemDetails.Detail ?? "");
    }

    [Fact]
    public async Task JoinRoom_WhenRequestBodyIsNull_ReturnsBadRequest()
    {
        // Arrange
        var roomCode = "AB12CD";
        var handler = new FakeJoinRoomHandler();
        var notifier = new FakeRoomLobbyNotifier();

        // Act
        var result = await CallJoinRoomEndpoint(roomCode, null, handler, notifier);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task JoinRoom_WhenHandlerSucceeds_ReturnsOkWithResponse()
    {
        // Arrange
        const string displayName = "TestPlayer";
        const string roomCode = "AB12CD";
        var playerId = RoomMemberId.New();
        
        var request = new JoinRoomRequest { DisplayName = displayName };
        var result = new JoinRoomResult(
            new RoomId(Guid.NewGuid()),
            new RoomCode(roomCode),
            playerId,
            "Host",
            displayName,
            RoomPlayerRole.Player,
            DateTime.UtcNow);

        var handler = new FakeJoinRoomHandler(result);
        var notifier = new FakeRoomLobbyNotifier();

        // Act
        var httpResult = await CallJoinRoomEndpoint(roomCode, request, handler, notifier);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(httpResult);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        
        var response = Assert.IsType<JoinRoomResponse>(okResult.Value);
        Assert.Equal(displayName, response.DisplayName);
        Assert.Equal(roomCode, response.RoomCode);
        Assert.Equal(playerId.Value, Guid.Parse(response.PlayerId));
    }

    [Fact]
    public async Task JoinRoom_WhenRoomNotFound_ReturnsNotFound()
    {
        // Arrange
        var request = new JoinRoomRequest { DisplayName = "TestPlayer" };
        var roomCode = "NOTFOUND";
        
        var exception = new RoomJoinNotFoundException(new RoomCode(roomCode));
        var handler = new FakeJoinRoomHandler(exception);
        var notifier = new FakeRoomLobbyNotifier();

        // Act
        var result = await CallJoinRoomEndpoint(roomCode, request, handler, notifier);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
        
        var problemDetails = Assert.IsType<ProblemDetails>(notFoundResult.Value);
        Assert.Equal("Room not found.", problemDetails.Title);
    }

    [Fact]
    public async Task JoinRoom_WhenDisplayNameAlreadyInUse_ReturnsConflict()
    {
        // Arrange
        var request = new JoinRoomRequest { DisplayName = "TestPlayer" };
        var roomCode = "AB12CD";
        
        var exception = new RoomJoinConflictException("Display name already in use.");
        var handler = new FakeJoinRoomHandler(exception);
        var notifier = new FakeRoomLobbyNotifier();

        // Act
        var result = await CallJoinRoomEndpoint(roomCode, request, handler, notifier);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, conflictResult.StatusCode);
        
        var problemDetails = Assert.IsType<ProblemDetails>(conflictResult.Value);
        Assert.Equal("Unable to join room.", problemDetails.Title);
    }

    [Fact]
    public async Task JoinRoom_WhenRoomCapacityReached_ReturnsConflict()
    {
        // Arrange
        var request = new JoinRoomRequest { DisplayName = "ExtraPlayer" };
        var roomCode = "AB12CD";
        
        var exception = new RoomJoinConflictException("Room is full.");
        var handler = new FakeJoinRoomHandler(exception);
        var notifier = new FakeRoomLobbyNotifier();

        // Act
        var result = await CallJoinRoomEndpoint(roomCode, request, handler, notifier);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, conflictResult.StatusCode);
    }

    [Fact]
    public async Task JoinRoom_WhenInvalidRoomCode_ReturnsBadRequest()
    {
        // Arrange
        var request = new JoinRoomRequest { DisplayName = "TestPlayer" };
        var roomCode = "INVALID";
        
        var exception = new ArgumentException("Invalid room code format.");
        var handler = new FakeJoinRoomHandler(exception);
        var notifier = new FakeRoomLobbyNotifier();

        // Act
        var result = await CallJoinRoomEndpoint(roomCode, request, handler, notifier);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task JoinRoom_WhenSuccessful_NotifiesLobbyUpdate()
    {
        // Arrange
        const string displayName = "TestPlayer";
        const string roomCode = "AB12CD";
        var playerId = RoomMemberId.New();
        
        var request = new JoinRoomRequest { DisplayName = displayName };
        var handlerResult = new JoinRoomResult(
            new RoomId(Guid.NewGuid()),
            new RoomCode(roomCode),
            playerId,
            "Host",
            displayName,
            RoomPlayerRole.Player,
            DateTime.UtcNow);

        var handler = new FakeJoinRoomHandler(handlerResult);
        var notifier = new FakeRoomLobbyNotifier();

        // Act
        await CallJoinRoomEndpoint(roomCode, request, handler, notifier);

        // Assert
        Assert.True(notifier.LobbyUpdatedNotified, "Lobby update notification should be called");
        Assert.Equal(roomCode, notifier.NotifiedRoomCode);
    }

    private static async Task<IResult> CallJoinRoomEndpoint(
        string roomCode,
        JoinRoomRequest? request,
        FakeJoinRoomHandler handler,
        FakeRoomLobbyNotifier notifier)
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
            var command = new JoinRoomCommand(roomCode, request.DisplayName);
            var result = await handler.HandleAsync(command, CancellationToken.None);
            var response = new JoinRoomResponse
            {
                RoomCode = result.RoomCode.Value,
                PlayerId = result.PlayerId.Value.ToString(),
                DisplayName = result.DisplayName,
                HostDisplayName = result.HostDisplayName,
                Players = []
            };

            await notifier.NotifyLobbyUpdatedAsync(response.RoomCode, CancellationToken.None);

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

    private sealed class FakeJoinRoomHandler
    {
        private readonly JoinRoomResult? result;
        private readonly Exception? exception;

        public FakeJoinRoomHandler()
        {
            this.result = null;
            this.exception = null;
        }

        public FakeJoinRoomHandler(JoinRoomResult result)
        {
            this.result = result;
            this.exception = null;
        }

        public FakeJoinRoomHandler(Exception exception)
        {
            this.result = null;
            this.exception = exception;
        }

        public Task<JoinRoomResult> HandleAsync(JoinRoomCommand command, CancellationToken cancellationToken)
        {
            if (exception is not null)
                throw exception;

            return Task.FromResult(result ?? throw new InvalidOperationException("No result configured"));
        }
    }

    private sealed class FakeRoomLobbyNotifier
    {
        public bool LobbyUpdatedNotified { get; private set; }
        public string? NotifiedRoomCode { get; private set; }
        public bool LobbyClosedNotified { get; private set; }

        public Task NotifyLobbyUpdatedAsync(string roomCode, CancellationToken cancellationToken)
        {
            LobbyUpdatedNotified = true;
            NotifiedRoomCode = roomCode;
            return Task.CompletedTask;
        }

        public Task NotifyLobbyClosedAsync(string roomCode, CancellationToken cancellationToken)
        {
            LobbyClosedNotified = true;
            NotifiedRoomCode = roomCode;
            return Task.CompletedTask;
        }
    }
}

