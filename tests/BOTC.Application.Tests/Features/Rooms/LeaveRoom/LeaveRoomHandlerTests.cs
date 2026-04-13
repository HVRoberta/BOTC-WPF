using BOTC.Application.Abstractions.Persistence;
using BOTC.Application.Features.Rooms.LeaveRoom;
using BOTC.Application.Tests.Fakes;
using BOTC.Domain.Rooms;
using BOTC.Domain.Users;

namespace BOTC.Application.Tests.Features.Rooms.LeaveRoom;

public sealed class LeaveRoomHandlerTests
{
    // -------------------------------------------------------------------------
    // Constructor guard clauses
    // -------------------------------------------------------------------------

    [Fact]
    public void Constructor_WhenRepositoryIsNull_ThrowsArgumentNullException()
    {
        Action act = () => _ = new LeaveRoomHandler(null!, new FakeDomainEventDispatcher());

        var exception = Assert.Throws<ArgumentNullException>(act);
        Assert.Equal("roomRepository", exception.ParamName);
    }

    [Fact]
    public void Constructor_WhenDispatcherIsNull_ThrowsArgumentNullException()
    {
        Action act = () => _ = new LeaveRoomHandler(new FakeRoomRepository(null), null!);

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
        var handler = new LeaveRoomHandler(repository, new FakeDomainEventDispatcher());

        var act = async () => await handler.HandleAsync(
            new LeaveRoomCommand("AB12CD", "not-a-guid"),
            CancellationToken.None);

        await Assert.ThrowsAsync<ArgumentException>(act);
    }

    // -------------------------------------------------------------------------
    // HandleAsync scenarios
    // -------------------------------------------------------------------------

    [Fact]
    public async Task HandleAsync_WhenRoomDoesNotExist_ThrowsRoomLeaveRoomNotFoundException()
    {
        var repository = new FakeRoomRepository(null);
        var handler = new LeaveRoomHandler(repository, new FakeDomainEventDispatcher());

        var act = async () => await handler.HandleAsync(
            new LeaveRoomCommand("AB12CD", PlayerId.New().Value.ToString()),
            CancellationToken.None);

        var exception = await Assert.ThrowsAsync<RoomLeaveRoomNotFoundException>(act);
        Assert.Equal("AB12CD", exception.RoomCode.Value);
    }

    [Fact]
    public async Task HandleAsync_WhenPlayerIsNotInRoom_ThrowsRoomLeavePlayerNotFoundException()
    {
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Test Room", UserId.New(), DateTime.UtcNow);
        var repository = new FakeRoomRepository(room);
        var handler = new LeaveRoomHandler(repository, new FakeDomainEventDispatcher());
        var missingPlayerId = PlayerId.New();

        var act = async () => await handler.HandleAsync(
            new LeaveRoomCommand("AB12CD", missingPlayerId.Value.ToString()),
            CancellationToken.None);

        var exception = await Assert.ThrowsAsync<BOTC.Application.Features.Rooms.LeaveRoom.RoomLeavePlayerNotFoundException>(act);
        Assert.Equal(room.Code, exception.RoomCode);
        Assert.Equal(missingPlayerId, exception.PlayerId);
    }

    [Fact]
    public async Task HandleAsync_WhenLastPlayerLeaves_DeletesRoomAndReturnsRemovedOutcome()
    {
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Test Room", UserId.New(), DateTime.UtcNow);
        room.ClearUncommittedEvents();
        var repository = new FakeRoomRepository(room);
        var dispatcher = new FakeDomainEventDispatcher();
        var handler = new LeaveRoomHandler(repository, dispatcher);

        var result = await handler.HandleAsync(
            new LeaveRoomCommand("AB12CD", room.HostPlayerId.Value.ToString()),
            CancellationToken.None);

        Assert.True(result.RoomWasRemoved);
        Assert.Null(result.NewHostPlayerId);
        Assert.Equal(0, repository.TrySaveCallCount);
        Assert.Equal(1, repository.TryDeleteCallCount);
        Assert.Single(dispatcher.DispatchedEvents);
    }

