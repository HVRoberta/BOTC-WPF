using BOTC.Application.Abstractions.Persistence;
using BOTC.Domain.Rooms;
using BOTC.Domain.Users;
using BOTC.Infrastructure.Persistence;
using BOTC.Infrastructure.Persistence.Entities;
using BOTC.Infrastructure.Persistence.Repositories;
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

    // -------------------------------------------------------------------------
    // Constructor guard clauses
    // -------------------------------------------------------------------------

    [Fact]
    public void Constructor_WhenDbContextIsNull_ThrowsArgumentNullException()
    {
        Action act = () => _ = new RoomRepository(null!);

        var exception = Assert.Throws<ArgumentNullException>(act);
        Assert.Equal("dbContext", exception.ParamName);
    }

    // -------------------------------------------------------------------------
    // TryAddAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task TryAddAsync_WhenRoomCodeIsUnique_ReturnsTrueAndPersistsRoomWithHostPlayer()
    {
        var (room, _) = await SetupRoomAsync("AB12CD");

        var result = await repository.TryAddAsync(room, CancellationToken.None);

        Assert.True(result);
        var persisted = await dbContext.Rooms.Include(r => r.Players).SingleAsync(r => r.Id == room.Id.Value);
        Assert.Equal("AB12CD", persisted.Code);
        Assert.Equal((int)RoomStatus.WaitingForPlayers, persisted.Status);
        Assert.Equal(room.CreatedAtUtc, persisted.CreatedAtUtc);
        Assert.Single(persisted.Players);
        Assert.Equal((int)PlayerRole.Host, persisted.Players.Single().Role);
    }

    [Fact]
    public async Task TryAddAsync_WhenRoomCodeAlreadyExists_ReturnsFalse()
    {
        var (firstRoom, _) = await SetupRoomAsync("AB12CD");
        var (duplicateCodeRoom, _) = await SetupRoomAsync("AB12CD");

        await repository.TryAddAsync(firstRoom, CancellationToken.None);

        var result = await repository.TryAddAsync(duplicateCodeRoom, CancellationToken.None);

        Assert.False(result);
    }

    // -------------------------------------------------------------------------
    // GetByCodeAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetByCodeAsync_WhenRoomExists_ReturnsRoomWithAllPlayers()
    {
        var (room, _) = await SetupRoomAsync("AB12CD");
        var aliceUserId = await SeedUserAsync();
        room.JoinPlayer(aliceUserId, DateTime.UtcNow.AddSeconds(1));
        await repository.TryAddAsync(room, CancellationToken.None);

        var result = await repository.GetByCodeAsync(new RoomCode("AB12CD"), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(room.Id, result!.Id);
        Assert.Equal(2, result.Players.Count);
        Assert.Contains(result.Players, player => player.Role == PlayerRole.Host);
        Assert.Contains(result.Players, player => player.UserId == aliceUserId && player.Role == PlayerRole.Player);
    }

    [Fact]
    public async Task GetByCodeAsync_WhenRoomDoesNotExist_ReturnsNull()
    {
        var result = await repository.GetByCodeAsync(new RoomCode("ZZ9999"), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByCodeAsync_WhenRoomReturned_DoesNotLeaveTrackedEntities()
    {
        var (room, _) = await SetupRoomAsync("AB12CD");
        await repository.TryAddAsync(room, CancellationToken.None);

        _ = await repository.GetByCodeAsync(new RoomCode("AB12CD"), CancellationToken.None);

        Assert.Empty(dbContext.ChangeTracker.Entries());
    }

    // -------------------------------------------------------------------------
    // TrySaveAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task TrySaveAsync_WhenRoomExists_PersistsNewlyJoinedPlayer()
    {
        var (room, _) = await SetupRoomAsync("AB12CD");
        await repository.TryAddAsync(room, CancellationToken.None);

        var loaded = await repository.GetByCodeAsync(new RoomCode("AB12CD"), CancellationToken.None);
        Assert.NotNull(loaded);
        var aliceUserId = await SeedUserAsync();
        loaded!.JoinPlayer(aliceUserId, DateTime.UtcNow.AddSeconds(1));

        var saved = await repository.TrySaveAsync(loaded, CancellationToken.None);

        Assert.True(saved);
        var persistedPlayers = await dbContext.Players
            .Where(player => player.RoomId == loaded.Id.Value)
            .ToListAsync();
        Assert.Equal(2, persistedPlayers.Count);
        Assert.Contains(persistedPlayers, player => player.UserId == aliceUserId.Value);
    }

    [Fact]
    public async Task TrySaveAsync_WhenPlayerReadinessChanges_PersistsUpdatedReadinessState()
    {
        var (room, _) = await SetupRoomAsync("AB12CD");
        var aliceUserId = await SeedUserAsync();
        var alice = room.JoinPlayer(aliceUserId, DateTime.UtcNow.AddSeconds(1));
        await repository.TryAddAsync(room, CancellationToken.None);

        var loaded = await repository.GetByCodeAsync(new RoomCode("AB12CD"), CancellationToken.None);
        Assert.NotNull(loaded);
        loaded!.SetPlayerReady(alice.Id, true);

        var saved = await repository.TrySaveAsync(loaded, CancellationToken.None);

        Assert.True(saved);
        var persistedPlayer = await dbContext.Players
            .SingleAsync(player => player.Id == alice.Id.Value);
        Assert.True(persistedPlayer.IsReady);
    }

    [Fact]
    public async Task TrySaveAsync_WhenGameStarts_PersistsInProgressStatus()
    {
        var (room, _) = await SetupRoomAsync("AB12CD");
        var aliceUserId = await SeedUserAsync();
        var alice = room.JoinPlayer(aliceUserId, DateTime.UtcNow.AddSeconds(1));
        await repository.TryAddAsync(room, CancellationToken.None);

        var loaded = await repository.GetByCodeAsync(new RoomCode("AB12CD"), CancellationToken.None);
        Assert.NotNull(loaded);
        loaded!.SetPlayerReady(alice.Id, true);
        loaded.StartGame(loaded.HostPlayerId);

        var saved = await repository.TrySaveAsync(loaded, CancellationToken.None);

        Assert.True(saved);
        var persistedRoom = await dbContext.Rooms.SingleAsync(r => r.Id == room.Id.Value);
        Assert.Equal((int)RoomStatus.InProgress, persistedRoom.Status);
    }

    [Fact]
    public async Task TrySaveAsync_WhenConcurrentSnapshotsSaveTheSameRoomCode_SecondSaveSucceedsWithLatestState()
    {
        // Two concurrent requests load the same room. Each adds a *different* user.
        // The second save wins and the DB reflects the second snapshot's players.
        var (room, _) = await SetupRoomAsync("AB12CD");
        await repository.TryAddAsync(room, CancellationToken.None);

        var first = await repository.GetByCodeAsync(new RoomCode("AB12CD"), CancellationToken.None);
        var second = await repository.GetByCodeAsync(new RoomCode("AB12CD"), CancellationToken.None);
        Assert.NotNull(first);
        Assert.NotNull(second);

        var aliceId = await SeedUserAsync();
        var bobId   = await SeedUserAsync();
        first!.JoinPlayer(aliceId, DateTime.UtcNow.AddSeconds(1));
        second!.JoinPlayer(bobId,  DateTime.UtcNow.AddSeconds(1));

        var firstSave  = await repository.TrySaveAsync(first, CancellationToken.None);
        var secondSave = await repository.TrySaveAsync(second, CancellationToken.None);

        // Both saves succeed; last-write-wins via synchronizer
        Assert.True(firstSave);
        Assert.True(secondSave);

        // The final DB state reflects the second snapshot (Alice was overwritten by Bob)
        var finalPlayers = await dbContext.Players
            .Where(p => p.RoomId == room.Id.Value)
            .ToListAsync();
        Assert.Equal(2, finalPlayers.Count); // host + bob
        Assert.Contains(finalPlayers, p => p.UserId == bobId.Value);
        Assert.DoesNotContain(finalPlayers, p => p.UserId == aliceId.Value);
    }

    [Fact]
    public async Task TrySaveAsync_WhenRoomNoLongerExists_ThrowsInvalidOperationException()
    {
        var (room, _) = await SetupRoomAsync("AB12CD");

        var act = async () => await repository.TrySaveAsync(room, CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(act);
    }

    // -------------------------------------------------------------------------
    // TryDeleteAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task TryDeleteAsync_WhenRoomExists_ReturnsTrueAndRemovesRoomAndPlayers()
    {
        var (room, _) = await SetupRoomAsync("AB12CD");
        await repository.TryAddAsync(room, CancellationToken.None);

        var deleted = await repository.TryDeleteAsync(room.Id, CancellationToken.None);

        Assert.True(deleted);
        Assert.False(await dbContext.Rooms.AnyAsync(r => r.Id == room.Id.Value));
        Assert.False(await dbContext.Players.AnyAsync(player => player.RoomId == room.Id.Value));
    }

    [Fact]
    public async Task TryDeleteAsync_WhenRoomDoesNotExist_ReturnsFalse()
    {
        var nonExistentRoomId = RoomId.New();

        var deleted = await repository.TryDeleteAsync(nonExistentRoomId, CancellationToken.None);

        Assert.False(deleted);
    }

    // -------------------------------------------------------------------------
    // TrySaveAsync — host transfer scenario
    // -------------------------------------------------------------------------

    [Fact]
    public async Task TrySaveAsync_WhenHostLeaves_PersistsTransferredHostAndRemovesLeavingPlayer()
    {
        var (room, _) = await SetupRoomAsync("AB12CD");
        var aliceUserId = await SeedUserAsync();
        var bobUserId = await SeedUserAsync();
        var alice = room.JoinPlayer(aliceUserId, DateTime.UtcNow.AddSeconds(1));
        var bob = room.JoinPlayer(bobUserId, DateTime.UtcNow.AddSeconds(2));
        await repository.TryAddAsync(room, CancellationToken.None);

        var loaded = await repository.GetByCodeAsync(new RoomCode("AB12CD"), CancellationToken.None);
        Assert.NotNull(loaded);
        var originalHostId = loaded!.HostPlayerId;
        loaded.LeavePlayer(originalHostId);

        var saved = await repository.TrySaveAsync(loaded, CancellationToken.None);

        Assert.True(saved);
        var persisted = await dbContext.Rooms
            .Include(r => r.Players)
            .SingleAsync(r => r.Code == "AB12CD");
        Assert.DoesNotContain(persisted.Players, player => player.Id == originalHostId.Value);
        Assert.Contains(persisted.Players, player => player.Id == alice.Id.Value && player.Role == (int)PlayerRole.Host);
        Assert.Contains(persisted.Players, player => player.Id == bob.Id.Value && player.Role == (int)PlayerRole.Player);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private async Task<(Room Room, UserId HostUserId)> SetupRoomAsync(string code)
    {
        var hostUserId = await SeedUserAsync();
        var room = Room.Create(RoomId.New(), new RoomCode(code), "Test Room", hostUserId, DateTime.UtcNow);
        return (room, hostUserId);
    }

    private async Task<UserId> SeedUserAsync()
    {
        var userId = UserId.New();
        var username = $"user_{userId.Value:N}";
        dbContext.Users.Add(new UserEntity
        {
            Id = userId.Value,
            Username = username,
            NickName = username,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();
        return userId;
    }

    public void Dispose()
    {
        dbContext.Dispose();
        connection.Dispose();
    }
}
