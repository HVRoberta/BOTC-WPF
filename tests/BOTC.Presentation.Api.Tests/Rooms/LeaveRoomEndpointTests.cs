using System.Reflection;
using System.Text;
using System.Text.Json;
using BOTC.Application.Features.Rooms.LeaveRoom;
using BOTC.Contracts.Rooms;
using BOTC.Domain.Rooms;
using BOTC.Presentation.Api.Rooms;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace BOTC.Presentation.Api.Tests.Rooms;

public sealed class LeaveRoomEndpointTests
{
    [Fact]
    public async Task LeaveRoomAsync_WhenHostLeavesAndPlayersRemain_Returns200AndNotifiesLobbyUpdated()
    {
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Host", DateTime.UtcNow);
        var successor = room.JoinPlayer("Alice", DateTime.UtcNow.AddSeconds(1));
        room.JoinPlayer("Bob", DateTime.UtcNow.AddSeconds(2));
        var handler = new LeaveRoomHandler(new FakeRoomLeaveRepository(room));
        var notifier = new FakeRoomLobbyNotifier();

        var result = await InvokeLeaveRoomAsync(
            "AB12CD",
            new LeaveRoomRequest(room.HostPlayerId.Value.ToString()),
            handler,
            notifier,
            CancellationToken.None);

        var response = await ExecuteResultAsync(result);

        Assert.Equal(StatusCodes.Status200OK, response.StatusCode);
        Assert.Equal(["AB12CD"], notifier.UpdatedRoomCodes);
        Assert.Empty(notifier.ClosedRoomCodes);

        using var document = JsonDocument.Parse(response.Body);
        var root = document.RootElement;
        Assert.Equal("AB12CD", root.GetProperty("roomCode").GetString());
        Assert.False(root.GetProperty("roomWasRemoved").GetBoolean());
        Assert.Equal(successor.Id.Value.ToString(), root.GetProperty("newHostPlayerId").GetString());
    }

    [Fact]
    public async Task LeaveRoomAsync_WhenLastPlayerLeaves_Returns200AndNotifiesLobbyClosed()
    {
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Host", DateTime.UtcNow);
        var handler = new LeaveRoomHandler(new FakeRoomLeaveRepository(room));
        var notifier = new FakeRoomLobbyNotifier();

        var result = await InvokeLeaveRoomAsync(
            "AB12CD",
            new LeaveRoomRequest(room.HostPlayerId.Value.ToString()),
            handler,
            notifier,
            CancellationToken.None);

        var response = await ExecuteResultAsync(result);

        Assert.Equal(StatusCodes.Status200OK, response.StatusCode);
        Assert.Empty(notifier.UpdatedRoomCodes);
        Assert.Equal(["AB12CD"], notifier.ClosedRoomCodes);

        using var document = JsonDocument.Parse(response.Body);
        var root = document.RootElement;
        Assert.True(root.GetProperty("roomWasRemoved").GetBoolean());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("newHostPlayerId").ValueKind);
    }

    [Fact]
    public async Task LeaveRoomAsync_WhenPlayerIdMissing_Returns400()
    {
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Host", DateTime.UtcNow);
        var handler = new LeaveRoomHandler(new FakeRoomLeaveRepository(room));
        var notifier = new FakeRoomLobbyNotifier();

        var result = await InvokeLeaveRoomAsync("AB12CD", new LeaveRoomRequest(" "), handler, notifier, CancellationToken.None);
        var response = await ExecuteResultAsync(result);

        Assert.Equal(StatusCodes.Status400BadRequest, response.StatusCode);
        Assert.Empty(notifier.UpdatedRoomCodes);
        Assert.Empty(notifier.ClosedRoomCodes);
    }

    private static async Task<IResult> InvokeLeaveRoomAsync(
        string roomCode,
        LeaveRoomRequest request,
        LeaveRoomHandler handler,
        IRoomLobbyNotifier notifier,
        CancellationToken cancellationToken)
    {
        var method = typeof(RoomsEndpoints).GetMethod(
            "LeaveRoomAsync",
            BindingFlags.Static | BindingFlags.NonPublic,
            null,
            [typeof(string), typeof(LeaveRoomRequest), typeof(LeaveRoomHandler), typeof(IRoomLobbyNotifier), typeof(CancellationToken)],
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

    private sealed class FakeRoomLeaveRepository : IRoomLeaveRepository
    {
        private readonly Room? _room;

        public FakeRoomLeaveRepository(Room? room)
        {
            _room = room;
        }

        public Task<Room?> GetByCodeAsync(RoomCode roomCode, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_room);
        }

        public Task<bool> TrySaveAsync(Room room, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(true);
        }

        public Task<bool> TryDeleteAsync(RoomId roomId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(true);
        }
    }

    private sealed class FakeRoomLobbyNotifier : IRoomLobbyNotifier
    {
        public List<string> UpdatedRoomCodes { get; } = [];

        public List<string> ClosedRoomCodes { get; } = [];

        public Task NotifyLobbyUpdatedAsync(string roomCode, CancellationToken cancellationToken)
        {
            UpdatedRoomCodes.Add(roomCode);
            return Task.CompletedTask;
        }

        public Task NotifyLobbyClosedAsync(string roomCode, CancellationToken cancellationToken)
        {
            ClosedRoomCodes.Add(roomCode);
            return Task.CompletedTask;
        }
    }
}
