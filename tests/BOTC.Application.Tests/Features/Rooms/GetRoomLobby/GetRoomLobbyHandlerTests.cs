using BOTC.Application.Features.Rooms.GetRoomLobby;
using BOTC.Domain.Rooms;

namespace BOTC.Application.Tests.Features.Rooms.GetRoomLobby;

public sealed class GetRoomLobbyHandlerTests
{
    [Fact]
    public void Constructor_WhenQueryServiceIsNull_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => _ = new GetRoomLobbyHandler(null!);

        // Assert
        var exception = Assert.Throws<ArgumentNullException>(act);
        Assert.Equal("roomLobbyQueryService", exception.ParamName);
    }

    [Fact]
    public async Task HandleAsync_WhenQueryIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var handler = new GetRoomLobbyHandler(new FakeRoomLobbyQueryService());

        // Act
        var act = async () => await handler.HandleAsync(null!, CancellationToken.None);

        // Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(act);
        Assert.Equal("query", exception.ParamName);
    }

    [Fact]
    public async Task HandleAsync_WhenRoomCodeIsInvalid_ThrowsArgumentExceptionBeforeRepositoryCall()
    {
        // Arrange
        var repository = new FakeRoomLobbyQueryService();
        var handler = new GetRoomLobbyHandler(repository);

        // Act
        var act = async () => await handler.HandleAsync(new GetRoomLobbyQuery("abc"), CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(act);
        Assert.Equal(0, repository.GetByRoomCodeCallCount);
    }

    [Fact]
    public async Task HandleAsync_WhenRoomDoesNotExist_ThrowsRoomLobbyNotFoundException()
    {
        // Arrange
        var repository = new FakeRoomLobbyQueryService();
        var handler = new GetRoomLobbyHandler(repository);

        // Act
        var act = async () => await handler.HandleAsync(new GetRoomLobbyQuery("AB12CD"), CancellationToken.None);

        // Assert
        var exception = await Assert.ThrowsAsync<RoomLobbyNotFoundException>(act);
        Assert.Equal("AB12CD", exception.RoomCode.Value);
        Assert.Equal(1, repository.GetByRoomCodeCallCount);
    }

    [Fact]
    public async Task HandleAsync_WhenRoomExists_ReturnsLobbyState()
    {
        // Arrange
        var repository = new FakeRoomLobbyQueryService();
        repository.SeedLobby(new GetRoomLobbyResult(
            new RoomCode("AB12CD"),
            "Host",
            RoomStatus.WaitingForPlayers));

        var handler = new GetRoomLobbyHandler(repository);

        // Act
        var result = await handler.HandleAsync(new GetRoomLobbyQuery("AB12CD"), CancellationToken.None);

        // Assert
        Assert.Equal("AB12CD", result.RoomCode.Value);
        Assert.Equal("Host", result.HostDisplayName);
        Assert.Equal(RoomStatus.WaitingForPlayers, result.Status);
        Assert.Equal(1, repository.GetByRoomCodeCallCount);
    }

    [Fact]
    public async Task HandleAsync_WhenCancellationIsRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        var repository = new FakeRoomLobbyQueryService();
        repository.SeedLobby(new GetRoomLobbyResult(
            new RoomCode("AB12CD"),
            "Host",
            RoomStatus.WaitingForPlayers));

        var handler = new GetRoomLobbyHandler(repository);

        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        // Act
        var act = async () => await handler.HandleAsync(new GetRoomLobbyQuery("AB12CD"), cancellationTokenSource.Token);

        // Assert
        await Assert.ThrowsAsync<OperationCanceledException>(act);
    }

    private sealed class FakeRoomLobbyQueryService : IRoomLobbyQueryService
    {
        private readonly Dictionary<string, GetRoomLobbyResult> lobbiesByCode = new(StringComparer.Ordinal);

        public int GetByRoomCodeCallCount { get; private set; }

        public Task<GetRoomLobbyResult?> GetByRoomCodeAsync(RoomCode roomCode, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            GetByRoomCodeCallCount++;

            lobbiesByCode.TryGetValue(roomCode.Value, out var result);
            return Task.FromResult(result);
        }

        public void SeedLobby(GetRoomLobbyResult lobby)
        {
            lobbiesByCode[lobby.RoomCode.Value] = lobby;
        }
    }
}

