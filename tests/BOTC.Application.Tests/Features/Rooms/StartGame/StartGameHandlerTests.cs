using BOTC.Application.Features.Rooms.StartGame;
using BOTC.Application.Tests.Fakes;
using BOTC.Domain.Rooms;
using BOTC.Domain.Users;

namespace BOTC.Application.Tests.Features.Rooms.StartGame;

public sealed class StartGameHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenAllNonHostPlayersAreReady_StartsGameAndSaves()
    {
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Test Room", UserId.New(), DateTime.UtcNow);
        var alice = room.JoinPlayer(UserId.New(), DateTime.UtcNow.AddSeconds(1));
        room.SetPlayerReady(alice.Id, true);
        room.ClearUncommittedEvents(); // simulate room loaded fresh from DB (no prior events)
        var repository = new FakeRoomStartGameRepository(room);
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
    public async Task HandleAsync_WhenGameIsBlockedByReadiness_ReturnsReasonWithoutSaving()
    {
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Test Room", UserId.New(), DateTime.UtcNow);
        room.JoinPlayer(UserId.New(), DateTime.UtcNow.AddSeconds(1));
        var repository = new FakeRoomStartGameRepository(room);
        var dispatcher = new FakeDomainEventDispatcher();
        var handler = new StartGameHandler(repository, dispatcher);

        var result = await handler.HandleAsync(
            new StartGameCommand("AB12CD", room.HostPlayerId.Value.ToString()),
            CancellationToken.None);

        Assert.False(result.IsStarted);
        Assert.Equal(RoomStartGameBlockedReason.NonHostPlayersNotReady, result.BlockedReason);
        Assert.Equal(0, repository.TrySaveCallCount);
    }

    private sealed class FakeRoomStartGameRepository : IRoomStartGameRepository
    {
        private readonly Room? room;

        public FakeRoomStartGameRepository(Room? room)
        {
            this.room = room;
        }

        public int TrySaveCallCount { get; private set; }

        public Task<Room?> GetByCodeAsync(RoomCode roomCode, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(room);
        }

        public Task<bool> TrySaveAsync(Room roomToSave, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            TrySaveCallCount++;
            return Task.FromResult(true);
        }
    }
}
