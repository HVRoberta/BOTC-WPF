using BOTC.Domain.Rooms;
using BOTC.Infrastructure.Persistence;
using BOTC.Infrastructure.Rooms;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BOTC.Infrastructure.Tests.Rooms;

public sealed class RoomRepositoryTests : IDisposable
{
    private readonly SqliteConnection connection;
    private readonly BotcDbContext dbContext;
    private readonly RoomRepository repository;

    public RoomRepositoryTests()
    {
        connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<BotcDbContext>()
            .UseSqlite(connection)
            .Options;

        dbContext = new BotcDbContext(options);
        dbContext.Database.EnsureCreated();

        repository = new RoomRepository(dbContext);
    }

    [Fact]
    public async Task TryAddAsync_WhenRoomCodeIsUnique_ReturnsTrueAndPersistsRoomWithHostPlayer()
    {
        // Arrange
        var room = CreateRoom("AB12CD", "Host");

        // Act
        var result = await repository.TryAddAsync(room, CancellationToken.None);

        // Assert
        Assert.True(result);
        var persisted = await dbContext.Rooms.Include(r => r.Players).SingleAsync(r => r.Id == room.Id.Value);
        Assert.Equal("AB12CD", persisted.Code);
        Assert.Equal((int)RoomStatus.WaitingForPlayers, persisted.Status);
        Assert.Equal(room.CreatedAtUtc, persisted.CreatedAtUtc);
        Assert.Single(persisted.Players);
        Assert.Equal("Host", persisted.Players.Single().DisplayName);
        Assert.Equal((int)RoomPlayerRole.Host, persisted.Players.Single().Role);
    }

    [Fact]
    public async Task TryAddAsync_WhenRoomCodeAlreadyExists_ReturnsFalse()
    {
        // Arrange
        var firstRoom = CreateRoom("AB12CD", "First Host");
        var duplicateCodeRoom = CreateRoom("AB12CD", "Second Host");

        await repository.TryAddAsync(firstRoom, CancellationToken.None);

        // Act
        var result = await repository.TryAddAsync(duplicateCodeRoom, CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetByCodeAsync_WhenRoomExists_ReturnsRoomWithPlayers()
    {
        // Arrange
        var room = CreateRoom("AB12CD", "Host");
        room.JoinPlayer("Alice", DateTime.UtcNow.AddSeconds(1));
        await repository.TryAddAsync(room, CancellationToken.None);

        // Act
        var result = await repository.GetByCodeAsync(new RoomCode("AB12CD"), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(room.Id, result!.Id);
        Assert.Equal(2, result.Players.Count);
        Assert.Contains(result.Players, player => player.DisplayName == "Host" && player.Role == RoomPlayerRole.Host);
        Assert.Contains(result.Players, player => player.DisplayName == "Alice" && player.Role == RoomPlayerRole.Player);
    }

    [Fact]
    public async Task TrySaveAsync_WhenRoomExists_PersistsNewlyJoinedPlayer()
    {
        // Arrange
        var room = CreateRoom("AB12CD", "Host");
        await repository.TryAddAsync(room, CancellationToken.None);

        var loaded = await repository.GetByCodeAsync(new RoomCode("AB12CD"), CancellationToken.None);
        Assert.NotNull(loaded);
        loaded!.JoinPlayer("Alice", DateTime.UtcNow.AddSeconds(1));

        // Act
        var saved = await repository.TrySaveAsync(loaded, CancellationToken.None);

        // Assert
        Assert.True(saved);
        var persistedPlayers = await dbContext.RoomPlayers
            .Where(player => player.RoomId == loaded.Id.Value)
            .ToListAsync();
        Assert.Equal(2, persistedPlayers.Count);
        Assert.Contains(persistedPlayers, player => player.DisplayName == "Alice");
    }

    [Fact]
    public async Task TrySaveAsync_WhenDuplicateNormalizedDisplayNameConflictOccurs_ReturnsFalse()
    {
        // Arrange
        var room = CreateRoom("AB12CD", "Host");
        await repository.TryAddAsync(room, CancellationToken.None);

        var first = await repository.GetByCodeAsync(new RoomCode("AB12CD"), CancellationToken.None);
        var second = await repository.GetByCodeAsync(new RoomCode("AB12CD"), CancellationToken.None);
        Assert.NotNull(first);
        Assert.NotNull(second);

        first!.JoinPlayer("Alice", DateTime.UtcNow.AddSeconds(1));
        var firstSave = await repository.TrySaveAsync(first, CancellationToken.None);
        Assert.True(firstSave);

        second!.JoinPlayer("ALICE", DateTime.UtcNow.AddSeconds(2));
        var secondSave = await repository.TrySaveAsync(second, CancellationToken.None);

        // Assert
        Assert.False(secondSave);
    }

    private static Room CreateRoom(string code, string hostDisplayName)
    {
        return Room.Create(
            RoomId.New(),
            new RoomCode(code),
            hostDisplayName,
            DateTime.UtcNow);
    }

    public void Dispose()
    {
        dbContext.Dispose();
        connection.Dispose();
    }
}