    [Fact]
    public async Task HandleAsync_WhenHostLeavesAndPlayersRemain_TransfersHostAndSavesRoom()
    {
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Test Room", UserId.New(), DateTime.UtcNow);
        var alice = room.JoinPlayer(UserId.New(), DateTime.UtcNow.AddSeconds(1));
        room.JoinPlayer(UserId.New(), DateTime.UtcNow.AddSeconds(2));
        room.ClearUncommittedEvents();
        var repository = new FakeRoomRepository(room);
        var dispatcher = new FakeDomainEventDispatcher();
        var handler = new LeaveRoomHandler(repository, dispatcher);

        var result = await handler.HandleAsync(
            new LeaveRoomCommand("AB12CD", room.HostPlayerId.Value.ToString()),
            CancellationToken.None);

        Assert.False(result.RoomWasRemoved);
        Assert.Equal(alice.Id, result.NewHostPlayerId);
        Assert.Equal(1, repository.TrySaveCallCount);
        Assert.Equal(0, repository.TryDeleteCallCount);
        Assert.Single(dispatcher.DispatchedEvents);
    }

    [Fact]
    public async Task HandleAsync_WhenNonHostPlayerLeaves_SavesRoomWithoutHostTransfer()
    {
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Test Room", UserId.New(), DateTime.UtcNow);
        var alice = room.JoinPlayer(UserId.New(), DateTime.UtcNow.AddSeconds(1));
        room.ClearUncommittedEvents();
        var repository = new FakeRoomRepository(room);
        var dispatcher = new FakeDomainEventDispatcher();
        var handler = new LeaveRoomHandler(repository, dispatcher);

        var result = await handler.HandleAsync(
            new LeaveRoomCommand("AB12CD", alice.Id.Value.ToString()),
            CancellationToken.None);

        Assert.False(result.RoomWasRemoved);
        Assert.Null(result.NewHostPlayerId);
        Assert.Equal(1, repository.TrySaveCallCount);
        Assert.Equal(0, repository.TryDeleteCallCount);
        Assert.Single(dispatcher.DispatchedEvents);
    }

    [Fact]
    public async Task HandleAsync_WhenSaveReturnsFalse_ThrowsRoomLeaveConflictException()
    {
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Test Room", UserId.New(), DateTime.UtcNow);
        var alice = room.JoinPlayer(UserId.New(), DateTime.UtcNow.AddSeconds(1));
        var repository = new FakeRoomRepository(room, trySaveResult: false);
        var handler = new LeaveRoomHandler(repository, new FakeDomainEventDispatcher());

        var act = async () => await handler.HandleAsync(
            new LeaveRoomCommand("AB12CD", alice.Id.Value.ToString()),
            CancellationToken.None);

        await Assert.ThrowsAsync<RoomLeaveConflictException>(act);
    }

    [Fact]
    public async Task HandleAsync_WhenSaveReportsRoomMissing_ThrowsRoomLeaveRoomNotFoundException()
    {
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Test Room", UserId.New(), DateTime.UtcNow);
        var alice = room.JoinPlayer(UserId.New(), DateTime.UtcNow.AddSeconds(1));
        var repository = new FakeRoomRepository(room, throwOnSave: new RoomLeaveSaveRoomMissingException(room.Id));
        var handler = new LeaveRoomHandler(repository, new FakeDomainEventDispatcher());

        var act = async () => await handler.HandleAsync(
            new LeaveRoomCommand("AB12CD", alice.Id.Value.ToString()),
            CancellationToken.None);

        var exception = await Assert.ThrowsAsync<RoomLeaveRoomNotFoundException>(act);
        Assert.Equal("AB12CD", exception.RoomCode.Value);
    }

    [Fact]
    public async Task HandleAsync_WhenCancellationIsRequested_ThrowsOperationCanceledException()
    {
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Test Room", UserId.New(), DateTime.UtcNow);
        var repository = new FakeRoomRepository(room);
        var handler = new LeaveRoomHandler(repository, new FakeDomainEventDispatcher());

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await handler.HandleAsync(
            new LeaveRoomCommand("AB12CD", PlayerId.New().Value.ToString()),
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
        public int TryDeleteCallCount { get; private set; }

        public Task<bool> TryAddAsync(Room room, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not used by LeaveRoomHandler.");

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
        {
            cancellationToken.ThrowIfCancellationRequested();
            TryDeleteCallCount++;
            return Task.FromResult(true);
        }
    }
}
