using BOTC.Application.Abstractions.Persistence;
using BOTC.Application.Features.Rooms.SetPlayerReady;
using BOTC.Application.Tests.Fakes;
using BOTC.Domain.Rooms;
using BOTC.Domain.Users;

namespace BOTC.Application.Tests.Features.Rooms.SetPlayerReady;

public sealed class SetPlayerReadyHandlerTests
{
    // -------------------------------------------------------------------------
    // Constructor guard clauses
    // -------------------------------------------------------------------------

    [Fact]
    public void Constructor_WhenRepositoryIsNull_ThrowsArgumentNullException()
    {
        Action act = () => _ = new SetPlayerReadyHandler(null!, new FakeDomainEventDispatcher());

        var exception = Assert.Throws<ArgumentNullException>(act);
        Assert.Equal("roomRepository", exception.ParamName);
    }

    [Fact]
    public void Constructor_WhenDispatcherIsNull_ThrowsArgumentNullException()
    {
        Action act = () => _ = new SetPlayerReadyHandler(new FakeRoomRepository(null), null!);

        var exception = Assert.Throws<ArgumentNullException>(act);
        Assert.Equal("domainEventDispatcher", exception.ParamName);
    }

    // -------------------------------------------------------------------------
    // HandleAsync guard clauses
    // -------------------------------------------------------------------------

    [Fact]
    public async Task HandleAsync_WhenPlayerIdIsNotAValidGuid_ThrowsArgumentException()
    {
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Test Room", UserId.New(), DateTime.UtcNow);
        var repository = new FakeRoomRepository(room);
        var handler = new SetPlayerReadyHandler(repository, new FakeDomainEventDispatcher());

        var act = async () => await handler.HandleAsync(
            new SetPlayerReadyCommand("AB12CD", "not-a-guid", true),
            CancellationToken.None);

        await Assert.ThrowsAsync<ArgumentException>(act);
    }

    // -------------------------------------------------------------------------
    // HandleAsync scenarios
    // -------------------------------------------------------------------------

    [Fact]
    public async Task HandleAsync_WhenRoomDoesNotExist_ThrowsRoomSetPlayerReadyRoomNotFoundException()
    {
        var repository = new FakeRoomRepository(null);
        var handler = new SetPlayerReadyHandler(repository, new FakeDomainEventDispatcher());

        var act = async () => await handler.HandleAsync(
            new SetPlayerReadyCommand("AB12CD", PlayerId.New().Value.ToString(), true),
            CancellationToken.None);

        var exception = await Assert.ThrowsAsync<RoomSetPlayerReadyRoomNotFoundException>(act);
        Assert.Equal("AB12CD", exception.RoomCode.Value);
    }

    [Fact]
    public async Task HandleAsync_WhenPlayerIsNotInRoom_ThrowsRoomSetPlayerReadyPlayerNotFoundException()
    {
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Test Room", UserId.New(), DateTime.UtcNow);
        var repository = new FakeRoomRepository(room);
        var handler = new SetPlayerReadyHandler(repository, new FakeDomainEventDispatcher());
        var missingPlayerId = PlayerId.New();

        var act = async () => await handler.HandleAsync(
            new SetPlayerReadyCommand("AB12CD", missingPlayerId.Value.ToString(), true),
            CancellationToken.None);

        var exception = await Assert.ThrowsAsync<RoomSetPlayerReadyPlayerNotFoundException>(act);
        Assert.Equal(room.Code, exception.RoomCode);
        Assert.Equal(missingPlayerId, exception.PlayerId);
    }

    [Fact]
    public async Task HandleAsync_WhenGameHasAlreadyStarted_ThrowsRoomSetPlayerReadyConflictException()
    {
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Test Room", UserId.New(), DateTime.UtcNow);
        var alice = room.JoinPlayer(UserId.New(), DateTime.UtcNow.AddSeconds(1));
        room.SetPlayerReady(alice.Id, true);
        room.StartGame(room.HostPlayerId);
        room.ClearUncommittedEvents();
        var repository = new FakeRoomRepository(room);
        var handler = new SetPlayerReadyHandler(repository, new FakeDomainEventDispatcher());

        var act = async () => await handler.HandleAsync(
            new SetPlayerReadyCommand("AB12CD", alice.Id.Value.ToString(), false),
            CancellationToken.None);

        await Assert.ThrowsAsync<RoomSetPlayerReadyConflictException>(act);
    }

