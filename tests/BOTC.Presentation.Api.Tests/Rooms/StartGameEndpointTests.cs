using System.Reflection;
using System.Text;
using System.Text.Json;
using BOTC.Application.Features.Rooms.StartGame;
using BOTC.Contracts.Rooms;
using BOTC.Domain.Rooms;
using BOTC.Presentation.Api.Rooms;
using BOTC.Presentation.Api.Rooms.Realtime;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace BOTC.Presentation.Api.Tests.Rooms;

public sealed class StartGameEndpointTests
{
    [Fact]
    public async Task StartGameAsync_WhenStartSucceeds_Returns200AndNotifiesLobbyUpdated()
    {
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Host", DateTime.UtcNow);
        var player = room.JoinPlayer("Alice", DateTime.UtcNow.AddSeconds(1));
        room.SetPlayerReady(player.Id, true);
        var handler = new StartGameHandler(new FakeStartGameRepository(room));
        var notifier = new FakeRoomLobbyNotifier();

        var result = await InvokeStartGameAsync(
            "AB12CD",
            new StartGameRequest(room.HostPlayerId.Value.ToString()),
            handler,
            notifier,
            CancellationToken.None);

        var response = await ExecuteResultAsync(result);

        Assert.Equal(StatusCodes.Status200OK, response.StatusCode);
        Assert.Equal(["AB12CD"], notifier.UpdatedRoomCodes);

        using var document = JsonDocument.Parse(response.Body);
        var root = document.RootElement;
        Assert.True(root.GetProperty("isStarted").GetBoolean());
        Assert.Equal((int)RoomStatus.InProgress, root.GetProperty("roomStatus").GetInt32());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("blockedReason").ValueKind);
    }

    [Fact]
    public async Task StartGameAsync_WhenStartIsBlocked_Returns200WithoutLobbyUpdate()
    {
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Host", DateTime.UtcNow);
        room.JoinPlayer("Alice", DateTime.UtcNow.AddSeconds(1));
        var handler = new StartGameHandler(new FakeStartGameRepository(room));
        var notifier = new FakeRoomLobbyNotifier();

        var result = await InvokeStartGameAsync(
            "AB12CD",
            new StartGameRequest(room.HostPlayerId.Value.ToString()),
            handler,
            notifier,
            CancellationToken.None);

        var response = await ExecuteResultAsync(result);

        Assert.Equal(StatusCodes.Status200OK, response.StatusCode);
        Assert.Empty(notifier.UpdatedRoomCodes);

        using var document = JsonDocument.Parse(response.Body);
        var root = document.RootElement;
        Assert.False(root.GetProperty("isStarted").GetBoolean());
        Assert.Equal((int)StartGameBlockedReasonContract.NonHostPlayersNotReady, root.GetProperty("blockedReason").GetInt32());
        Assert.Equal((int)RoomStatus.WaitingForPlayers, root.GetProperty("roomStatus").GetInt32());
    }

    [Fact]
    public async Task StartGameAsync_WhenStarterPlayerIdMissing_Returns400AndDoesNotNotify()
    {
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Host", DateTime.UtcNow);
        var handler = new StartGameHandler(new FakeStartGameRepository(room));
        var notifier = new FakeRoomLobbyNotifier();

        var result = await InvokeStartGameAsync(
            "AB12CD",
            new StartGameRequest("  "),
            handler,
            notifier,
            CancellationToken.None);

        var response = await ExecuteResultAsync(result);

        Assert.Equal(StatusCodes.Status400BadRequest, response.StatusCode);
        Assert.Empty(notifier.UpdatedRoomCodes);
    }

    private static async Task<IResult> InvokeStartGameAsync(
        string roomCode,
        StartGameRequest request,
        StartGameHandler handler,
        IRoomLobbyNotifier notifier,
        CancellationToken cancellationToken)
    {
        var method = typeof(RoomsEndpoints).GetMethod(
            "StartGameAsync",
            BindingFlags.Static | BindingFlags.NonPublic,
            null,
            [typeof(string), typeof(StartGameRequest), typeof(StartGameHandler), typeof(IRoomLobbyNotifier), typeof(CancellationToken)],
            null);

        Assert.NotNull(method);

        var invocationResult = method!.Invoke(null, [roomCode, request, handler, notifier, cancellationToken]);
        Assert.NotNull(invocationResult);

        return await (Task<IResult>)invocationResult!;
    }

    private static async Task<HttpExecutionResult> ExecuteResultAsync(IResult result)
    {
        var httpContext = new DefaultHttpContext();
        var serviceProvider = new ServiceCollection()
            .AddLogging()
            .AddProblemDetails()
            .BuildServiceProvider();

        httpContext.RequestServices = serviceProvider;

        await using var bodyStream = new MemoryStream();
        httpContext.Response.Body = bodyStream;

        await result.ExecuteAsync(httpContext);

        bodyStream.Position = 0;
        using var reader = new StreamReader(bodyStream, Encoding.UTF8);
        var body = await reader.ReadToEndAsync();

        await serviceProvider.DisposeAsync();

        return new HttpExecutionResult(httpContext.Response.StatusCode, body);
    }

    private sealed record HttpExecutionResult(int StatusCode, string Body);

    private sealed class FakeStartGameRepository : IRoomStartGameRepository
    {
        private readonly Room? room;

        public FakeStartGameRepository(Room? room)
        {
            this.room = room;
        }

        public Task<Room?> GetByCodeAsync(RoomCode roomCode, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(room);
        }

        public Task<bool> TrySaveAsync(Room room, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(true);
        }
    }

    private sealed class FakeRoomLobbyNotifier : IRoomLobbyNotifier
    {
        public List<string> UpdatedRoomCodes { get; } = [];

        public Task NotifyLobbyUpdatedAsync(string roomCode, CancellationToken cancellationToken)
        {
            UpdatedRoomCodes.Add(roomCode);
            return Task.CompletedTask;
        }

        public Task NotifyLobbyClosedAsync(string roomCode, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }
}

