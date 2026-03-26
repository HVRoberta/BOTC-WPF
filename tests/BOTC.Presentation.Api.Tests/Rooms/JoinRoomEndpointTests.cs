using System.Reflection;
using System.Text;
using System.Text.Json;
using BOTC.Application.Features.Rooms.JoinRoom;
using BOTC.Contracts.Rooms;
using BOTC.Domain.Rooms;
using BOTC.Presentation.Api.Rooms;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace BOTC.Presentation.Api.Tests.Rooms;

public sealed class JoinRoomEndpointTests
{
    [Fact]
    public async Task JoinRoomAsync_WhenRequestIsValid_Returns200WithResponsePayload()
    {
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Host", DateTime.UtcNow);
        var repository = new FakeRoomJoinRepository(room);
        var handler = new JoinRoomHandler(repository);
        var notifier = new FakeRoomLobbyNotifier();

        var result = await InvokeJoinRoomAsync("AB12CD", new JoinRoomRequest("Alice"), handler, notifier, CancellationToken.None);
        var response = await ExecuteResultAsync(result);

        Assert.Equal(StatusCodes.Status200OK, response.StatusCode);
        Assert.Equal(["AB12CD"], notifier.NotifiedRoomCodes);

        using var document = JsonDocument.Parse(response.Body);
        var root = document.RootElement;
        Assert.Equal("AB12CD", root.GetProperty("roomCode").GetString());
        Assert.Equal("Alice", root.GetProperty("displayName").GetString());
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("playerId").GetString()));
    }

    [Fact]
    public async Task JoinRoomAsync_WhenDisplayNameMissing_Returns400()
    {
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Host", DateTime.UtcNow);
        var handler = new JoinRoomHandler(new FakeRoomJoinRepository(room));
        var notifier = new FakeRoomLobbyNotifier();

        var result = await InvokeJoinRoomAsync("AB12CD", new JoinRoomRequest("  "), handler, notifier, CancellationToken.None);
        var response = await ExecuteResultAsync(result);

        Assert.Equal(StatusCodes.Status400BadRequest, response.StatusCode);
        Assert.Empty(notifier.NotifiedRoomCodes);
    }

    [Fact]
    public async Task JoinRoomAsync_WhenRoomNotFound_Returns404()
    {
        var handler = new JoinRoomHandler(new FakeRoomJoinRepository());
        var notifier = new FakeRoomLobbyNotifier();

        var result = await InvokeJoinRoomAsync("AB12CD", new JoinRoomRequest("Alice"), handler, notifier, CancellationToken.None);
        var response = await ExecuteResultAsync(result);

        Assert.Equal(StatusCodes.Status404NotFound, response.StatusCode);
        Assert.Empty(notifier.NotifiedRoomCodes);
    }

    [Fact]
    public async Task JoinRoomAsync_WhenConflictOccurs_Returns409()
    {
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Host", DateTime.UtcNow);
        var handler = new JoinRoomHandler(new FakeRoomJoinRepository(room, trySaveResult: false));
        var notifier = new FakeRoomLobbyNotifier();

        var result = await InvokeJoinRoomAsync("AB12CD", new JoinRoomRequest("Alice"), handler, notifier, CancellationToken.None);
        var response = await ExecuteResultAsync(result);

        Assert.Equal(StatusCodes.Status409Conflict, response.StatusCode);
        Assert.Empty(notifier.NotifiedRoomCodes);
    }

    [Fact]
    public async Task JoinRoomAsync_WhenRoomDisappearsDuringSave_Returns404()
    {
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Host", DateTime.UtcNow);
        var exceptionToThrow = new RoomJoinSaveRoomMissingException(room.Id);
        var handler = new JoinRoomHandler(new FakeRoomJoinRepository(room, throwOnSave: exceptionToThrow));
        var notifier = new FakeRoomLobbyNotifier();

        var result = await InvokeJoinRoomAsync("AB12CD", new JoinRoomRequest("Alice"), handler, notifier, CancellationToken.None);
        var response = await ExecuteResultAsync(result);

        Assert.Equal(StatusCodes.Status404NotFound, response.StatusCode);
        Assert.Empty(notifier.NotifiedRoomCodes);
    }

    private static async Task<IResult> InvokeJoinRoomAsync(
        string roomCode,
        JoinRoomRequest request,
        JoinRoomHandler handler,
        IRoomLobbyNotifier notifier,
        CancellationToken cancellationToken)
    {
        var method = typeof(RoomsEndpoints).GetMethod(
            "JoinRoomAsync",
            BindingFlags.Static | BindingFlags.NonPublic,
            null,
            [typeof(string), typeof(JoinRoomRequest), typeof(JoinRoomHandler), typeof(IRoomLobbyNotifier), typeof(CancellationToken)],
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

    private sealed class FakeRoomJoinRepository : IRoomJoinRepository
    {
        private readonly Room? _room;
        private readonly bool _trySaveResult;
        private readonly Exception? _throwOnSave;

        public FakeRoomJoinRepository(Room? room = null, bool trySaveResult = true, Exception? throwOnSave = null)
        {
            _room = room;
            _trySaveResult = trySaveResult;
            _throwOnSave = throwOnSave;
        }

        public Task<Room?> GetByCodeAsync(RoomCode roomCode, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_room);
        }

        public Task<bool> TrySaveAsync(Room roomToSave, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_throwOnSave is not null)
            {
                throw _throwOnSave;
            }

            return Task.FromResult(_trySaveResult);
        }
    }

    private sealed class FakeRoomLobbyNotifier : IRoomLobbyNotifier
    {
        public List<string> NotifiedRoomCodes { get; } = [];

        public Task NotifyLobbyUpdatedAsync(string roomCode, CancellationToken cancellationToken)
        {
            NotifiedRoomCodes.Add(roomCode);
            return Task.CompletedTask;
        }
    }
}
