using BOTC.Application.Features.Rooms.LeaveRoom;
using BOTC.Application.Tests.Fakes;
using BOTC.Domain.Rooms;

namespace BOTC.Application.Tests.Features.Rooms.LeaveRoom;

public sealed class LeaveRoomHandlerTests
{
    [Fact]
    public void Constructor_WhenRepositoryIsNull_ThrowsArgumentNullException()
    {
        Action act = () => _ = new LeaveRoomHandler(null!, new FakeDomainEventDispatcher());

        var exception = Assert.Throws<ArgumentNullException>(act);
        Assert.Equal("roomLeaveRepository", exception.ParamName);
    }

    [Fact]
    public void Constructor_WhenDispatcherIsNull_ThrowsArgumentNullException()
    {
        Action act = () => _ = new LeaveRoomHandler(new FakeRoomLeaveRepository(null), null!);

        var exception = Assert.Throws<ArgumentNullException>(act);
        Assert.Equal("domainEventDispatcher", exception.ParamName);
    }

    [Fact]
    public async Task HandleAsync_WhenHostLeavesAndPlayersRemain_SavesRoomAndReturnsTransferredHost()
    {
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Host", DateTime.UtcNow);
        var alice = room.JoinPlayer("Alice", DateTime.UtcNow.AddSeconds(1));
        room.JoinPlayer("Bob", DateTime.UtcNow.AddSeconds(2));
        room.ClearUncommittedEvents(); // simulate room loaded fresh from DB (no prior events)
        var repository = new FakeRoomLeaveRepository(room);
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
    public async Task HandleAsync_WhenLastPlayerLeaves_DeletesRoomAndReturnsRemovedOutcome()
    {
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Host", DateTime.UtcNow);
        room.ClearUncommittedEvents(); // simulate room loaded fresh from DB (no prior events)
        var repository = new FakeRoomLeaveRepository(room);
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
    public async Task HandleAsync_WhenPlayerIsMissing_ThrowsRoomLeavePlayerNotFoundException()
    {
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Host", DateTime.UtcNow);
        var repository = new FakeRoomLeaveRepository(room);
        var dispatcher = new FakeDomainEventDispatcher();
        var handler = new LeaveRoomHandler(repository, dispatcher);
        var missingPlayerId = RoomPlayerId.New();

        var act = async () => await handler.HandleAsync(
            new LeaveRoomCommand("AB12CD", missingPlayerId.Value.ToString()),
            CancellationToken.None);

        var exception = await Assert.ThrowsAsync<BOTC.Application.Features.Rooms.LeaveRoom.RoomLeavePlayerNotFoundException>(act);
        Assert.Equal(room.Code, exception.RoomCode);
        Assert.Equal(missingPlayerId, exception.PlayerId);
    }

    private sealed class FakeRoomLeaveRepository : IRoomLeaveRepository
    {
        private readonly Room? _room;

        public FakeRoomLeaveRepository(Room? room)
        {
            _room = room;
        }

        public int TrySaveCallCount { get; private set; }

        public int TryDeleteCallCount { get; private set; }

        public Task<Room?> GetByCodeAsync(RoomCode roomCode, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_room);
        }

        public Task<bool> TrySaveAsync(Room room, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            TrySaveCallCount++;
            return Task.FromResult(true);
        }

        public Task<bool> TryDeleteAsync(RoomId roomId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            TryDeleteCallCount++;
            return Task.FromResult(true);
        }
    }
}
