using BOTC.Application.Features.Rooms.JoinRoom;
using BOTC.Application.Features.Rooms.LeaveRoom;
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
        var saved = await ((IRoomJoinRepository)repository).TrySaveAsync(loaded, CancellationToken.None);

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
        var firstSave = await ((IRoomJoinRepository)repository).TrySaveAsync(first, CancellationToken.None);
        Assert.True(firstSave);

        second!.JoinPlayer("ALICE", DateTime.UtcNow.AddSeconds(2));
        var secondSave = await ((IRoomJoinRepository)repository).TrySaveAsync(second, CancellationToken.None);

        // Assert
        Assert.False(secondSave);
    }

    [Fact]
    public async Task TrySaveAsync_WhenRoomNoLongerExists_ThrowsRoomJoinSaveRoomMissingException()
    {
        // Arrange
        var room = CreateRoom("AB12CD", "Host");

        // Act
        var act = async () => await ((IRoomJoinRepository)repository).TrySaveAsync(room, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<RoomJoinSaveRoomMissingException>(act);
    }

    [Fact]
    public async Task LeaveRoomTrySaveAsync_WhenHostLeaves_PersistsTransferredHostAndRemovesLeavingPlayer()
    {
        var room = CreateRoom("AB12CD", "Host");
        var aliceJoinedAtUtc = DateTime.UtcNow.AddSeconds(1);
        var bobJoinedAtUtc = DateTime.UtcNow.AddSeconds(2);
        var alice = room.JoinPlayer("Alice", aliceJoinedAtUtc);
        var bob = room.JoinPlayer("Bob", bobJoinedAtUtc);
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
        Assert.Contains(persisted.Players, player => player.Id == alice.Id.Value && player.Role == (int)RoomPlayerRole.Host);
        Assert.Contains(persisted.Players, player => player.Id == bob.Id.Value && player.Role == (int)RoomPlayerRole.Player);
    }

    [Fact]
    public async Task LeaveRoomTryDeleteAsync_WhenLastPlayerLeaves_RemovesRoom()
    {
        var room = CreateRoom("AB12CD", "Host");
        await repository.TryAddAsync(room, CancellationToken.None);

        var deleted = await ((IRoomLeaveRepository)repository).TryDeleteAsync(room.Id, CancellationToken.None);

        Assert.True(deleted);
        Assert.False(await dbContext.Rooms.AnyAsync(existingRoom => existingRoom.Id == room.Id.Value));
        Assert.False(await dbContext.RoomPlayers.AnyAsync(player => player.RoomId == room.Id.Value));
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
