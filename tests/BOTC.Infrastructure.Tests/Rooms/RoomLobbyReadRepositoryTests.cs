using BOTC.Domain.Rooms;
using BOTC.Infrastructure.Persistence;
using BOTC.Infrastructure.Persistence.Entities;
using BOTC.Infrastructure.Rooms.Queries;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BOTC.Infrastructure.Tests.Rooms;

public sealed class RoomLobbyQueryServiceTests : IDisposable
{
    private readonly SqliteConnection connection;
    private readonly BotcDbContext dbContext;
    private readonly RoomLobbyQueryService queryService;

    public RoomLobbyQueryServiceTests()
    {
        connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<BotcDbContext>()
            .UseSqlite(connection)
            .Options;

        dbContext = new BotcDbContext(options);
        dbContext.Database.EnsureCreated();

        queryService = new RoomLobbyQueryService(dbContext);
    }

    // -------------------------------------------------------------------------
    // Constructor guard clauses
    // -------------------------------------------------------------------------

    [Fact]
    public void Constructor_WhenDbContextIsNull_ThrowsArgumentNullException()
    {
        Action act = () => _ = new RoomLobbyQueryService(null!);

        var exception = Assert.Throws<ArgumentNullException>(act);
        Assert.Equal("dbContext", exception.ParamName);
    }

    // -------------------------------------------------------------------------
    // GetByRoomCodeAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetByRoomCodeAsync_WhenRoomDoesNotExist_ReturnsNull()
    {
        var result = await queryService.GetByRoomCodeAsync(new RoomCode("AB12CD"), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByRoomCodeAsync_WhenRoomExists_ReturnsMappedLobbyState()
    {
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
                new PlayerEntity { Id = Guid.NewGuid(), RoomId = roomId, UserId = hostUserId, Role = (int)PlayerRole.Host, JoinedAtUtc = DateTime.UtcNow },
                new PlayerEntity { Id = Guid.NewGuid(), RoomId = roomId, UserId = aliceUserId, Role = (int)PlayerRole.Player, JoinedAtUtc = DateTime.UtcNow.AddSeconds(1) }
            ]
        });
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        var result = await queryService.GetByRoomCodeAsync(new RoomCode("AB12CD"), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("AB12CD", result!.RoomCode.Value);
        Assert.Equal(RoomStatus.WaitingForPlayers, result.Status);
        Assert.Equal(2, result.Players.Count);
        Assert.Equal("Host", result.Players[0].Name);
        Assert.Equal(PlayerRole.Host, result.Players[0].Role);
        Assert.Equal("Alice", result.Players[1].Name);
        Assert.Equal(PlayerRole.Player, result.Players[1].Role);
    }

