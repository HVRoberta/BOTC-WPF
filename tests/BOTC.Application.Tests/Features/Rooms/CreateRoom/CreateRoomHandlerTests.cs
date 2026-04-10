using BOTC.Application.Abstractions.Persistence;
using BOTC.Application.Abstractions.Services;
using BOTC.Application.Features.Rooms.CreateRoom;
using BOTC.Application.Tests.Fakes;
using BOTC.Domain.Rooms;
using BOTC.Domain.Users;

namespace BOTC.Application.Tests.Features.Rooms.CreateRoom;

public sealed class CreateRoomHandlerTests
{
    private static readonly UserId TestHostUserId = UserId.New();
    private const string TestRoomName = "Test Room";

    [Fact]
    public void Constructor_WhenRepositoryIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var generator = new FakeRoomCodeGenerator(["AB12CD"]);
        var dispatcher = new FakeDomainEventDispatcher();

        // Act
        Action act = () => _ = new CreateRoomHandler(null!, generator, dispatcher);

        // Assert
        var exception = Assert.Throws<ArgumentNullException>(act);
        Assert.Equal("roomRepository", exception.ParamName);
    }

    [Fact]
    public void Constructor_WhenRoomCodeGeneratorIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var repository = new FakeRoomRepository();
        var dispatcher = new FakeDomainEventDispatcher();

        // Act
        Action act = () => _ = new CreateRoomHandler(repository, null!, dispatcher);

        // Assert
        var exception = Assert.Throws<ArgumentNullException>(act);
        Assert.Equal("roomCodeGenerator", exception.ParamName);
    }

    [Fact]
    public void Constructor_WhenDispatcherIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var repository = new FakeRoomRepository();
        var generator = new FakeRoomCodeGenerator(["AB12CD"]);

        // Act
        Action act = () => _ = new CreateRoomHandler(repository, generator, null!);

        // Assert
        var exception = Assert.Throws<ArgumentNullException>(act);
        Assert.Equal("domainEventDispatcher", exception.ParamName);
    }

    [Fact]
    public async Task HandleAsync_WhenCommandIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var handler = new CreateRoomHandler(
            new FakeRoomRepository(),
            new FakeRoomCodeGenerator(["AB12CD"]),
            new FakeDomainEventDispatcher());

        // Act
        var act = async () => await handler.HandleAsync(null!, CancellationToken.None);

        // Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(act);
        Assert.Equal("command", exception.ParamName);
    }

    [Fact]
    public async Task HandleAsync_WhenFirstCodeIsUnique_ReturnsCreatedRoomResult()
    {
        // Arrange
        var repository = new FakeRoomRepository();
        var generator = new FakeRoomCodeGenerator(["AB12CD"]);
        var dispatcher = new FakeDomainEventDispatcher();
        var handler = new CreateRoomHandler(repository, generator, dispatcher);
        var command = new CreateRoomCommand(TestHostUserId, TestRoomName);
        var before = DateTime.UtcNow;

        // Act
        var result = await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        var after = DateTime.UtcNow;
        Assert.NotEqual(Guid.Empty, result.RoomId.Value);
        Assert.Equal("AB12CD", result.RoomCode.Value);
        Assert.Equal(TestRoomName, result.RoomName);
        Assert.Equal(RoomStatus.WaitingForPlayers, result.Status);
        Assert.Equal(DateTimeKind.Utc, result.CreatedAtUtc.Kind);
        Assert.True(result.CreatedAtUtc >= before && result.CreatedAtUtc <= after);
        Assert.Equal(1, generator.GenerateCallCount);
        Assert.Equal(1, repository.TryAddCallCount);
    }

    [Fact]
    public async Task HandleAsync_WhenFirstCodeCollidesAndSecondSucceeds_RetriesAndReturnsCreatedRoom()
    {
        // Arrange
        var repository = new FakeRoomRepository();
        repository.SeedExistingCode("AB12CD");

        var generator = new FakeRoomCodeGenerator(["AB12CD", "EF34GH"]);
        var dispatcher = new FakeDomainEventDispatcher();
        var handler = new CreateRoomHandler(repository, generator, dispatcher);
        var command = new CreateRoomCommand(TestHostUserId, TestRoomName);

        // Act
        var result = await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        Assert.Equal("EF34GH", result.RoomCode.Value);
        Assert.Equal(2, generator.GenerateCallCount);
        Assert.Equal(2, repository.TryAddCallCount);
    }

    [Fact]
    public async Task HandleAsync_WhenAllAttemptsCollide_ThrowsRoomCodeGenerationExhaustedException()
    {
        // Arrange
        var repository = new FakeRoomRepository();
        repository.SeedExistingCode("AB12CD");

        var generator = new FakeRoomCodeGenerator(Enumerable.Repeat("AB12CD", 10));
        var dispatcher = new FakeDomainEventDispatcher();
        var handler = new CreateRoomHandler(repository, generator, dispatcher);
        var command = new CreateRoomCommand(TestHostUserId, TestRoomName);

        // Act
        var act = async () => await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        var exception = await Assert.ThrowsAsync<RoomCodeGenerationExhaustedException>(act);
        Assert.Contains("10", exception.Message);
        Assert.Equal(10, generator.GenerateCallCount);
        Assert.Equal(10, repository.TryAddCallCount);
    }

    [Fact]
    public async Task HandleAsync_WhenGeneratorReturnsInvalidCode_FailsFast()
    {
        // Arrange
        var repository = new FakeRoomRepository();
        var generator = new FakeRoomCodeGenerator(["abc"]);
        var dispatcher = new FakeDomainEventDispatcher();
        var handler = new CreateRoomHandler(repository, generator, dispatcher);
        var command = new CreateRoomCommand(TestHostUserId, TestRoomName);

        // Act
        var act = async () => await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(act);
        Assert.Equal(1, generator.GenerateCallCount);
        Assert.Equal(0, repository.TryAddCallCount);
    }

    [Fact]
    public async Task HandleAsync_WhenRepositoryThrows_PropagatesException()
    {
        // Arrange
        var repositoryException = new InvalidOperationException("Repository failure");
        var repository = new FakeRoomRepository(repositoryException);
        var generator = new FakeRoomCodeGenerator(["AB12CD"]);
        var dispatcher = new FakeDomainEventDispatcher();
        var handler = new CreateRoomHandler(repository, generator, dispatcher);
        var command = new CreateRoomCommand(TestHostUserId, TestRoomName);

        // Act
        var act = async () => await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(act);
        Assert.Equal("Repository failure", exception.Message);
        Assert.Equal(1, generator.GenerateCallCount);
        Assert.Equal(1, repository.TryAddCallCount);
    }

    [Fact]
    public async Task HandleAsync_WhenCancellationIsRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        var repository = new FakeRoomRepository();
        var generator = new FakeRoomCodeGenerator(["AB12CD"]);
        var dispatcher = new FakeDomainEventDispatcher();
        var handler = new CreateRoomHandler(repository, generator, dispatcher);
        var command = new CreateRoomCommand(TestHostUserId, TestRoomName);

        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        // Act
        var act = async () => await handler.HandleAsync(command, cancellationTokenSource.Token);

        // Assert
        await Assert.ThrowsAsync<OperationCanceledException>(act);
        Assert.Equal(0, generator.GenerateCallCount);
        Assert.Equal(0, repository.TryAddCallCount);
    }

    private sealed class FakeRoomRepository : IRoomRepository
    {
        private readonly HashSet<RoomCode> existingCodes = [];
        private readonly Exception? exceptionToThrow;

        public FakeRoomRepository(Exception? exceptionToThrow = null)
        {
            this.exceptionToThrow = exceptionToThrow;
        }

        public int TryAddCallCount { get; private set; }

        public Task<bool> TryAddAsync(Room room, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            TryAddCallCount++;

            if (exceptionToThrow is not null)
            {
                throw exceptionToThrow;
            }

            return Task.FromResult(existingCodes.Add(room.Code));
        }

        public void SeedExistingCode(string code)
        {
            existingCodes.Add(new RoomCode(code));
        }
    }

    private sealed class FakeRoomCodeGenerator : IRoomCodeGenerator
    {
        private readonly Queue<string> generatedCodes;

        public FakeRoomCodeGenerator(IEnumerable<string> generatedCodes)
        {
            this.generatedCodes = new Queue<string>(generatedCodes);
        }

        public int GenerateCallCount { get; private set; }

        public string Generate()
        {
            GenerateCallCount++;

            if (generatedCodes.Count == 0)
            {
                throw new InvalidOperationException("No generated room code configured for test.");
            }

            return generatedCodes.Dequeue();
        }
    }
}

