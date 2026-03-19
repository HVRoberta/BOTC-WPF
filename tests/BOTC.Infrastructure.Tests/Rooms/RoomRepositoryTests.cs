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
        // Keep the connection open for the lifetime of the test so the in-memory SQLite database persists.
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
    public async Task TryAddAsync_WhenRoomCodeIsUnique_ReturnsTrueAndPersistsRoom()
    {
        // Arrange
        var room = CreateRoom("AB12CD", "Host");

        // Act
        var result = await repository.TryAddAsync(room, CancellationToken.None);

        // Assert
        Assert.True(result);
        var persisted = await dbContext.Rooms.SingleAsync(r => r.Id == room.Id.Value);
        Assert.Equal("AB12CD", persisted.Code);
        Assert.Equal("Host", persisted.HostDisplayName);
        Assert.Equal((int)RoomStatus.WaitingForPlayers, persisted.Status);
        Assert.Equal(room.CreatedAtUtc, persisted.CreatedAtUtc);
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
    public async Task TryAddAsync_WhenRoomCodeAlreadyExists_DoesNotPersistDuplicateRoom()
    {
        // Arrange
        var firstRoom = CreateRoom("AB12CD", "First Host");
        var duplicateCodeRoom = CreateRoom("AB12CD", "Second Host");

        await repository.TryAddAsync(firstRoom, CancellationToken.None);

        // Act
        await repository.TryAddAsync(duplicateCodeRoom, CancellationToken.None);

        // Assert – only one room with this code exists in the database.
        var count = await dbContext.Rooms.CountAsync(r => r.Code == "AB12CD");
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task TryAddAsync_WhenMultipleRoomsHaveUniqueCode_PersistsAll()
    {
        // Arrange
        var rooms = new[]
        {
            CreateRoom("AA0001", "Host One"),
            CreateRoom("BB0002", "Host Two"),
            CreateRoom("CC0003", "Host Three")
        };

        // Act
        var results = new bool[rooms.Length];
        for (var i = 0; i < rooms.Length; i++)
        {
            results[i] = await repository.TryAddAsync(rooms[i], CancellationToken.None);
        }

        // Assert
        Assert.All(results, r => Assert.True(r));
        Assert.Equal(3, await dbContext.Rooms.CountAsync());
    }

    [Fact]
    public async Task TryAddAsync_WhenCancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        var room = CreateRoom("AB12CD", "Host");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var act = async () => await repository.TryAddAsync(room, cts.Token);

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(act);
    }

    [Fact]
    public async Task TryAddAsync_WhenRoomIdAlreadyExists_RethrowsDbUpdateException()
    {
        // Arrange
        var sharedId = RoomId.New();
        var firstRoom = CreateRoom(sharedId, "AB12CD", "First Host");
        var duplicateIdRoom = CreateRoom(sharedId, "EF34GH", "Second Host");

        await repository.TryAddAsync(firstRoom, CancellationToken.None);
        dbContext.ChangeTracker.Clear();

        // Act
        var act = async () => await repository.TryAddAsync(duplicateIdRoom, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<DbUpdateException>(act);
    }

    [Fact]
    public async Task TryAddAsync_WhenNonUniqueConstraintViolationOccurs_RethrowsDbUpdateException()
    {
        // Arrange
        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE TRIGGER Rooms_PreventInsert
            BEFORE INSERT ON Rooms
            BEGIN
                SELECT RAISE(ABORT, 'blocked by test trigger');
            END;
            """);

        var room = CreateRoom("AB12CD", "Host");

        // Act
        var act = async () => await repository.TryAddAsync(room, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<DbUpdateException>(act);
    }

    private static Room CreateRoom(string code, string hostDisplayName)
    {
        return Room.Create(
            RoomId.New(),
            new RoomCode(code),
            hostDisplayName,
            DateTime.UtcNow);
    }

    private static Room CreateRoom(RoomId id, string code, string hostDisplayName)
    {
        return Room.Create(
            id,
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
