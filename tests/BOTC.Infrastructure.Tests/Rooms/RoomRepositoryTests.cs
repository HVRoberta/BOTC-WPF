using BOTC.Application.Features.Rooms.JoinRoom;
using BOTC.Application.Features.Rooms.LeaveRoom;
using BOTC.Domain.Rooms;
using BOTC.Domain.Rooms.Players;
using BOTC.Domain.Users;
using BOTC.Infrastructure.Persistence;
using BOTC.Infrastructure.Persistence.User;
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

    [Fact]
    public async Task GetByCodeAsync_WhenRoomExists_ReturnsRoomWithPlayers()
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
    public async Task TrySaveAsync_WhenRoomExists_PersistsNewlyJoinedPlayer()
    {
        var (room, _) = await SetupRoomAsync("AB12CD");
        await repository.TryAddAsync(room, CancellationToken.None);

        var loaded = await repository.GetByCodeAsync(new RoomCode("AB12CD"), CancellationToken.None);
        Assert.NotNull(loaded);
        var aliceUserId = await SeedUserAsync();
        loaded!.JoinPlayer(aliceUserId, DateTime.UtcNow.AddSeconds(1));

        var saved = await ((IRoomJoinRepository)repository).TrySaveAsync(loaded, CancellationToken.None);

        Assert.True(saved);
        var persistedPlayers = await dbContext.RoomPlayers
            .Where(player => player.RoomId == loaded.Id.Value)
            .ToListAsync();
        Assert.Equal(2, persistedPlayers.Count);
        Assert.Contains(persistedPlayers, player => player.UserId == aliceUserId.Value);
    }

    [Fact]
    public async Task TrySaveAsync_WhenSameUserJoinsTwice_ReturnsFalse()
    {
        var (room, _) = await SetupRoomAsync("AB12CD");
        await repository.TryAddAsync(room, CancellationToken.None);

        var first = await repository.GetByCodeAsync(new RoomCode("AB12CD"), CancellationToken.None);
        var second = await repository.GetByCodeAsync(new RoomCode("AB12CD"), CancellationToken.None);
        Assert.NotNull(first);
        Assert.NotNull(second);

        var sharedUserId = await SeedUserAsync();
        first!.JoinPlayer(sharedUserId, DateTime.UtcNow.AddSeconds(1));
        var firstSave = await ((IRoomJoinRepository)repository).TrySaveAsync(first, CancellationToken.None);
        Assert.True(firstSave);

        // second snapshot doesn't know Alice is already there — simulate concurrent join with same UserId
        second!.JoinPlayer(sharedUserId, DateTime.UtcNow.AddSeconds(2));
        var secondSave = await ((IRoomJoinRepository)repository).TrySaveAsync(second, CancellationToken.None);

        Assert.False(secondSave);
    }

    [Fact]
    public async Task TrySaveAsync_WhenRoomNoLongerExists_ThrowsRoomJoinSaveRoomMissingException()
    {
        var (room, _) = await SetupRoomAsync("AB12CD");

        var act = async () => await ((IRoomJoinRepository)repository).TrySaveAsync(room, CancellationToken.None);

        await Assert.ThrowsAsync<RoomJoinSaveRoomMissingException>(act);
    }

    [Fact]
    public async Task LeaveRoomTrySaveAsync_WhenHostLeaves_PersistsTransferredHostAndRemovesLeavingPlayer()
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

        var saved = await ((IRoomLeaveRepository)repository).TrySaveAsync(loaded, CancellationToken.None);

        Assert.True(saved);

        var persisted = await dbContext.Rooms
            .Include(existingRoom => existingRoom.Players)
            .SingleAsync(existingRoom => existingRoom.Code == "AB12CD");

        Assert.DoesNotContain(persisted.Players, player => player.Id == originalHostId.Value);
        Assert.Contains(persisted.Players, player => player.Id == alice.Id.Value && player.Role == (int)PlayerRole.Host);
        Assert.Contains(persisted.Players, player => player.Id == bob.Id.Value && player.Role == (int)PlayerRole.Player);
    }

    [Fact]
    public async Task LeaveRoomTryDeleteAsync_WhenLastPlayerLeaves_RemovesRoom()
    {
        var (room, _) = await SetupRoomAsync("AB12CD");
        await repository.TryAddAsync(room, CancellationToken.None);

        var deleted = await ((IRoomLeaveRepository)repository).TryDeleteAsync(room.Id, CancellationToken.None);

        Assert.True(deleted);
        Assert.False(await dbContext.Rooms.AnyAsync(existingRoom => existingRoom.Id == room.Id.Value));
        Assert.False(await dbContext.RoomPlayers.AnyAsync(player => player.RoomId == room.Id.Value));
    }

    /// <summary>Seeds a UserEntity and returns a Room created with that host UserId.</summary>
    private async Task<(Room Room, UserId HostUserId)> SetupRoomAsync(string code)
    {
        var hostUserId = await SeedUserAsync();
        var room = Room.Create(RoomId.New(), new RoomCode(code), "Test Room", hostUserId, DateTime.UtcNow);
        return (room, hostUserId);
    }

    /// <summary>Inserts a UserEntity with a unique username and returns its UserId.</summary>
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
