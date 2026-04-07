using BOTC.Application.Features.Rooms.SetPlayerReady;
using BOTC.Application.Tests.Fakes;
using BOTC.Domain.Rooms;

namespace BOTC.Application.Tests.Features.Rooms.SetPlayerReady;

public sealed class SetPlayerReadyHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenPlayerExists_UpdatesReadinessAndSaves()
    {
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Host", DateTime.UtcNow);
        var alice = room.JoinPlayer("Alice", DateTime.UtcNow.AddSeconds(1));
        room.ClearUncommittedEvents(); // simulate room loaded fresh from DB (no prior events)
        var repository = new FakeRoomSetPlayerReadyRepository(room);
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
    public async Task HandleAsync_WhenPlayerIsMissing_ThrowsRoomSetPlayerReadyPlayerNotFoundException()
    {
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Host", DateTime.UtcNow);
        var repository = new FakeRoomSetPlayerReadyRepository(room);
        var dispatcher = new FakeDomainEventDispatcher();
        var handler = new SetPlayerReadyHandler(repository, dispatcher);
        var missingPlayerId = RoomPlayerId.New();

        var act = async () => await handler.HandleAsync(
            new SetPlayerReadyCommand("AB12CD", missingPlayerId.Value.ToString(), true),
            CancellationToken.None);

        var exception = await Assert.ThrowsAsync<
            BOTC.Application.Features.Rooms.SetPlayerReady.RoomSetPlayerReadyPlayerNotFoundException>(act);
        Assert.Equal(room.Code, exception.RoomCode);
        Assert.Equal(missingPlayerId, exception.PlayerId);
    }

    private sealed class FakeRoomSetPlayerReadyRepository : IRoomSetPlayerReadyRepository
    {
        private readonly Room? room;

        public FakeRoomSetPlayerReadyRepository(Room? room)
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


