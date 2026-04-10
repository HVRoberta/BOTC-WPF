using BOTC.Domain.Rooms.Players;
using BOTC.Domain.Rooms;
using BOTC.Domain.Rooms.Exceptions;
using BOTC.Domain.Users;

namespace BOTC.Domain.Tests.Rooms;

public sealed class RoomTests
{
    [Fact]
    public void Create_WhenInputIsValid_SetsAllPropertiesAndWaitingForPlayersStatus()
    {
        // Arrange
        var id = new RoomId(Guid.NewGuid());
        var code = new RoomCode("AB12CD");
        const string roomName = "Test Room";
        var hostUserId = UserId.New();
        var createdAtUtc = new DateTime(2026, 3, 17, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var room = Room.Create(id, code, roomName, hostUserId, createdAtUtc);

        // Assert
        Assert.Equal(id, room.Id);
        Assert.Equal(code, room.Code);
        Assert.Equal(roomName, room.Name);
        Assert.Equal(createdAtUtc, room.CreatedAtUtc);
        Assert.Equal(RoomStatus.WaitingForPlayers, room.Status);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WhenRoomNameIsNullEmptyOrWhitespace_ThrowsArgumentException(string? roomName)
    {
        // Arrange
        var id = new RoomId(Guid.NewGuid());
        var code = new RoomCode("AB12CD");
        var createdAtUtc = new DateTime(2026, 3, 17, 12, 0, 0, DateTimeKind.Utc);

        // Act
        Action act = () =>
        {
            _ = Room.Create(id, code, roomName!, UserId.New(), createdAtUtc);
        };

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Create_WhenRoomNameHasSurroundingWhitespace_TrimsRoomName()
    {
        // Arrange
        var id = new RoomId(Guid.NewGuid());
        var code = new RoomCode("AB12CD");
        const string roomName = "  Test Room  ";
        var createdAtUtc = new DateTime(2026, 3, 17, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var room = Room.Create(id, code, roomName, UserId.New(), createdAtUtc);

        // Assert
        Assert.Equal("Test Room", room.Name);
    }

    [Fact]
    public void Create_WhenCreatedAtIsLocal_ConvertsCreatedAtToUtc()
    {
        // Arrange
        var id = new RoomId(Guid.NewGuid());
        var code = new RoomCode("AB12CD");
        const string roomName = "Test Room";
        var localDateTime = new DateTime(2026, 3, 17, 13, 0, 0, DateTimeKind.Local);

        // Act
        var room = Room.Create(id, code, roomName, UserId.New(), localDateTime);

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
        const string roomName = "Test Room";
        var unspecifiedDateTime = new DateTime(2026, 3, 17, 13, 0, 0, DateTimeKind.Unspecified);

        // Act
        Action act = () =>
        {
            _ = Room.Create(id, code, roomName, UserId.New(), unspecifiedDateTime);
        };

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Rename_WhenInputIsValid_UpdatesName()
    {
        // Arrange
        var room = Room.Create(
            new RoomId(Guid.NewGuid()),
            new RoomCode("AB12CD"),
            "Original Name",
            UserId.New(),
            new DateTime(2026, 3, 17, 12, 0, 0, DateTimeKind.Utc));

        // Act
        room.Rename("New Name");

        // Assert
        Assert.Equal("New Name", room.Name);
    }

    [Fact]
    public void Rename_WhenInputHasSurroundingWhitespace_TrimsName()
    {
        // Arrange
        var room = Room.Create(
            new RoomId(Guid.NewGuid()),
            new RoomCode("AB12CD"),
            "Original Name",
            UserId.New(),
            new DateTime(2026, 3, 17, 12, 0, 0, DateTimeKind.Utc));

        // Act
        room.Rename("  New Name  ");

        // Assert
        Assert.Equal("New Name", room.Name);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Rename_WhenNameIsInvalid_ThrowsArgumentException(string? name)
    {
        // Arrange
        var room = Room.Create(
            new RoomId(Guid.NewGuid()),
            new RoomCode("AB12CD"),
            "Original Name",
            UserId.New(),
            new DateTime(2026, 3, 17, 12, 0, 0, DateTimeKind.Utc));

        // Act
        Action act = () =>
        {
            room.Rename(name!);
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
            "Test Room",
            UserId.New(),
            new DateTime(2026, 3, 17, 12, 0, 0, DateTimeKind.Utc));

        // Act
        var players = room.Players;

        // Assert
        Assert.IsNotType<List<Player>>(players);

        var collection = Assert.IsAssignableFrom<ICollection<Player>>(players);
        Assert.True(collection.IsReadOnly);
        Assert.Throws<NotSupportedException>(() =>
            collection.Add(Player.Create(PlayerId.New(), UserId.New(), PlayerRole.Player, DateTime.UtcNow)));
    }

    [Fact]
    public void JoinPlayer_WhenInputIsValid_AddsPlayerAndReturnsNewPlayer()
    {
        // Arrange
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Test Room", UserId.New(), DateTime.UtcNow);
        var joinUserId = UserId.New();
        var joinedAt = DateTime.UtcNow.AddSeconds(1);

        // Act
        var joined = room.JoinPlayer(joinUserId, joinedAt);

        // Assert
        Assert.Equal(2, room.Players.Count);
        Assert.Equal(joinUserId, joined.UserId);
        Assert.Equal(PlayerRole.Player, joined.Role);
        Assert.False(joined.IsReady);
        Assert.Contains(room.Players, p => p.Id == joined.Id);
    }

    [Fact]
    public void JoinPlayer_WhenSameUserJoinsTwice_ThrowsRoomUserAlreadyJoinedException()
    {
        // Arrange
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Test Room", UserId.New(), DateTime.UtcNow);
        var userId = UserId.New();
        room.JoinPlayer(userId, DateTime.UtcNow.AddSeconds(1));

        // Act
        Action act = () => room.JoinPlayer(userId, DateTime.UtcNow.AddSeconds(2));

        // Assert
        Assert.Throws<RoomUserAlreadyJoinedException>(act);
    }

    [Fact]
    public void JoinPlayer_WhenRoomIsInProgress_ThrowsRoomJoinNotAllowedException()
    {
        // Arrange
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Test Room", UserId.New(), DateTime.UtcNow);
        var alice = room.JoinPlayer(UserId.New(), DateTime.UtcNow.AddSeconds(1));
        room.SetPlayerReady(alice.Id, true);
        room.StartGame(room.HostPlayerId);

        // Act
        Action act = () => room.JoinPlayer(UserId.New(), DateTime.UtcNow.AddSeconds(2));

        // Assert
        Assert.Throws<RoomJoinNotAllowedException>(act);
    }

    [Fact]
    public void JoinPlayer_WhenRoomIsAtCapacity_ThrowsRoomJoinCapacityReachedException()
    {
        // Arrange
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Test Room", UserId.New(), DateTime.UtcNow);
        for (var i = 0; i < 19; i++)
        {
            room.JoinPlayer(UserId.New(), DateTime.UtcNow.AddSeconds(i + 1));
        }

        // Act — 21st player
        Action act = () => room.JoinPlayer(UserId.New(), DateTime.UtcNow.AddSeconds(20));

        // Assert
        Assert.Throws<RoomJoinCapacityReachedException>(act);
    }

    [Fact]
    public void LeavePlayer_WhenNonHostLeaves_RemovesPlayerAndKeepsExistingHost()
    {
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Test Room", UserId.New(), DateTime.UtcNow);
        var alice = room.JoinPlayer(UserId.New(), DateTime.UtcNow.AddSeconds(1));
        var bob = room.JoinPlayer(UserId.New(), DateTime.UtcNow.AddSeconds(2));
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
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Test Room", UserId.New(), DateTime.UtcNow);
        var originalHostId = room.HostPlayerId;
        var alice = room.JoinPlayer(UserId.New(), DateTime.UtcNow.AddSeconds(1));
        var bob = room.JoinPlayer(UserId.New(), DateTime.UtcNow.AddSeconds(2));

        var outcome = room.LeavePlayer(originalHostId);

        Assert.False(outcome.RoomWasRemoved);
        Assert.Equal(alice.Id, outcome.NewHostPlayerId);
        Assert.Equal(alice.Id, room.HostPlayerId);
        Assert.DoesNotContain(room.Players, player => player.Id == originalHostId);
        Assert.Contains(room.Players, player => player.Id == bob.Id && player.Role == PlayerRole.Player);
    }

    [Fact]
    public void LeavePlayer_WhenLastPlayerLeaves_ReturnsRoomRemovedOutcome()
    {
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Test Room", UserId.New(), DateTime.UtcNow);
        var hostPlayerId = room.HostPlayerId;

        var outcome = room.LeavePlayer(hostPlayerId);

        Assert.True(outcome.RoomWasRemoved);
        Assert.Null(outcome.NewHostPlayerId);
        Assert.Empty(room.Players);
    }

    [Fact]
    public void LeavePlayer_WhenPlayerDoesNotExist_ThrowsRoomLeavePlayerNotFoundException()
    {
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Test Room", UserId.New(), DateTime.UtcNow);
        var missingPlayerId = PlayerId.New();

        var exception = Assert.Throws<RoomLeavePlayerNotFoundException>(() => room.LeavePlayer(missingPlayerId));

        Assert.Equal(missingPlayerId, exception.PlayerId);
    }

    [Fact]
    public void SetPlayerReady_WhenPlayerExists_UpdatesPlayerReadiness()
    {
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Test Room", UserId.New(), DateTime.UtcNow);
        var alice = room.JoinPlayer(UserId.New(), DateTime.UtcNow.AddSeconds(1));

        room.SetPlayerReady(alice.Id, true);

        Assert.Contains(room.Players, player => player.Id == alice.Id && player.IsReady);
    }

    [Fact]
    public void SetPlayerReady_WhenRoomIsNotWaitingForPlayers_ThrowsRoomSetPlayerReadyNotAllowedException()
    {
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Test Room", UserId.New(), DateTime.UtcNow);
        var alice = room.JoinPlayer(UserId.New(), DateTime.UtcNow.AddSeconds(1));
        room.SetPlayerReady(alice.Id, true);
        var startOutcome = room.StartGame(room.HostPlayerId);
        Assert.True(startOutcome.IsStarted);

        var act = () => room.SetPlayerReady(alice.Id, false);

        Assert.Throws<RoomSetPlayerReadyNotAllowedException>(act);
    }

    [Fact]
    public void SetPlayerReady_WhenPlayerNotFound_ThrowsRoomSetPlayerReadyPlayerNotFoundException()
    {
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Test Room", UserId.New(), DateTime.UtcNow);
        var missingId = PlayerId.New();

        Assert.Throws<RoomSetPlayerReadyPlayerNotFoundException>(() => room.SetPlayerReady(missingId, true));
    }

    [Fact]
    public void StartGame_WhenOnlyHostIsInRoom_ReturnsNotEnoughPlayersReason()
    {
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Test Room", UserId.New(), DateTime.UtcNow);

        var outcome = room.StartGame(room.HostPlayerId);

        Assert.False(outcome.IsStarted);
        Assert.Equal(RoomStartGameBlockedReason.NotEnoughPlayers, outcome.BlockedReason);
        Assert.Equal(RoomStatus.WaitingForPlayers, room.Status);
    }

    [Fact]
    public void StartGame_WhenNonHostPlayerIsNotReady_ReturnsNonHostPlayersNotReadyReason()
    {
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Test Room", UserId.New(), DateTime.UtcNow);
        room.JoinPlayer(UserId.New(), DateTime.UtcNow.AddSeconds(1));

        var outcome = room.StartGame(room.HostPlayerId);

        Assert.False(outcome.IsStarted);
        Assert.Equal(RoomStartGameBlockedReason.NonHostPlayersNotReady, outcome.BlockedReason);
    }

    [Fact]
    public void StartGame_WhenAllNonHostPlayersAreReady_StartsGame()
    {
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Test Room", UserId.New(), DateTime.UtcNow);
        var alice = room.JoinPlayer(UserId.New(), DateTime.UtcNow.AddSeconds(1));
        room.SetPlayerReady(alice.Id, true);

        var outcome = room.StartGame(room.HostPlayerId);

        Assert.True(outcome.IsStarted);
        Assert.Null(outcome.BlockedReason);
        Assert.Equal(RoomStatus.InProgress, room.Status);
    }

    [Fact]
    public void StartGame_WhenStartedByNonHost_ReturnsStartedByNonHostReason()
    {
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Test Room", UserId.New(), DateTime.UtcNow);
        var alice = room.JoinPlayer(UserId.New(), DateTime.UtcNow.AddSeconds(1));
        room.SetPlayerReady(alice.Id, true);

        var outcome = room.StartGame(alice.Id);

        Assert.False(outcome.IsStarted);
        Assert.Equal(RoomStartGameBlockedReason.StartedByNonHost, outcome.BlockedReason);
    }

    [Fact]
    public void StartGame_WhenRoomIsAlreadyInProgress_ReturnsRoomIsNotWaitingForPlayersReason()
    {
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Test Room", UserId.New(), DateTime.UtcNow);
        var alice = room.JoinPlayer(UserId.New(), DateTime.UtcNow.AddSeconds(1));
        room.SetPlayerReady(alice.Id, true);
        room.StartGame(room.HostPlayerId);

        var outcome = room.StartGame(room.HostPlayerId);

        Assert.False(outcome.IsStarted);
        Assert.Equal(RoomStartGameBlockedReason.RoomIsNotWaitingForPlayers, outcome.BlockedReason);
    }

    [Fact]
    public void StartGame_WhenStarterPlayerNotFound_ReturnsStarterPlayerNotFoundReason()
    {
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Test Room", UserId.New(), DateTime.UtcNow);
        var missingId = PlayerId.New();

        var outcome = room.StartGame(missingId);

        Assert.False(outcome.IsStarted);
        Assert.Equal(RoomStartGameBlockedReason.StarterPlayerNotFound, outcome.BlockedReason);
    }

    [Fact]
    public void Rehydrate_WhenInputIsValid_SetsAllProperties()
    {
        // Arrange
        var id = RoomId.New();
        var code = new RoomCode("AB12CD");
        const string roomName = "Test Room";
        var createdAt = new DateTime(2026, 3, 17, 12, 0, 0, DateTimeKind.Utc);
        var hostPlayer = Player.Create(PlayerId.New(), UserId.New(), PlayerRole.Host, createdAt);

        // Act
        var room = Room.Rehydrate(id, code, roomName, [hostPlayer], RoomStatus.WaitingForPlayers, createdAt);

        // Assert
        Assert.Equal(id, room.Id);
        Assert.Equal(code, room.Code);
        Assert.Equal(roomName, room.Name);
        Assert.Equal(RoomStatus.WaitingForPlayers, room.Status);
        Assert.Single(room.Players);
    }

    [Fact]
    public void Rehydrate_WhenPlayersIsNull_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => Room.Rehydrate(
            RoomId.New(),
            new RoomCode("AB12CD"),
            "Test Room",
            null!,
            RoomStatus.WaitingForPlayers,
            DateTime.UtcNow);

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public void Create_WhenCalled_RaisesRoomCreatedDomainEvent()
    {
        // Act
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Test Room", UserId.New(), DateTime.UtcNow);

        // Assert
        var domainEvent = Assert.Single(room.UncommittedEvents);
        var createdEvent = Assert.IsType<BOTC.Domain.Rooms.Events.RoomCreatedDomainEvent>(domainEvent);
        Assert.Equal(room.Id, createdEvent.RoomId);
        Assert.Equal(room.Code, createdEvent.RoomCode);
        Assert.Equal(room.HostPlayerId, createdEvent.HostPlayerId);
    }

    [Fact]
    public void JoinPlayer_WhenCalled_RaisesPlayerJoinedRoomDomainEvent()
    {
        // Arrange
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Test Room", UserId.New(), DateTime.UtcNow);
        room.ClearUncommittedEvents();

        // Act
        var joined = room.JoinPlayer(UserId.New(), DateTime.UtcNow.AddSeconds(1));

        // Assert
        var domainEvent = Assert.Single(room.UncommittedEvents);
        var joinedEvent = Assert.IsType<BOTC.Domain.Rooms.Events.PlayerJoinedRoomDomainEvent>(domainEvent);
        Assert.Equal(room.Id, joinedEvent.RoomId);
        Assert.Equal(joined.Id, joinedEvent.PlayerId);
    }

    [Fact]
    public void LeavePlayer_WhenLastPlayerLeaves_RaisesPlayerLeftRoomDomainEventWithIsRoomDeletedTrue()
    {
        // Arrange
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Test Room", UserId.New(), DateTime.UtcNow);
        var hostId = room.HostPlayerId;
        room.ClearUncommittedEvents();

        // Act
        room.LeavePlayer(hostId);

        // Assert
        var domainEvent = Assert.Single(room.UncommittedEvents);
        var leftEvent = Assert.IsType<BOTC.Domain.Rooms.Events.PlayerLeftRoomDomainEvent>(domainEvent);
        Assert.True(leftEvent.IsRoomDeleted);
        Assert.Equal(hostId, leftEvent.PlayerId);
        Assert.Null(leftEvent.NewHostPlayerId);
    }

    [Fact]
    public void LeavePlayer_WhenHostLeaves_RaisesPlayerLeftRoomDomainEventWithNewHostId()
    {
        // Arrange
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Test Room", UserId.New(), DateTime.UtcNow);
        var alice = room.JoinPlayer(UserId.New(), DateTime.UtcNow.AddSeconds(1));
        var originalHostId = room.HostPlayerId;
        room.ClearUncommittedEvents();

        // Act
        room.LeavePlayer(originalHostId);

        // Assert
        var domainEvent = Assert.Single(room.UncommittedEvents);
        var leftEvent = Assert.IsType<BOTC.Domain.Rooms.Events.PlayerLeftRoomDomainEvent>(domainEvent);
        Assert.False(leftEvent.IsRoomDeleted);
        Assert.Equal(originalHostId, leftEvent.PlayerId);
        Assert.Equal(alice.Id, leftEvent.NewHostPlayerId);
    }

    [Fact]
    public void SetPlayerReady_WhenCalled_RaisesPlayerReadyStateChangedDomainEvent()
    {
        // Arrange
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Test Room", UserId.New(), DateTime.UtcNow);
        var alice = room.JoinPlayer(UserId.New(), DateTime.UtcNow.AddSeconds(1));
        room.ClearUncommittedEvents();

        // Act
        room.SetPlayerReady(alice.Id, true);

        // Assert
        var domainEvent = Assert.Single(room.UncommittedEvents);
        var readyEvent = Assert.IsType<BOTC.Domain.Rooms.Events.PlayerReadyStateChangedDomainEvent>(domainEvent);
        Assert.Equal(alice.Id, readyEvent.PlayerId);
        Assert.True(readyEvent.IsReady);
    }

    [Fact]
    public void StartGame_WhenStarted_RaisesGameStartedDomainEvent()
    {
        // Arrange
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Test Room", UserId.New(), DateTime.UtcNow);
        var alice = room.JoinPlayer(UserId.New(), DateTime.UtcNow.AddSeconds(1));
        room.SetPlayerReady(alice.Id, true);
        room.ClearUncommittedEvents();

        // Act
        room.StartGame(room.HostPlayerId);

        // Assert
        var domainEvent = Assert.Single(room.UncommittedEvents);
        Assert.IsType<BOTC.Domain.Rooms.Events.GameStartedDomainEvent>(domainEvent);
    }
}
