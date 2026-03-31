using BOTC.Domain.Rooms;

namespace BOTC.Domain.Tests.Rooms;

public sealed class RoomTests
{
    [Fact]
    public void Create_WhenInputIsValid_SetsAllPropertiesAndWaitingForPlayersStatus()
    {
        // Arrange
        var id = new RoomId(Guid.NewGuid());
        var code = new RoomCode("AB12CD");
        const string hostDisplayName = "Host";
        var createdAtUtc = new DateTime(2026, 3, 17, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var room = Room.Create(id, code, hostDisplayName, createdAtUtc);

        // Assert
        Assert.Equal(id, room.Id);
        Assert.Equal(code, room.Code);
        Assert.Equal(hostDisplayName, room.HostDisplayName);
        Assert.Equal(createdAtUtc, room.CreatedAtUtc);
        Assert.Equal(RoomStatus.WaitingForPlayers, room.Status);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WhenHostDisplayNameIsNullEmptyOrWhitespace_ThrowsArgumentException(string? hostDisplayName)
    {
        // Arrange
        var id = new RoomId(Guid.NewGuid());
        var code = new RoomCode("AB12CD");
        var createdAtUtc = new DateTime(2026, 3, 17, 12, 0, 0, DateTimeKind.Utc);

        // Act
        Action act = () =>
        {
            _ = Room.Create(id, code, hostDisplayName!, createdAtUtc);
        };

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Create_WhenHostDisplayNameExceedsMaximumLength_ThrowsArgumentException()
    {
        // Arrange
        var id = new RoomId(Guid.NewGuid());
        var code = new RoomCode("AB12CD");
        var tooLongHostDisplayName = new string('A', 51);
        var createdAtUtc = new DateTime(2026, 3, 17, 12, 0, 0, DateTimeKind.Utc);

        // Act
        Action act = () =>
        {
            _ = Room.Create(id, code, tooLongHostDisplayName, createdAtUtc);
        };

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Create_WhenHostDisplayNameHasSurroundingWhitespace_TrimsHostDisplayName()
    {
        // Arrange
        var id = new RoomId(Guid.NewGuid());
        var code = new RoomCode("AB12CD");
        const string hostDisplayName = "  Host Name  ";
        var createdAtUtc = new DateTime(2026, 3, 17, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var room = Room.Create(id, code, hostDisplayName, createdAtUtc);

        // Assert
        Assert.Equal("Host Name", room.HostDisplayName);
    }

    [Fact]
    public void Create_WhenCreatedAtIsLocal_ConvertsCreatedAtToUtc()
    {
        // Arrange
        var id = new RoomId(Guid.NewGuid());
        var code = new RoomCode("AB12CD");
        const string hostDisplayName = "Host";
        var localDateTime = new DateTime(2026, 3, 17, 13, 0, 0, DateTimeKind.Local);

        // Act
        var room = Room.Create(id, code, hostDisplayName, localDateTime);

        // Assert
        Assert.Equal(DateTimeKind.Utc, room.CreatedAtUtc.Kind);
        Assert.Equal(localDateTime.ToUniversalTime(), room.CreatedAtUtc);
    }

    [Fact]
    public void Create_WhenCreatedAtKindIsUnspecified_ThrowsArgumentException()
    {
        // Arrange
        var id = new RoomId(Guid.NewGuid());
        var code = new RoomCode("AB12CD");
        const string hostDisplayName = "Host";
        var unspecifiedDateTime = new DateTime(2026, 3, 17, 13, 0, 0, DateTimeKind.Unspecified);

        // Act
        Action act = () =>
        {
            _ = Room.Create(id, code, hostDisplayName, unspecifiedDateTime);
        };

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void RenameHost_WhenInputIsValid_UpdatesHostDisplayName()
    {
        // Arrange
        var room = Room.Create(
            new RoomId(Guid.NewGuid()),
            new RoomCode("AB12CD"),
            "Original Host",
            new DateTime(2026, 3, 17, 12, 0, 0, DateTimeKind.Utc));

        // Act
        room.RenameHost("New Host");

        // Assert
        Assert.Equal("New Host", room.HostDisplayName);
    }

    [Fact]
    public void RenameHost_WhenInputHasSurroundingWhitespace_TrimsHostDisplayName()
    {
        // Arrange
        var room = Room.Create(
            new RoomId(Guid.NewGuid()),
            new RoomCode("AB12CD"),
            "Original Host",
            new DateTime(2026, 3, 17, 12, 0, 0, DateTimeKind.Utc));

        // Act
        room.RenameHost("  New Host  ");

        // Assert
        Assert.Equal("New Host", room.HostDisplayName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RenameHost_WhenHostDisplayNameIsInvalid_ThrowsArgumentException(string? hostDisplayName)
    {
        // Arrange
        var room = Room.Create(
            new RoomId(Guid.NewGuid()),
            new RoomCode("AB12CD"),
            "Original Host",
            new DateTime(2026, 3, 17, 12, 0, 0, DateTimeKind.Utc));

        // Act
        Action act = () =>
        {
            room.RenameHost(hostDisplayName!);
        };

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Players_WhenExposed_CannotBeMutatedThroughCollectionInterface()
    {
        // Arrange
        var room = Room.Create(
            new RoomId(Guid.NewGuid()),
            new RoomCode("AB12CD"),
            "Host",
            new DateTime(2026, 3, 17, 12, 0, 0, DateTimeKind.Utc));

        // Act
        var players = room.Players;

        // Assert
        Assert.IsNotType<List<RoomPlayer>>(players);

        var collection = Assert.IsAssignableFrom<ICollection<RoomPlayer>>(players);
        Assert.True(collection.IsReadOnly);
        Assert.Throws<NotSupportedException>(() =>
            collection.Add(RoomPlayer.Create(RoomPlayerId.New(), "Alice", RoomPlayerRole.Player, DateTime.UtcNow)));
    }

    [Fact]
    public void LeavePlayer_WhenNonHostLeaves_RemovesPlayerAndKeepsExistingHost()
    {
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Host", DateTime.UtcNow);
        var alice = room.JoinPlayer("Alice", DateTime.UtcNow.AddSeconds(1));
        var bob = room.JoinPlayer("Bob", DateTime.UtcNow.AddSeconds(2));
        var originalHostId = room.HostPlayerId;

        var outcome = room.LeavePlayer(alice.Id);

        Assert.False(outcome.RoomWasRemoved);
        Assert.Null(outcome.NewHostPlayerId);
        Assert.Equal(originalHostId, room.HostPlayerId);
        Assert.DoesNotContain(room.Players, player => player.Id == alice.Id);
        Assert.Contains(room.Players, player => player.Id == bob.Id);
    }

    [Fact]
    public void LeavePlayer_WhenHostLeaves_TransfersHostToLongestPresentRemainingPlayer()
    {
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Host", DateTime.UtcNow);
        var originalHostId = room.HostPlayerId;
        var alice = room.JoinPlayer("Alice", DateTime.UtcNow.AddSeconds(1));
        var bob = room.JoinPlayer("Bob", DateTime.UtcNow.AddSeconds(2));

        var outcome = room.LeavePlayer(originalHostId);

        Assert.False(outcome.RoomWasRemoved);
        Assert.Equal(alice.Id, outcome.NewHostPlayerId);
        Assert.Equal(alice.Id, room.HostPlayerId);
        Assert.DoesNotContain(room.Players, player => player.Id == originalHostId);
        Assert.Contains(room.Players, player => player.Id == bob.Id && player.Role == RoomPlayerRole.Player);
    }

    [Fact]
    public void LeavePlayer_WhenLastPlayerLeaves_ReturnsRoomRemovedOutcome()
    {
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Host", DateTime.UtcNow);
        var hostPlayerId = room.HostPlayerId;

        var outcome = room.LeavePlayer(hostPlayerId);

        Assert.True(outcome.RoomWasRemoved);
        Assert.Null(outcome.NewHostPlayerId);
        Assert.Empty(room.Players);
    }

    [Fact]
    public void LeavePlayer_WhenPlayerDoesNotExist_ThrowsRoomLeavePlayerNotFoundException()
    {
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Host", DateTime.UtcNow);
        var missingPlayerId = RoomPlayerId.New();

        var exception = Assert.Throws<RoomLeavePlayerNotFoundException>(() => room.LeavePlayer(missingPlayerId));

        Assert.Equal(missingPlayerId, exception.PlayerId);
    }

    [Fact]
    public void SetPlayerReady_WhenPlayerExists_UpdatesPlayerReadiness()
    {
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Host", DateTime.UtcNow);
        var alice = room.JoinPlayer("Alice", DateTime.UtcNow.AddSeconds(1));

        room.SetPlayerReady(alice.Id, true);

        Assert.Contains(room.Players, player => player.Id == alice.Id && player.IsReady);
    }

    [Fact]
    public void SetPlayerReady_WhenRoomIsNotWaitingForPlayers_ThrowsRoomSetPlayerReadyNotAllowedException()
    {
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Host", DateTime.UtcNow);
        var alice = room.JoinPlayer("Alice", DateTime.UtcNow.AddSeconds(1));
        room.SetPlayerReady(alice.Id, true);
        var startOutcome = room.StartGame(room.HostPlayerId);
        Assert.True(startOutcome.IsStarted);

        var act = () => room.SetPlayerReady(alice.Id, false);

        Assert.Throws<RoomSetPlayerReadyNotAllowedException>(act);
    }

    [Fact]
    public void StartGame_WhenOnlyHostIsInRoom_ReturnsNotEnoughPlayersReason()
    {
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Host", DateTime.UtcNow);

        var outcome = room.StartGame(room.HostPlayerId);

        Assert.False(outcome.IsStarted);
        Assert.Equal(RoomStartGameBlockedReason.NotEnoughPlayers, outcome.BlockedReason);
        Assert.Equal(RoomStatus.WaitingForPlayers, room.Status);
    }

    [Fact]
    public void StartGame_WhenNonHostPlayerIsNotReady_ReturnsNonHostPlayersNotReadyReason()
    {
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Host", DateTime.UtcNow);
        room.JoinPlayer("Alice", DateTime.UtcNow.AddSeconds(1));

        var outcome = room.StartGame(room.HostPlayerId);

        Assert.False(outcome.IsStarted);
        Assert.Equal(RoomStartGameBlockedReason.NonHostPlayersNotReady, outcome.BlockedReason);
    }

    [Fact]
    public void StartGame_WhenAllNonHostPlayersAreReady_StartsGame()
    {
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Host", DateTime.UtcNow);
        var alice = room.JoinPlayer("Alice", DateTime.UtcNow.AddSeconds(1));
        room.SetPlayerReady(alice.Id, true);

        var outcome = room.StartGame(room.HostPlayerId);

        Assert.True(outcome.IsStarted);
        Assert.Null(outcome.BlockedReason);
        Assert.Equal(RoomStatus.InProgress, room.Status);
    }
}
