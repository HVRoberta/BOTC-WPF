using BOTC.Application.Abstractions.Persistence;
using BOTC.Application.Features.Rooms.StartGame;
using BOTC.Application.Tests.Fakes;
using BOTC.Domain.Rooms;
using BOTC.Domain.Users;

namespace BOTC.Application.Tests.Features.Rooms.StartGame;

public sealed class StartGameHandlerTests
{
    // -------------------------------------------------------------------------
    // Constructor guard clauses
    // -------------------------------------------------------------------------

    [Fact]
    public void Constructor_WhenRepositoryIsNull_ThrowsArgumentNullException()
    {
        Action act = () => _ = new StartGameHandler(null!, new FakeDomainEventDispatcher());

        var exception = Assert.Throws<ArgumentNullException>(act);
        Assert.Equal("roomRepository", exception.ParamName);
    }

    [Fact]
    public void Constructor_WhenDispatcherIsNull_ThrowsArgumentNullException()
    {
        Action act = () => _ = new StartGameHandler(new FakeRoomRepository(null), null!);

        var exception = Assert.Throws<ArgumentNullException>(act);
        Assert.Equal("domainEventDispatcher", exception.ParamName);
    }

    // -------------------------------------------------------------------------
    // HandleAsync guard clauses
    // -------------------------------------------------------------------------

    [Fact]
    public async Task HandleAsync_WhenStarterPlayerIdIsNotAValidGuid_ThrowsArgumentException()
    {
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Test Room", UserId.New(), DateTime.UtcNow);
        var repository = new FakeRoomRepository(room);
        var handler = new StartGameHandler(repository, new FakeDomainEventDispatcher());

        var act = async () => await handler.HandleAsync(
            new StartGameCommand("AB12CD", "not-a-guid"),
            CancellationToken.None);

        await Assert.ThrowsAsync<ArgumentException>(act);
    }

    // -------------------------------------------------------------------------
    // HandleAsync scenarios
    // -------------------------------------------------------------------------

    [Fact]
    public async Task HandleAsync_WhenRoomDoesNotExist_ThrowsRoomStartGameRoomNotFoundException()
    {
        var repository = new FakeRoomRepository(null);
        var handler = new StartGameHandler(repository, new FakeDomainEventDispatcher());

        var act = async () => await handler.HandleAsync(
            new StartGameCommand("AB12CD", PlayerId.New().Value.ToString()),
            CancellationToken.None);

        var exception = await Assert.ThrowsAsync<RoomStartGameRoomNotFoundException>(act);
        Assert.Equal("AB12CD", exception.RoomCode.Value);
    }

    [Fact]
    public async Task HandleAsync_WhenNonHostPlayersAreNotReady_ReturnsBlockedResultWithoutSaving()
    {
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Test Room", UserId.New(), DateTime.UtcNow);
        room.JoinPlayer(UserId.New(), DateTime.UtcNow.AddSeconds(1));
        var repository = new FakeRoomRepository(room);
        var handler = new StartGameHandler(repository, new FakeDomainEventDispatcher());

        var result = await handler.HandleAsync(
            new StartGameCommand("AB12CD", room.HostPlayerId.Value.ToString()),
            CancellationToken.None);

        Assert.False(result.IsStarted);
        Assert.Equal(RoomStartGameBlockedReason.NonHostPlayersNotReady, result.BlockedReason);
        Assert.Equal(0, repository.TrySaveCallCount);
    }

    [Fact]
    public async Task HandleAsync_WhenNotEnoughPlayers_ReturnsBlockedResultWithoutSaving()
    {
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Test Room", UserId.New(), DateTime.UtcNow);
        var repository = new FakeRoomRepository(room);
        var handler = new StartGameHandler(repository, new FakeDomainEventDispatcher());

        var result = await handler.HandleAsync(
            new StartGameCommand("AB12CD", room.HostPlayerId.Value.ToString()),
            CancellationToken.None);

        Assert.False(result.IsStarted);
        Assert.Equal(RoomStartGameBlockedReason.NotEnoughPlayers, result.BlockedReason);
        Assert.Equal(0, repository.TrySaveCallCount);
    }

    [Fact]
    public async Task HandleAsync_WhenAllNonHostPlayersAreReady_StartsGameAndDispatchesEvent()
    {
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Test Room", UserId.New(), DateTime.UtcNow);
        var alice = room.JoinPlayer(UserId.New(), DateTime.UtcNow.AddSeconds(1));
        room.SetPlayerReady(alice.Id, true);
        room.ClearUncommittedEvents();
        var repository = new FakeRoomRepository(room);
        var dispatcher = new FakeDomainEventDispatcher();
        var handler = new StartGameHandler(repository, dispatcher);

        var result = await handler.HandleAsync(
            new StartGameCommand("AB12CD", room.HostPlayerId.Value.ToString()),
            CancellationToken.None);

        Assert.True(result.IsStarted);
        Assert.Null(result.BlockedReason);
        Assert.Equal(RoomStatus.InProgress, result.RoomStatus);
        Assert.Equal(1, repository.TrySaveCallCount);
        Assert.Single(dispatcher.DispatchedEvents);
    }

    [Fact]
    public async Task HandleAsync_WhenSaveReturnsFalse_ThrowsRoomStartGameConflictException()
    {
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Test Room", UserId.New(), DateTime.UtcNow);
        var alice = room.JoinPlayer(UserId.New(), DateTime.UtcNow.AddSeconds(1));
        room.SetPlayerReady(alice.Id, true);
        var repository = new FakeRoomRepository(room, trySaveResult: false);
        var handler = new StartGameHandler(repository, new FakeDomainEventDispatcher());

        var act = async () => await handler.HandleAsync(
            new StartGameCommand("AB12CD", room.HostPlayerId.Value.ToString()),
            CancellationToken.None);

        await Assert.ThrowsAsync<RoomStartGameConflictException>(act);
    }

    [Fact]
    public async Task HandleAsync_WhenSaveReportsRoomMissing_ThrowsRoomStartGameRoomNotFoundException()
    {
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Test Room", UserId.New(), DateTime.UtcNow);
        var alice = room.JoinPlayer(UserId.New(), DateTime.UtcNow.AddSeconds(1));
        room.SetPlayerReady(alice.Id, true);
        var repository = new FakeRoomRepository(room, throwOnSave: new RoomStartGameSaveRoomMissingException(room.Id));
        var handler = new StartGameHandler(repository, new FakeDomainEventDispatcher());

        var act = async () => await handler.HandleAsync(
            new StartGameCommand("AB12CD", room.HostPlayerId.Value.ToString()),
            CancellationToken.None);

        var exception = await Assert.ThrowsAsync<RoomStartGameRoomNotFoundException>(act);
        Assert.Equal("AB12CD", exception.RoomCode.Value);
    }

    [Fact]
    public async Task HandleAsync_WhenCancellationIsRequested_ThrowsOperationCanceledException()
    {
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Test Room", UserId.New(), DateTime.UtcNow);
        var repository = new FakeRoomRepository(room);
        var handler = new StartGameHandler(repository, new FakeDomainEventDispatcher());

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await handler.HandleAsync(
            new StartGameCommand("AB12CD", PlayerId.New().Value.ToString()),
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
            => throw new NotSupportedException("Not used by StartGameHandler.");

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
            => throw new NotSupportedException("Not used by StartGameHandler.");
    }
}