    [Fact]
    public async Task HandleAsync_WhenPlayerExists_UpdatesReadinessAndDispatchesEvent()
    {
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Test Room", UserId.New(), DateTime.UtcNow);
        var alice = room.JoinPlayer(UserId.New(), DateTime.UtcNow.AddSeconds(1));
        room.ClearUncommittedEvents();
        var repository = new FakeRoomRepository(room);
        var dispatcher = new FakeDomainEventDispatcher();
        var handler = new SetPlayerReadyHandler(repository, dispatcher);

        var result = await handler.HandleAsync(
            new SetPlayerReadyCommand("AB12CD", alice.Id.Value.ToString(), true),
            CancellationToken.None);

        Assert.Equal("AB12CD", result.RoomCode.Value);
        Assert.Equal(alice.Id, result.PlayerId);
        Assert.True(result.IsReady);
        Assert.Equal(1, repository.TrySaveCallCount);
        Assert.Single(dispatcher.DispatchedEvents);
    }

    [Fact]
    public async Task HandleAsync_WhenSaveReturnsFalse_ThrowsRoomSetPlayerReadyConflictException()
    {
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Test Room", UserId.New(), DateTime.UtcNow);
        var alice = room.JoinPlayer(UserId.New(), DateTime.UtcNow.AddSeconds(1));
        var repository = new FakeRoomRepository(room, trySaveResult: false);
        var handler = new SetPlayerReadyHandler(repository, new FakeDomainEventDispatcher());

        var act = async () => await handler.HandleAsync(
            new SetPlayerReadyCommand("AB12CD", alice.Id.Value.ToString(), true),
            CancellationToken.None);

        await Assert.ThrowsAsync<RoomSetPlayerReadyConflictException>(act);
    }

    [Fact]
    public async Task HandleAsync_WhenSaveReportsRoomMissing_ThrowsRoomSetPlayerReadyRoomNotFoundException()
    {
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Test Room", UserId.New(), DateTime.UtcNow);
        var alice = room.JoinPlayer(UserId.New(), DateTime.UtcNow.AddSeconds(1));
        var repository = new FakeRoomRepository(room, throwOnSave: new RoomSetPlayerReadySaveRoomMissingException(room.Id));
        var handler = new SetPlayerReadyHandler(repository, new FakeDomainEventDispatcher());

        var act = async () => await handler.HandleAsync(
            new SetPlayerReadyCommand("AB12CD", alice.Id.Value.ToString(), true),
            CancellationToken.None);

        var exception = await Assert.ThrowsAsync<RoomSetPlayerReadyRoomNotFoundException>(act);
        Assert.Equal("AB12CD", exception.RoomCode.Value);
    }

    [Fact]
    public async Task HandleAsync_WhenCancellationIsRequested_ThrowsOperationCanceledException()
    {
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Test Room", UserId.New(), DateTime.UtcNow);
        var repository = new FakeRoomRepository(room);
        var handler = new SetPlayerReadyHandler(repository, new FakeDomainEventDispatcher());

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await handler.HandleAsync(
            new SetPlayerReadyCommand("AB12CD", PlayerId.New().Value.ToString(), true),
            cts.Token);

        await Assert.ThrowsAsync<OperationCanceledException>(act);
    }

    // -------------------------------------------------------------------------
    // Fake
    // -------------------------------------------------------------------------

    private sealed class FakeRoomRepository : IRoomRepository
    {
        private readonly Room? _room;
        private readonly bool _trySaveResult;
        private readonly Exception? _throwOnSave;

        public FakeRoomRepository(Room? room, bool trySaveResult = true, Exception? throwOnSave = null)
        {
            _room = room;
            _trySaveResult = trySaveResult;
            _throwOnSave = throwOnSave;
        }

        public int TrySaveCallCount { get; private set; }

        public Task<bool> TryAddAsync(Room room, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not used by SetPlayerReadyHandler.");

        public Task<Room?> GetByCodeAsync(RoomCode roomCode, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_room);
        }

        public Task<bool> TrySaveAsync(Room room, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            TrySaveCallCount++;

            if (_throwOnSave is not null)
            {
                throw _throwOnSave;
            }

            return Task.FromResult(_trySaveResult);
        }

        public Task<bool> TryDeleteAsync(RoomId roomId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not used by SetPlayerReadyHandler.");
    }
}
