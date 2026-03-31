using System.Reflection;
using System.Text;
using System.Text.Json;
using BOTC.Application.Features.Rooms.SetPlayerReady;
using BOTC.Contracts.Rooms;
using BOTC.Domain.Rooms;
using BOTC.Presentation.Api.Rooms;
using BOTC.Presentation.Api.Rooms.Realtime;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace BOTC.Presentation.Api.Tests.Rooms;

public sealed class SetPlayerReadyEndpointTests
{
    [Fact]
    public async Task SetPlayerReadyAsync_WhenRequestIsValid_Returns200AndNotifiesLobbyUpdated()
    {
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Host", DateTime.UtcNow);
        var player = room.JoinPlayer("Alice", DateTime.UtcNow.AddSeconds(1));
        var handler = new SetPlayerReadyHandler(new FakeSetPlayerReadyRepository(room));
        var notifier = new FakeRoomLobbyNotifier();

        var result = await InvokeSetPlayerReadyAsync(
            "AB12CD",
            new SetPlayerReadyRequest(player.Id.Value.ToString(), true),
            handler,
            notifier,
            CancellationToken.None);

        var response = await ExecuteResultAsync(result);

        Assert.Equal(StatusCodes.Status200OK, response.StatusCode);
        Assert.Equal(["AB12CD"], notifier.UpdatedRoomCodes);

        using var document = JsonDocument.Parse(response.Body);
        var root = document.RootElement;
        Assert.Equal("AB12CD", root.GetProperty("roomCode").GetString());
        Assert.Equal(player.Id.Value.ToString(), root.GetProperty("playerId").GetString());
        Assert.True(root.GetProperty("isReady").GetBoolean());
    }

    [Fact]
    public async Task SetPlayerReadyAsync_WhenPlayerIdMissing_Returns400AndDoesNotNotify()
    {
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Host", DateTime.UtcNow);
        var handler = new SetPlayerReadyHandler(new FakeSetPlayerReadyRepository(room));
        var notifier = new FakeRoomLobbyNotifier();

        var result = await InvokeSetPlayerReadyAsync(
            "AB12CD",
            new SetPlayerReadyRequest(" ", true),
            handler,
            notifier,
            CancellationToken.None);

        var response = await ExecuteResultAsync(result);

        Assert.Equal(StatusCodes.Status400BadRequest, response.StatusCode);
        Assert.Empty(notifier.UpdatedRoomCodes);
    }

    [Fact]
    public async Task SetPlayerReadyAsync_WhenSaveConflicts_Returns409AndDoesNotNotify()
    {
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Host", DateTime.UtcNow);
        var player = room.JoinPlayer("Alice", DateTime.UtcNow.AddSeconds(1));
        var handler = new SetPlayerReadyHandler(new FakeSetPlayerReadyRepository(room, trySaveResult: false));
        var notifier = new FakeRoomLobbyNotifier();

        var result = await InvokeSetPlayerReadyAsync(
            "AB12CD",
            new SetPlayerReadyRequest(player.Id.Value.ToString(), true),
            handler,
            notifier,
            CancellationToken.None);

        var response = await ExecuteResultAsync(result);

        Assert.Equal(StatusCodes.Status409Conflict, response.StatusCode);
        Assert.Empty(notifier.UpdatedRoomCodes);
    }

    private static async Task<IResult> InvokeSetPlayerReadyAsync(
        string roomCode,
        SetPlayerReadyRequest request,
        SetPlayerReadyHandler handler,
        IRoomLobbyNotifier notifier,
        CancellationToken cancellationToken)
    {
        var method = typeof(RoomsEndpoints).GetMethod(
            "SetPlayerReadyAsync",
            BindingFlags.Static | BindingFlags.NonPublic,
            null,
            [typeof(string), typeof(SetPlayerReadyRequest), typeof(SetPlayerReadyHandler), typeof(IRoomLobbyNotifier), typeof(CancellationToken)],
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

    private sealed class FakeSetPlayerReadyRepository : IRoomSetPlayerReadyRepository
    {
        private readonly Room? room;
        private readonly bool trySaveResult;

        public FakeSetPlayerReadyRepository(Room? room, bool trySaveResult = true)
        {
            this.room = room;
            this.trySaveResult = trySaveResult;
        }

        public Task<Room?> GetByCodeAsync(RoomCode roomCode, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(room);
        }

        public Task<bool> TrySaveAsync(Room room, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(trySaveResult);
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

