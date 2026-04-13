using BOTC.Domain.Rooms;
using BOTC.Domain.Rooms.Events;
using BOTC.Domain.Rooms.Exceptions;
using BOTC.Domain.Rooms.Outcomes;
using BOTC.Domain.Users;

namespace BOTC.Domain.Tests.Rooms;

public sealed class RoomTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Room CreateRoom(string name = "Test Room") =>
        Room.Create(RoomId.New(), new RoomCode("AB12CD"), name, UserId.New(), DateTime.UtcNow);

    // ── Create ────────────────────────────────────────────────────────────────

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

    [Fact]
    public void Create_WhenInputIsValid_AddsHostPlayerToRoom()
    {
        // Arrange
        var hostUserId = UserId.New();

        // Act
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Test Room", hostUserId, DateTime.UtcNow);

        // Assert
        Assert.Single(room.Players);
        Assert.Equal(PlayerRole.Host, room.Players.Single().Role);
        Assert.Equal(hostUserId, room.Players.Single().UserId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WhenRoomNameIsNullEmptyOrWhitespace_ThrowsArgumentException(string? roomName)
    {
        // Act
        Action act = () => Room.Create(RoomId.New(), new RoomCode("AB12CD"), roomName!, UserId.New(), DateTime.UtcNow);

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Create_WhenRoomNameExceedsMaxLength_ThrowsArgumentException()
    {
        // Arrange
        var longName = new string('a', 31);

        // Act
        Action act = () => Room.Create(RoomId.New(), new RoomCode("AB12CD"), longName, UserId.New(), DateTime.UtcNow);

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Create_WhenRoomNameIsExactlyMaxLength_Succeeds()
    {
        // Arrange
        var maxName = new string('a', 30);

        // Act
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), maxName, UserId.New(), DateTime.UtcNow);

        // Assert
        Assert.Equal(maxName, room.Name);
    }

    [Fact]
    public void Create_WhenRoomNameHasSurroundingWhitespace_TrimsRoomName()
    {
        // Act
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "  Test Room  ", UserId.New(), DateTime.UtcNow);

        // Assert
        Assert.Equal("Test Room", room.Name);
    }

    [Fact]
    public void Create_WhenCreatedAtIsLocal_ConvertsCreatedAtToUtc()
    {
        // Arrange
        var localDateTime = new DateTime(2026, 3, 17, 13, 0, 0, DateTimeKind.Local);

        // Act
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Test Room", UserId.New(), localDateTime);

        // Assert
        Assert.Equal(DateTimeKind.Utc, room.CreatedAtUtc.Kind);
        Assert.Equal(localDateTime.ToUniversalTime(), room.CreatedAtUtc);
    }

    [Fact]
    public void Create_WhenCreatedAtKindIsUnspecified_ThrowsArgumentException()
    {
        // Arrange
        var unspecifiedDateTime = new DateTime(2026, 3, 17, 13, 0, 0, DateTimeKind.Unspecified);

        // Act
        Action act = () => Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Test Room", UserId.New(), unspecifiedDateTime);

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Create_WhenCalled_RaisesRoomCreatedDomainEvent()
    {
        // Act
        var room = Room.Create(RoomId.New(), new RoomCode("AB12CD"), "Test Room", UserId.New(), DateTime.UtcNow);

        // Assert
        var domainEvent = Assert.Single(room.UncommittedEvents);
        var createdEvent = Assert.IsType<RoomCreatedDomainEvent>(domainEvent);
        Assert.Equal(room.Id, createdEvent.RoomId);
        Assert.Equal(room.Code, createdEvent.RoomCode);
        Assert.Equal(room.HostPlayerId, createdEvent.HostPlayerId);
    }

    // ── Rehydrate ─────────────────────────────────────────────────────────────

    [Fact]
    public void Rehydrate_WhenInputIsValid_SetsAllProperties()
    {
        // Arrange
        var id = RoomId.New();
        var code = new RoomCode("AB12CD");
        var createdAt = new DateTime(2026, 3, 17, 12, 0, 0, DateTimeKind.Utc);
        var hostPlayer = Player.Create(PlayerId.New(), UserId.New(), PlayerRole.Host, createdAt);

        // Act
        var room = Room.Rehydrate(id, code, "Test Room", [hostPlayer], RoomStatus.WaitingForPlayers, createdAt);

        // Assert
        Assert.Equal(id, room.Id);
        Assert.Equal(code, room.Code);
        Assert.Equal("Test Room", room.Name);
        Assert.Equal(RoomStatus.WaitingForPlayers, room.Status);
        Assert.Single(room.Players);
    }

    [Fact]
    public void Rehydrate_WhenPlayersIsNull_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => Room.Rehydrate(RoomId.New(), new RoomCode("AB12CD"), "Test Room", null!, RoomStatus.WaitingForPlayers, DateTime.UtcNow);

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public void Rehydrate_WhenNoHostPresent_ThrowsArgumentException()
    {
        // Arrange
        var player = Player.Create(PlayerId.New(), UserId.New(), PlayerRole.Player, DateTime.UtcNow);

        // Act
        Action act = () => Room.Rehydrate(RoomId.New(), new RoomCode("AB12CD"), "Test Room", [player], RoomStatus.WaitingForPlayers, DateTime.UtcNow);

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Rehydrate_WhenMultipleHostsPresent_ThrowsArgumentException()
    {
        // Arrange
        var host1 = Player.Create(PlayerId.New(), UserId.New(), PlayerRole.Host, DateTime.UtcNow);
        var host2 = Player.Create(PlayerId.New(), UserId.New(), PlayerRole.Host, DateTime.UtcNow.AddSeconds(1));

        // Act
        Action act = () => Room.Rehydrate(RoomId.New(), new RoomCode("AB12CD"), "Test Room", [host1, host2], RoomStatus.WaitingForPlayers, DateTime.UtcNow);

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Rehydrate_WhenDuplicateUserIdsPresent_ThrowsArgumentException()
    {
        // Arrange
        var sharedUserId = UserId.New();
        var host = Player.Create(PlayerId.New(), sharedUserId, PlayerRole.Host, DateTime.UtcNow);
        var duplicate = Player.Create(PlayerId.New(), sharedUserId, PlayerRole.Player, DateTime.UtcNow.AddSeconds(1));

        // Act
        Action act = () => Room.Rehydrate(RoomId.New(), new RoomCode("AB12CD"), "Test Room", [host, duplicate], RoomStatus.WaitingForPlayers, DateTime.UtcNow);

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Rehydrate_WhenStatusIsInvalid_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var host = Player.Create(PlayerId.New(), UserId.New(), PlayerRole.Host, DateTime.UtcNow);
        var invalidStatus = (RoomStatus)999;

        // Act
        Action act = () => Room.Rehydrate(RoomId.New(), new RoomCode("AB12CD"), "Test Room", [host], invalidStatus, DateTime.UtcNow);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    // ── Rename ────────────────────────────────────────────────────────────────

    [Fact]
    public void Rename_WhenInputIsValid_UpdatesName()
    {
        // Arrange
        var room = CreateRoom("Original Name");

        // Act
        room.Rename("New Name");

        // Assert
        Assert.Equal("New Name", room.Name);
    }

    [Fact]
    public void Rename_WhenInputHasSurroundingWhitespace_TrimsName()
    {
        // Arrange
        var room = CreateRoom("Original Name");

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
        var room = CreateRoom();

        // Act
        Action act = () => room.Rename(name!);

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Rename_WhenNameExceedsMaxLength_ThrowsArgumentException()
    {
        // Arrange
        var room = CreateRoom();
        var longName = new string('a', 31);

        // Act
        Action act = () => room.Rename(longName);

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    // ── Players collection ────────────────────────────────────────────────────

    [Fact]
    public void Players_WhenExposed_CannotBeMutatedThroughCollectionInterface()
    {
        // Arrange
        var room = CreateRoom();

        // Act
        var players = room.Players;

        // Assert
        Assert.IsNotType<List<Player>>(players);
        var collection = Assert.IsAssignableFrom<ICollection<Player>>(players);
        Assert.True(collection.IsReadOnly);
        Assert.Throws<NotSupportedException>(() =>
            collection.Add(Player.Create(PlayerId.New(), UserId.New(), PlayerRole.Player, DateTime.UtcNow)));
    }

    // ── JoinPlayer ────────────────────────────────────────────────────────────

    [Fact]
    public void JoinPlayer_WhenInputIsValid_AddsPlayerAndReturnsNewPlayer()
    {
        // Arrange
        var room = CreateRoom();
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
    public void JoinPlayer_WhenJoinedAtKindIsUnspecified_ThrowsArgumentException()
    {
        // Arrange
        var room = CreateRoom();
        var unspecifiedDateTime = new DateTime(2026, 3, 17, 13, 0, 0, DateTimeKind.Unspecified);

        // Act
        Action act = () => room.JoinPlayer(UserId.New(), unspecifiedDateTime);

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void JoinPlayer_WhenJoinedAtIsLocal_ConvertsToUtc()
    {
        // Arrange
        var room = CreateRoom();
        var localDateTime = new DateTime(2026, 3, 17, 14, 0, 0, DateTimeKind.Local);

        // Act
        var joined = room.JoinPlayer(UserId.New(), localDateTime);

        // Assert
        Assert.Equal(DateTimeKind.Utc, joined.JoinedAtUtc.Kind);
        Assert.Equal(localDateTime.ToUniversalTime(), joined.JoinedAtUtc);
    }

    [Fact]
    public void JoinPlayer_WhenSameUserJoinsTwice_ThrowsRoomUserAlreadyJoinedException()
    {
        // Arrange
        var room = CreateRoom();
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
        var room = CreateRoom();
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
        var room = CreateRoom();
        for (var i = 0; i < 19; i++)
        {
            room.JoinPlayer(UserId.New(), DateTime.UtcNow.AddSeconds(i + 1));
        }

        // Act - 21st player
        Action act = () => room.JoinPlayer(UserId.New(), DateTime.UtcNow.AddSeconds(20));

        // Assert
        Assert.Throws<RoomJoinCapacityReachedException>(act);
    }

    [Fact]
    public void JoinPlayer_WhenCalled_RaisesPlayerJoinedRoomDomainEvent()
    {
        // Arrange
        var room = CreateRoom();
        room.ClearUncommittedEvents();

        // Act
        var joined = room.JoinPlayer(UserId.New(), DateTime.UtcNow.AddSeconds(1));

        // Assert
        var domainEvent = Assert.Single(room.UncommittedEvents);
        var joinedEvent = Assert.IsType<PlayerJoinedRoomDomainEvent>(domainEvent);
        Assert.Equal(room.Id, joinedEvent.RoomId);
        Assert.Equal(joined.Id, joinedEvent.PlayerId);
    }

    // ── LeavePlayer ───────────────────────────────────────────────────────────

    [Fact]
    public void LeavePlayer_WhenNonHostLeaves_RemovesPlayerAndKeepsExistingHost()
    {
        // Arrange
        var room = CreateRoom();
        var alice = room.JoinPlayer(UserId.New(), DateTime.UtcNow.AddSeconds(1));
        var bob = room.JoinPlayer(UserId.New(), DateTime.UtcNow.AddSeconds(2));
        var originalHostId = room.HostPlayerId;

        // Act
        var outcome = room.LeavePlayer(alice.Id);

        // Assert
        Assert.False(outcome.RoomWasRemoved);
        Assert.Null(outcome.NewHostPlayerId);
        Assert.False(outcome.HostWasTransferred);
        Assert.Equal(originalHostId, room.HostPlayerId);
        Assert.DoesNotContain(room.Players, p => p.Id == alice.Id);
        Assert.Contains(room.Players, p => p.Id == bob.Id);
    }

    [Fact]
    public void LeavePlayer_WhenHostLeaves_TransfersHostToLongestPresentRemainingPlayer()
    {
        // Arrange
        var room = CreateRoom();
        var originalHostId = room.HostPlayerId;
        var alice = room.JoinPlayer(UserId.New(), DateTime.UtcNow.AddSeconds(1));
        var bob = room.JoinPlayer(UserId.New(), DateTime.UtcNow.AddSeconds(2));

        // Act
        var outcome = room.LeavePlayer(originalHostId);

        // Assert
        Assert.False(outcome.RoomWasRemoved);
        Assert.Equal(alice.Id, outcome.NewHostPlayerId);
        Assert.True(outcome.HostWasTransferred);
        Assert.Equal(alice.Id, room.HostPlayerId);
        Assert.DoesNotContain(room.Players, p => p.Id == originalHostId);
        Assert.Contains(room.Players, p => p.Id == bob.Id && p.Role == PlayerRole.Player);
    }

    [Fact]
    public void LeavePlayer_WhenLastPlayerLeaves_ReturnsRoomRemovedOutcome()
    {
        // Arrange
        var room = CreateRoom();
        var hostPlayerId = room.HostPlayerId;

        // Act
        var outcome = room.LeavePlayer(hostPlayerId);

        // Assert
        Assert.True(outcome.RoomWasRemoved);
        Assert.Null(outcome.NewHostPlayerId);
        Assert.Empty(room.Players);
    }

    [Fact]
    public void LeavePlayer_WhenPlayerDoesNotExist_ThrowsRoomLeavePlayerNotFoundException()
    {
        // Arrange
        var room = CreateRoom();
        var missingPlayerId = PlayerId.New();

        // Act
        var exception = Assert.Throws<RoomLeavePlayerNotFoundException>(() => room.LeavePlayer(missingPlayerId));

        // Assert
        Assert.Equal(missingPlayerId, exception.PlayerId);
    }

    [Fact]
    public void LeavePlayer_WhenLastPlayerLeaves_RaisesPlayerLeftRoomDomainEventWithIsRoomDeletedTrue()
    {
        // Arrange
        var room = CreateRoom();
        var hostId = room.HostPlayerId;
        room.ClearUncommittedEvents();

        // Act
        room.LeavePlayer(hostId);

        // Assert
        var domainEvent = Assert.Single(room.UncommittedEvents);
        var leftEvent = Assert.IsType<PlayerLeftRoomDomainEvent>(domainEvent);
        Assert.True(leftEvent.IsRoomDeleted);
        Assert.Equal(hostId, leftEvent.PlayerId);
        Assert.Null(leftEvent.NewHostPlayerId);
    }

    [Fact]
    public void LeavePlayer_WhenNonHostLeaves_RaisesPlayerLeftRoomDomainEventWithNullNewHostId()
    {
        // Arrange
        var room = CreateRoom();
        var alice = room.JoinPlayer(UserId.New(), DateTime.UtcNow.AddSeconds(1));
        room.ClearUncommittedEvents();

        // Act
        room.LeavePlayer(alice.Id);

        // Assert
        var domainEvent = Assert.Single(room.UncommittedEvents);
        var leftEvent = Assert.IsType<PlayerLeftRoomDomainEvent>(domainEvent);
        Assert.False(leftEvent.IsRoomDeleted);
        Assert.Equal(alice.Id, leftEvent.PlayerId);
        Assert.Null(leftEvent.NewHostPlayerId);
    }

    [Fact]
    public void LeavePlayer_WhenHostLeaves_RaisesPlayerLeftRoomDomainEventWithNewHostId()
    {
        // Arrange
        var room = CreateRoom();
        var alice = room.JoinPlayer(UserId.New(), DateTime.UtcNow.AddSeconds(1));
        var originalHostId = room.HostPlayerId;
        room.ClearUncommittedEvents();

        // Act
        room.LeavePlayer(originalHostId);

        // Assert
        var domainEvent = Assert.Single(room.UncommittedEvents);
        var leftEvent = Assert.IsType<PlayerLeftRoomDomainEvent>(domainEvent);
        Assert.False(leftEvent.IsRoomDeleted);
        Assert.Equal(originalHostId, leftEvent.PlayerId);
        Assert.Equal(alice.Id, leftEvent.NewHostPlayerId);
    }

    // ── SetPlayerReady ────────────────────────────────────────────────────────

    [Fact]
    public void SetPlayerReady_WhenPlayerExists_UpdatesPlayerReadiness()
    {
        // Arrange
        var room = CreateRoom();
        var alice = room.JoinPlayer(UserId.New(), DateTime.UtcNow.AddSeconds(1));

        // Act
        room.SetPlayerReady(alice.Id, true);

        // Assert
        Assert.Contains(room.Players, p => p.Id == alice.Id && p.IsReady);
    }

    [Fact]
    public void SetPlayerReady_WhenCalledWithFalse_MarksPlayerAsNotReady()
    {
        // Arrange
        var room = CreateRoom();
        var alice = room.JoinPlayer(UserId.New(), DateTime.UtcNow.AddSeconds(1));
        room.SetPlayerReady(alice.Id, true);

        // Act
        room.SetPlayerReady(alice.Id, false);

        // Assert
        Assert.Contains(room.Players, p => p.Id == alice.Id && !p.IsReady);
    }

    [Fact]
    public void SetPlayerReady_WhenRoomIsNotWaitingForPlayers_ThrowsRoomSetPlayerReadyNotAllowedException()
    {
        // Arrange
        var room = CreateRoom();
        var alice = room.JoinPlayer(UserId.New(), DateTime.UtcNow.AddSeconds(1));
        room.SetPlayerReady(alice.Id, true);
        var startOutcome = room.StartGame(room.HostPlayerId);
        Assert.True(startOutcome.IsStarted);

        // Act
        var act = () => room.SetPlayerReady(alice.Id, false);

        // Assert
        Assert.Throws<RoomSetPlayerReadyNotAllowedException>(act);
    }

    [Fact]
    public void SetPlayerReady_WhenPlayerNotFound_ThrowsRoomSetPlayerReadyPlayerNotFoundException()
    {
        // Arrange
        var room = CreateRoom();
        var missingId = PlayerId.New();

        // Act & Assert
        Assert.Throws<RoomSetPlayerReadyPlayerNotFoundException>(() => room.SetPlayerReady(missingId, true));
    }

    [Fact]
    public void SetPlayerReady_WhenCalled_RaisesPlayerReadyStateChangedDomainEvent()
    {
        // Arrange
        var room = CreateRoom();
        var alice = room.JoinPlayer(UserId.New(), DateTime.UtcNow.AddSeconds(1));
        room.ClearUncommittedEvents();

        // Act
        room.SetPlayerReady(alice.Id, true);

        // Assert
        var domainEvent = Assert.Single(room.UncommittedEvents);
        var readyEvent = Assert.IsType<PlayerReadyStateChangedDomainEvent>(domainEvent);
        Assert.Equal(alice.Id, readyEvent.PlayerId);
        Assert.True(readyEvent.IsReady);
    }

    // ── StartGame ─────────────────────────────────────────────────────────────

    [Fact]
    public void StartGame_WhenOnlyHostIsInRoom_ReturnsNotEnoughPlayersReason()
    {
        var room = CreateRoom();

        var outcome = room.StartGame(room.HostPlayerId);

        Assert.False(outcome.IsStarted);
        Assert.Equal(RoomStartGameBlockedReason.NotEnoughPlayers, outcome.BlockedReason);
        Assert.Equal(RoomStatus.WaitingForPlayers, room.Status);
    }

    [Fact]
    public void StartGame_WhenNonHostPlayerIsNotReady_ReturnsNonHostPlayersNotReadyReason()
    {
        var room = CreateRoom();
        room.JoinPlayer(UserId.New(), DateTime.UtcNow.AddSeconds(1));

        var outcome = room.StartGame(room.HostPlayerId);

        Assert.False(outcome.IsStarted);
        Assert.Equal(RoomStartGameBlockedReason.NonHostPlayersNotReady, outcome.BlockedReason);
    }

    [Fact]
    public void StartGame_WhenAllNonHostPlayersAreReady_StartsGame()
    {
        var room = CreateRoom();
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
        var room = CreateRoom();
        var alice = room.JoinPlayer(UserId.New(), DateTime.UtcNow.AddSeconds(1));
        room.SetPlayerReady(alice.Id, true);

        var outcome = room.StartGame(alice.Id);

        Assert.False(outcome.IsStarted);
        Assert.Equal(RoomStartGameBlockedReason.StartedByNonHost, outcome.BlockedReason);
    }

    [Fact]
    public void StartGame_WhenRoomIsAlreadyInProgress_ReturnsRoomIsNotWaitingForPlayersReason()
    {
        var room = CreateRoom();
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
        var room = CreateRoom();
        var missingId = PlayerId.New();

        var outcome = room.StartGame(missingId);

        Assert.False(outcome.IsStarted);
        Assert.Equal(RoomStartGameBlockedReason.StarterPlayerNotFound, outcome.BlockedReason);
    }

    [Fact]
    public void StartGame_WhenStarted_RaisesGameStartedDomainEvent()
    {
        // Arrange
        var room = CreateRoom();
        var alice = room.JoinPlayer(UserId.New(), DateTime.UtcNow.AddSeconds(1));
        room.SetPlayerReady(alice.Id, true);
        room.ClearUncommittedEvents();

        // Act
        room.StartGame(room.HostPlayerId);

        // Assert
        var domainEvent = Assert.Single(room.UncommittedEvents);
        var gameStarted = Assert.IsType<GameStartedDomainEvent>(domainEvent);
        Assert.Equal(room.Id, gameStarted.RoomId);
    }

    [Fact]
    public void StartGame_WhenMultiplePlayersAllReady_StartsGame()
    {
        // Arrange
        var room = CreateRoom();
        var alice = room.JoinPlayer(UserId.New(), DateTime.UtcNow.AddSeconds(1));
        var bob = room.JoinPlayer(UserId.New(), DateTime.UtcNow.AddSeconds(2));
        var carol = room.JoinPlayer(UserId.New(), DateTime.UtcNow.AddSeconds(3));
        room.SetPlayerReady(alice.Id, true);
        room.SetPlayerReady(bob.Id, true);
        room.SetPlayerReady(carol.Id, true);

        // Act
        var outcome = room.StartGame(room.HostPlayerId);

        // Assert
        Assert.True(outcome.IsStarted);
        Assert.Equal(RoomStatus.InProgress, room.Status);
    }

    // ── ClearUncommittedEvents ────────────────────────────────────────────────

    [Fact]
    public void ClearUncommittedEvents_WhenCalled_RemovesAllEvents()
    {
        // Arrange
        var room = CreateRoom();
        Assert.NotEmpty(room.UncommittedEvents);

        // Act
        room.ClearUncommittedEvents();

        // Assert
        Assert.Empty(room.UncommittedEvents);
    }
}
