using BOTC.Domain.Rooms;
using BOTC.Infrastructure.Persistence;
using BOTC.Infrastructure.Persistence.Rooms;
using BOTC.Infrastructure.Rooms;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BOTC.Infrastructure.Tests.Rooms;

public sealed class RoomLobbyReadRepositoryTests : IDisposable
{
    private readonly SqliteConnection connection;
    private readonly BotcDbContext dbContext;
    private readonly RoomLobbyReadRepository repository;

    public RoomLobbyReadRepositoryTests()
    {
        connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<BotcDbContext>()
            .UseSqlite(connection)
            .Options;

        dbContext = new BotcDbContext(options);
        dbContext.Database.EnsureCreated();

        repository = new RoomLobbyReadRepository(dbContext);
    }

    [Fact]
    public async Task GetByRoomCodeAsync_WhenRoomExists_ReturnsMappedLobbyState()
    {
        // Arrange
        dbContext.Rooms.Add(new RoomEntity
        {
            Id = Guid.NewGuid(),
            Code = "AB12CD",
            HostDisplayName = "Host",
            Status = (int)RoomStatus.WaitingForPlayers,
            CreatedAtUtc = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        // Act
        var result = await repository.GetByRoomCodeAsync(new RoomCode("AB12CD"), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("AB12CD", result!.RoomCode.Value);
        Assert.Equal("Host", result.HostDisplayName);
        Assert.Equal(RoomStatus.WaitingForPlayers, result.Status);
        Assert.Empty(dbContext.ChangeTracker.Entries());
    }

    [Fact]
    public async Task GetByRoomCodeAsync_WhenRoomDoesNotExist_ReturnsNull()
    {
        // Act
        var result = await repository.GetByRoomCodeAsync(new RoomCode("AB12CD"), CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByRoomCodeAsync_WhenPersistedStatusIsInvalid_ThrowsInvalidOperationException()
    {
        // Arrange
        dbContext.Rooms.Add(new RoomEntity
        {
            Id = Guid.NewGuid(),
            Code = "AB12CD",
            HostDisplayName = "Host",
            Status = 999,
            CreatedAtUtc = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        // Act
        var act = async () => await repository.GetByRoomCodeAsync(new RoomCode("AB12CD"), CancellationToken.None);

        // Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(act);
        Assert.Contains("invalid persisted status", exception.Message, StringComparison.Ordinal);
    }

    public void Dispose()
    {
        dbContext.Dispose();
        connection.Dispose();
    }
}

