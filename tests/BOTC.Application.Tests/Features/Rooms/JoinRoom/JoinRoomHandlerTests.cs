using BOTC.Application.Features.Rooms.JoinRoom;
using BOTC.Domain.Rooms;

namespace BOTC.Application.Tests.Features.Rooms.JoinRoom;

public sealed class JoinRoomHandlerTests
{
    [Fact]
    public void Constructor_WhenRepositoryIsNull_ThrowsArgumentNullException()
    {
        Action act = () => _ = new JoinRoomHandler(null!);

        var exception = Assert.Throws<ArgumentNullException>(act);
        Assert.Equal("roomJoinRepository", exception.ParamName);
    }

    [Fact]
    public async Task HandleAsync_WhenCommandIsNull_ThrowsArgumentNullException()
    {
        var handler = new JoinRoomHandler(new FakeRoomJoinRepository());

        var act = async () => await handler.HandleAsync(null!, CancellationToken.None);

        var exception = await Assert.ThrowsAsync<ArgumentNullException>(act);
        Assert.Equal("command", exception.ParamName);
    }

    [Fact]
    public async Task HandleAsync_WhenRoomCodeIsInvalid_ThrowsArgumentExceptionBeforeRepositoryCall()
    {
        var repository = new FakeRoomJoinRepository();
        var handler = new JoinRoomHandler(repository);

        var act = async () => await handler.HandleAsync(new JoinRoomCommand("abc", "Alice"), CancellationToken.None);

        await Assert.ThrowsAsync<ArgumentException>(act);
        Assert.Equal(0, repository.GetByCodeCallCount);
    }

    [Fact]
    public async Task HandleAsync_WhenRoomDoesNotExist_ThrowsRoomJoinNotFoundException()
    {
        var repository = new FakeRoomJoinRepository();
        var handler = new JoinRoomHandler(repository);

        var act = async () => await handler.HandleAsync(new JoinRoomCommand("AB12CD", "Alice"), CancellationToken.None);

        var exception = await Assert.ThrowsAsync<RoomJoinNotFoundException>(act);
        Assert.Equal("AB12CD", exception.RoomCode.Value);
    }

    [Fact]
    public async Task HandleAsync_WhenJoinSucceeds_ReturnsJoinedPlayer()
    {
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Host", DateTime.UtcNow);
        var repository = new FakeRoomJoinRepository(room);
        var handler = new JoinRoomHandler(repository);

        var result = await handler.HandleAsync(new JoinRoomCommand("AB12CD", "Alice"), CancellationToken.None);

        Assert.Equal("AB12CD", result.RoomCode.Value);
        Assert.Equal("Alice", result.DisplayName);
        Assert.NotEqual(Guid.Empty, result.PlayerId.Value);
        Assert.Equal(1, repository.TrySaveCallCount);
    }

    [Fact]
    public async Task HandleAsync_WhenSaveReturnsFalse_ThrowsRoomJoinConflictException()
    {
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Host", DateTime.UtcNow);
        var repository = new FakeRoomJoinRepository(room, trySaveResult: false);
        var handler = new JoinRoomHandler(repository);

        var act = async () => await handler.HandleAsync(new JoinRoomCommand("AB12CD", "Alice"), CancellationToken.None);

        await Assert.ThrowsAsync<RoomJoinConflictException>(act);
    }

    [Fact]
    public async Task HandleAsync_WhenSaveReportsRoomMissing_ThrowsRoomJoinNotFoundException()
    {
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Host", DateTime.UtcNow);
        var repository = new FakeRoomJoinRepository(room, throwOnSave: new RoomJoinSaveRoomMissingException(room.Id));
        var handler = new JoinRoomHandler(repository);

        var act = async () => await handler.HandleAsync(new JoinRoomCommand("AB12CD", "Alice"), CancellationToken.None);

        var exception = await Assert.ThrowsAsync<RoomJoinNotFoundException>(act);
        Assert.Equal("AB12CD", exception.RoomCode.Value);
    }

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

        public int GetByCodeCallCount { get; private set; }

        public int TrySaveCallCount { get; private set; }

        public Task<Room?> GetByCodeAsync(RoomCode roomCode, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            GetByCodeCallCount++;
            return Task.FromResult(_room);
        }

        public Task<bool> TrySaveAsync(Room roomToSave, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            TrySaveCallCount++;

            if (_throwOnSave is not null)
            {
                throw _throwOnSave;
            }

            return Task.FromResult(_trySaveResult);
        }
    }
}