    [Fact]
    public async Task GetByRoomCodeAsync_WhenHostOrderedFirst_ThenByJoinTime()
    {
        var roomId = Guid.NewGuid();
        var hostUserId = Guid.NewGuid();
        var firstJoinerUserId = Guid.NewGuid();
        var secondJoinerUserId = Guid.NewGuid();
        var baseTime = DateTime.UtcNow;

        dbContext.Users.AddRange(
            new UserEntity { Id = hostUserId, Username = "host", NickName = "Host", CreatedAtUtc = baseTime, UpdatedAtUtc = baseTime },
            new UserEntity { Id = firstJoinerUserId, Username = "bob", NickName = "Bob", CreatedAtUtc = baseTime, UpdatedAtUtc = baseTime },
            new UserEntity { Id = secondJoinerUserId, Username = "carol", NickName = "Carol", CreatedAtUtc = baseTime, UpdatedAtUtc = baseTime });

        dbContext.Rooms.Add(new RoomEntity
        {
            Id = roomId,
            Code = "AB12CD",
            Name = "Test Room",
            Status = (int)RoomStatus.WaitingForPlayers,
            CreatedAtUtc = baseTime,
            Players =
            [
                // Inserted out-of-order on purpose to verify sorting
                new PlayerEntity { Id = Guid.NewGuid(), RoomId = roomId, UserId = secondJoinerUserId, Role = (int)PlayerRole.Player, JoinedAtUtc = baseTime.AddSeconds(2) },
                new PlayerEntity { Id = Guid.NewGuid(), RoomId = roomId, UserId = hostUserId,         Role = (int)PlayerRole.Host,   JoinedAtUtc = baseTime },
                new PlayerEntity { Id = Guid.NewGuid(), RoomId = roomId, UserId = firstJoinerUserId,  Role = (int)PlayerRole.Player, JoinedAtUtc = baseTime.AddSeconds(1) }
            ]
        });
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        var result = await queryService.GetByRoomCodeAsync(new RoomCode("AB12CD"), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(3, result!.Players.Count);
        Assert.Equal("Host",  result.Players[0].Name);   // host always first
        Assert.Equal("Bob",   result.Players[1].Name);   // joined second (earlier)
        Assert.Equal("Carol", result.Players[2].Name);   // joined third (later)
    }

    [Fact]
    public async Task GetByRoomCodeAsync_WhenPlayerIsReady_ReturnsIsReadyTrue()
    {
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
                new PlayerEntity { Id = Guid.NewGuid(), RoomId = roomId, UserId = hostUserId,  Role = (int)PlayerRole.Host,   JoinedAtUtc = DateTime.UtcNow,                IsReady = false },
                new PlayerEntity { Id = Guid.NewGuid(), RoomId = roomId, UserId = aliceUserId, Role = (int)PlayerRole.Player, JoinedAtUtc = DateTime.UtcNow.AddSeconds(1),  IsReady = true }
            ]
        });
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        var result = await queryService.GetByRoomCodeAsync(new RoomCode("AB12CD"), CancellationToken.None);

        Assert.NotNull(result);
        Assert.False(result!.Players[0].IsReady);   // host
        Assert.True(result.Players[1].IsReady);     // alice
    }

    [Fact]
    public async Task GetByRoomCodeAsync_WhenRoomReturned_DoesNotLeaveTrackedEntities()
    {
        var roomId = Guid.NewGuid();
        var hostUserId = Guid.NewGuid();

        dbContext.Users.Add(new UserEntity { Id = hostUserId, Username = "host", NickName = "Host", CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow });
        dbContext.Rooms.Add(new RoomEntity
        {
            Id = roomId, Code = "AB12CD", Name = "Test Room", Status = (int)RoomStatus.WaitingForPlayers, CreatedAtUtc = DateTime.UtcNow,
            Players = [ new PlayerEntity { Id = Guid.NewGuid(), RoomId = roomId, UserId = hostUserId, Role = (int)PlayerRole.Host, JoinedAtUtc = DateTime.UtcNow } ]
        });
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        _ = await queryService.GetByRoomCodeAsync(new RoomCode("AB12CD"), CancellationToken.None);

        Assert.Empty(dbContext.ChangeTracker.Entries());
    }

    [Fact]
    public async Task GetByRoomCodeAsync_WhenPersistedStatusIsInvalid_ThrowsInvalidOperationException()
    {
        var roomId = Guid.NewGuid();
        var hostUserId = Guid.NewGuid();

        dbContext.Users.Add(new UserEntity { Id = hostUserId, Username = "host", NickName = "Host", CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow });
        dbContext.Rooms.Add(new RoomEntity
        {
            Id = roomId, Code = "AB12CD", Name = "Test Room", Status = 999, CreatedAtUtc = DateTime.UtcNow,
            Players = [ new PlayerEntity { Id = Guid.NewGuid(), RoomId = roomId, UserId = hostUserId, Role = (int)PlayerRole.Host, JoinedAtUtc = DateTime.UtcNow } ]
        });
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        var act = async () => await queryService.GetByRoomCodeAsync(new RoomCode("AB12CD"), CancellationToken.None);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(act);
        Assert.Contains("invalid persisted status", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetByRoomCodeAsync_WhenPersistedPlayerRoleIsInvalid_ThrowsInvalidOperationException()
    {
        var roomId = Guid.NewGuid();
        var hostUserId = Guid.NewGuid();

        dbContext.Users.Add(new UserEntity { Id = hostUserId, Username = "host", NickName = "Host", CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow });
        dbContext.Rooms.Add(new RoomEntity
        {
            Id = roomId, Code = "AB12CD", Name = "Test Room", Status = (int)RoomStatus.WaitingForPlayers, CreatedAtUtc = DateTime.UtcNow,
            Players = [ new PlayerEntity { Id = Guid.NewGuid(), RoomId = roomId, UserId = hostUserId, Role = 999, JoinedAtUtc = DateTime.UtcNow } ]
        });
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        var act = async () => await queryService.GetByRoomCodeAsync(new RoomCode("AB12CD"), CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(act);
    }

    public void Dispose()
    {
        dbContext.Dispose();
        connection.Dispose();
    }
}
