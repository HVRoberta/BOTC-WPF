using BOTC.Application.Abstractions.Persistence;
using BOTC.Application.Features.Rooms.JoinRoom;
using BOTC.Application.Tests.Fakes;
using BOTC.Domain.Rooms;
using BOTC.Domain.Users;

namespace BOTC.Application.Tests.Features.Rooms.JoinRoom;

public sealed class JoinRoomHandlerTests
{
    // -------------------------------------------------------------------------
    // Constructor guard clauses
    // -------------------------------------------------------------------------

    [Fact]
    public void Constructor_WhenRepositoryIsNull_ThrowsArgumentNullException()
    {
        Action act = () => _ = new JoinRoomHandler(null!, new FakeDomainEventDispatcher());

        var exception = Assert.Throws<ArgumentNullException>(act);
        Assert.Equal("roomRepository", exception.ParamName);
    }

    [Fact]
    public void Constructor_WhenDispatcherIsNull_ThrowsArgumentNullException()
    {
        Action act = () => _ = new JoinRoomHandler(new FakeRoomRepository(), null!);

        var exception = Assert.Throws<ArgumentNullException>(act);
        Assert.Equal("domainEventDispatcher", exception.ParamName);
    }

    // -------------------------------------------------------------------------
    // HandleAsync guard clauses
    // -------------------------------------------------------------------------

    [Fact]
    public async Task HandleAsync_WhenCommandIsNull_ThrowsArgumentNullException()
    {
        var handler = new JoinRoomHandler(new FakeRoomRepository(), new FakeDomainEventDispatcher());

        var act = async () => await handler.HandleAsync(null!, CancellationToken.None);

        var exception = await Assert.ThrowsAsync<ArgumentNullException>(act);
        Assert.Equal("command", exception.ParamName);
    }

    [Fact]
    public async Task HandleAsync_WhenRoomCodeIsInvalid_ThrowsArgumentExceptionBeforeRepositoryCall()
    {
        var repository = new FakeRoomRepository();
        var handler = new JoinRoomHandler(repository, new FakeDomainEventDispatcher());

        var act = async () => await handler.HandleAsync(
            new JoinRoomCommand("abc", UserId.New(), "Alice"),
            CancellationToken.None);

        await Assert.ThrowsAsync<ArgumentException>(act);
        Assert.Equal(0, repository.GetByCodeCallCount);
    }

    // -------------------------------------------------------------------------
    // HandleAsync scenarios
    // -------------------------------------------------------------------------

    [Fact]
    public async Task HandleAsync_WhenRoomDoesNotExist_ThrowsRoomJoinNotFoundException()
    {
        var repository = new FakeRoomRepository();
        var handler = new JoinRoomHandler(repository, new FakeDomainEventDispatcher());

        var act = async () => await handler.HandleAsync(
            new JoinRoomCommand("AB12CD", UserId.New(), "Alice"),
            CancellationToken.None);

        var exception = await Assert.ThrowsAsync<RoomJoinNotFoundException>(act);
        Assert.Equal("AB12CD", exception.RoomCode.Value);
    }

    [Fact]
    public async Task HandleAsync_WhenUserIsAlreadyInRoom_ThrowsRoomJoinConflictException()
    {
        var existingUserId = UserId.New();
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Test Room", existingUserId, DateTime.UtcNow);
        room.ClearUncommittedEvents();
        var repository = new FakeRoomRepository(room);
        var handler = new JoinRoomHandler(repository, new FakeDomainEventDispatcher());

        var act = async () => await handler.HandleAsync(
            new JoinRoomCommand("AB12CD", existingUserId, "Alice"),
            CancellationToken.None);

        await Assert.ThrowsAsync<RoomJoinConflictException>(act);
    }

    [Fact]
    public async Task HandleAsync_WhenJoinSucceeds_ReturnsJoinedPlayerAndDispatchesEvent()
    {
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Test Room", UserId.New(), DateTime.UtcNow);
        room.ClearUncommittedEvents();
        var repository = new FakeRoomRepository(room);
        var dispatcher = new FakeDomainEventDispatcher();
        var handler = new JoinRoomHandler(repository, dispatcher);

        var result = await handler.HandleAsync(
            new JoinRoomCommand("AB12CD", UserId.New(), "Alice"),
            CancellationToken.None);

        Assert.Equal("AB12CD", result.RoomCode.Value);
        Assert.Equal("Alice", result.Name);
        Assert.NotEqual(Guid.Empty, result.PlayerId.Value);
        Assert.Equal(1, repository.TrySaveCallCount);
        Assert.Single(dispatcher.DispatchedEvents);
    }

    [Fact]
    public async Task HandleAsync_WhenSaveReturnsFalse_ThrowsRoomJoinConflictException()
    {
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Test Room", UserId.New(), DateTime.UtcNow);
        var repository = new FakeRoomRepository(room, trySaveResult: false);
        var handler = new JoinRoomHandler(repository, new FakeDomainEventDispatcher());

        var act = async () => await handler.HandleAsync(
            new JoinRoomCommand("AB12CD", UserId.New(), "Alice"),
            CancellationToken.None);

        await Assert.ThrowsAsync<RoomJoinConflictException>(act);
    }

    [Fact]
    public async Task HandleAsync_WhenSaveReportsRoomMissing_ThrowsRoomJoinNotFoundException()
    {
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Test Room", UserId.New(), DateTime.UtcNow);
        var repository = new FakeRoomRepository(room, throwOnSave: new RoomJoinSaveRoomMissingException(room.Id));
        var handler = new JoinRoomHandler(repository, new FakeDomainEventDispatcher());

        var act = async () => await handler.HandleAsync(
            new JoinRoomCommand("AB12CD", UserId.New(), "Alice"),
            CancellationToken.None);

        var exception = await Assert.ThrowsAsync<RoomJoinNotFoundException>(act);
        Assert.Equal("AB12CD", exception.RoomCode.Value);
    }

    [Fact]
    public async Task HandleAsync_WhenCancellationIsRequested_ThrowsOperationCanceledException()
    {
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Test Room", UserId.New(), DateTime.UtcNow);
        var repository = new FakeRoomRepository(room);
        var handler = new JoinRoomHandler(repository, new FakeDomainEventDispatcher());

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await handler.HandleAsync(
            new JoinRoomCommand("AB12CD", UserId.New(), "Alice"),
            cts.Token);

        await Assert.ThrowsAsync<OperationCanceledException>(act);
        Assert.Equal(0, repository.TrySaveCallCount);
    }

    // -------------------------------------------------------------------------
    // Fake
    // -------------------------------------------------------------------------

    private sealed class FakeRoomRepository : IRoomRepository
    {
        private readonly Room? _room;
        private readonly bool _trySaveResult;
        private readonly Exception? _throwOnSave;

        public FakeRoomRepository(Room? room = null, bool trySaveResult = true, Exception? throwOnSave = null)
        {
            _room = room;
            _trySaveResult = trySaveResult;
            _throwOnSave = throwOnSave;
        }

        public int GetByCodeCallCount { get; private set; }
        public int TrySaveCallCount { get; private set; }

        public Task<bool> TryAddAsync(Room room, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not used by JoinRoomHandler.");

        public Task<Room?> GetByCodeAsync(RoomCode roomCode, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            GetByCodeCallCount++;
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
            => throw new NotSupportedException("Not used by JoinRoomHandler.");
    }
}
