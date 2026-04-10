using BOTC.Domain.Rooms;
using BOTC.Domain.Rooms.Players;
using BOTC.Infrastructure.Persistence;
using BOTC.Infrastructure.Persistence.Rooms;
using BOTC.Infrastructure.Persistence.User;
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
        var hostUserId = Guid.NewGuid();
        var aliceUserId = Guid.NewGuid();

        dbContext.Users.AddRange(
            new UserEntity { Id = hostUserId, Username = "host", NickName = "Host", CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow },
            new UserEntity { Id = aliceUserId, Username = "alice", NickName = "Alice", CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow });

        dbContext.Rooms.Add(new RoomEntity
        {
            Id = roomId,
            Code = "AB12CD",
            Name = "Test Room",
            Status = (int)RoomStatus.WaitingForPlayers,
            CreatedAtUtc = DateTime.UtcNow,
            Players =
            [
                new PlayerEntity
                {
                    Id = Guid.NewGuid(),
                    RoomId = roomId,
                    UserId = hostUserId,
                    Role = (int)PlayerRole.Host,
                    JoinedAtUtc = DateTime.UtcNow
                },
                new PlayerEntity
                {
                    Id = Guid.NewGuid(),
                    RoomId = roomId,
                    UserId = aliceUserId,
                    Role = (int)PlayerRole.Player,
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
        Assert.Equal("Host", result.Players[0].Name);
        Assert.Equal(PlayerRole.Host, result.Players[0].Role);
        Assert.Equal("Alice", result.Players[1].Name);
        Assert.Equal(PlayerRole.Player, result.Players[1].Role);
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
        var hostUserId = Guid.NewGuid();

        dbContext.Users.Add(new UserEntity
        {
            Id = hostUserId,
            Username = "host",
            NickName = "Host",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });

        dbContext.Rooms.Add(new RoomEntity
        {
            Id = roomId,
            Code = "AB12CD",
            Name = "Test Room",
            Status = 999,
            CreatedAtUtc = DateTime.UtcNow,
            Players =
            [
                new PlayerEntity
                {
                    Id = Guid.NewGuid(),
                    RoomId = roomId,
                    UserId = hostUserId,
                    Role = (int)PlayerRole.Host,
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
