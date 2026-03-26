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
    private readonly RoomLobbyQueryService repository;

    public RoomLobbyReadRepositoryTests()
    {
        connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<BotcDbContext>()
            .UseSqlite(connection)
            .Options;

        dbContext = new BotcDbContext(options);
        dbContext.Database.EnsureCreated();

        repository = new RoomLobbyQueryService(dbContext);
    }

    [Fact]
    public async Task GetByRoomCodeAsync_WhenRoomExists_ReturnsMappedLobbyState()
    {
        // Arrange
        var roomId = Guid.NewGuid();
        dbContext.Rooms.Add(new RoomEntity
        {
            Id = roomId,
            Code = "AB12CD",
            Status = (int)RoomStatus.WaitingForPlayers,
            CreatedAtUtc = DateTime.UtcNow,
            Players =
            [
                new RoomPlayerEntity
                {
                    Id = Guid.NewGuid(),
                    RoomId = roomId,
                    DisplayName = "Host",
                    NormalizedDisplayName = "HOST",
                    Role = (int)RoomPlayerRole.Host,
                    JoinedAtUtc = DateTime.UtcNow
                },
                new RoomPlayerEntity
                {
                    Id = Guid.NewGuid(),
                    RoomId = roomId,
                    DisplayName = "Alice",
                    NormalizedDisplayName = "ALICE",
                    Role = (int)RoomPlayerRole.Player,
                    JoinedAtUtc = DateTime.UtcNow.AddSeconds(1)
                }
            ]
        });
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        // Act
        var result = await repository.GetByRoomCodeAsync(new RoomCode("AB12CD"), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("AB12CD", result!.RoomCode.Value);
        Assert.Equal(RoomStatus.WaitingForPlayers, result.Status);
        Assert.Equal(2, result.Players.Count);
        Assert.Contains(result.Players, player => player.DisplayName == "Host" && player.Role == RoomPlayerRole.Host);
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
        var roomId = Guid.NewGuid();
        dbContext.Rooms.Add(new RoomEntity
        {
            Id = roomId,
            Code = "AB12CD",
            Status = 999,
            CreatedAtUtc = DateTime.UtcNow,
            Players =
            [
                new RoomPlayerEntity
                {
                    Id = Guid.NewGuid(),
                    RoomId = roomId,
                    DisplayName = "Host",
                    NormalizedDisplayName = "HOST",
                    Role = (int)RoomPlayerRole.Host,
                    JoinedAtUtc = DateTime.UtcNow
                }
            ]
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
