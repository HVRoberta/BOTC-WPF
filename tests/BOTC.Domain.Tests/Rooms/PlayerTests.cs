using BOTC.Domain.Rooms;
using BOTC.Domain.Users;

namespace BOTC.Domain.Tests.Rooms;

public sealed class PlayerTests
{
    // ── Create ───────────────────────────────────────────────────────────────

    [Fact]
    public void Create_WhenInputIsValid_CreatesPlayerWithExpectedProperties()
    {
        // Arrange
        var playerId = PlayerId.New();
        var userId = UserId.New();
        var createdAtUtc = DateTime.UtcNow;

        // Act
        var player = Player.Create(playerId, userId, PlayerRole.Player, createdAtUtc);

        // Assert
        Assert.Equal(playerId, player.Id);
        Assert.Equal(userId, player.UserId);
        Assert.Equal(PlayerRole.Player, player.Role);
        Assert.Equal(createdAtUtc, player.JoinedAtUtc);
        Assert.False(player.IsReady);
    }

    [Fact]
    public void Create_WhenIsReadyProvided_SetsReadyState()
    {
        // Arrange
        var playerId = PlayerId.New();
        var userId = UserId.New();

        // Act
        var player = Player.Create(playerId, userId, PlayerRole.Host, DateTime.UtcNow, isReady: true);

        // Assert
        Assert.True(player.IsReady);
    }

    [Fact]
    public void Create_WhenJoinedAtKindIsUnspecified_ThrowsArgumentException()
    {
        // Arrange
        var unspecifiedDateTime = new DateTime(2026, 3, 17, 13, 0, 0, DateTimeKind.Unspecified);

        // Act
        Action act = () => _ = Player.Create(PlayerId.New(), UserId.New(), PlayerRole.Player, unspecifiedDateTime);

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Create_WhenJoinedAtIsLocal_ConvertsToUtc()
    {
        // Arrange
        var localDateTime = new DateTime(2026, 3, 17, 13, 0, 0, DateTimeKind.Local);

        // Act
        var player = Player.Create(PlayerId.New(), UserId.New(), PlayerRole.Player, localDateTime);

        // Assert
        Assert.Equal(DateTimeKind.Utc, player.JoinedAtUtc.Kind);
        Assert.Equal(localDateTime.ToUniversalTime(), player.JoinedAtUtc);
    }

    [Fact]
    public void Create_WhenRoleIsInvalid_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var invalidRole = (PlayerRole)999;

        // Act
        Action act = () => _ = Player.Create(PlayerId.New(), UserId.New(), invalidRole, DateTime.UtcNow);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    // ── Rehydrate ─────────────────────────────────────────────────────────────

    [Fact]
    public void Rehydrate_WhenInputIsValid_CreatesPlayerWithExpectedProperties()
    {
        // Arrange
        var playerId = PlayerId.New();
        var userId = UserId.New();
        var createdAtUtc = DateTime.UtcNow;

        // Act
        var player = Player.Rehydrate(playerId, userId, PlayerRole.Player, createdAtUtc, isReady: true);

        // Assert
        Assert.Equal(playerId, player.Id);
        Assert.Equal(userId, player.UserId);
        Assert.Equal(PlayerRole.Player, player.Role);
        Assert.Equal(DateTimeKind.Utc, player.JoinedAtUtc.Kind);
        Assert.True(player.IsReady);
    }

    [Fact]
    public void Rehydrate_WhenJoinedAtIsLocal_ConvertsToUtc()
    {
        // Arrange
        var localDateTime = new DateTime(2026, 3, 17, 13, 0, 0, DateTimeKind.Local);

        // Act
        var player = Player.Rehydrate(PlayerId.New(), UserId.New(), PlayerRole.Player, localDateTime, isReady: false);

        // Assert
        Assert.Equal(DateTimeKind.Utc, player.JoinedAtUtc.Kind);
        Assert.Equal(localDateTime.ToUniversalTime(), player.JoinedAtUtc);
    }

    [Fact]
    public void Rehydrate_WhenJoinedAtKindIsUnspecified_ThrowsArgumentException()
    {
        // Arrange
        var unspecifiedDateTime = new DateTime(2026, 3, 17, 13, 0, 0, DateTimeKind.Unspecified);

        // Act
        Action act = () => _ = Player.Rehydrate(PlayerId.New(), UserId.New(), PlayerRole.Player, unspecifiedDateTime, isReady: false);

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Rehydrate_WhenRoleIsInvalid_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var invalidRole = (PlayerRole)999;

        // Act
        Action act = () => _ = Player.Rehydrate(PlayerId.New(), UserId.New(), invalidRole, DateTime.UtcNow, isReady: false);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    // ── ChangeRole ────────────────────────────────────────────────────────────

    [Fact]
    public void ChangeRole_WhenCalled_ReturnsNewPlayerWithUpdatedRole()
    {
        // Arrange
        var playerId = PlayerId.New();
        var userId = UserId.New();
        var createdAtUtc = DateTime.UtcNow;
        var player = Player.Create(playerId, userId, PlayerRole.Player, createdAtUtc);

        // Act
        var updated = player.ChangeRole(PlayerRole.Host);

        // Assert
        Assert.Equal(playerId, updated.Id);
        Assert.Equal(userId, updated.UserId);
        Assert.Equal(PlayerRole.Host, updated.Role);
        Assert.Equal(createdAtUtc, updated.JoinedAtUtc);
        Assert.Equal(player.IsReady, updated.IsReady);
    }

    [Fact]
    public void ChangeRole_WhenRoleIsInvalid_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var player = Player.Create(PlayerId.New(), UserId.New(), PlayerRole.Player, DateTime.UtcNow);
        var invalidRole = (PlayerRole)999;

        // Act
        Action act = () => player.ChangeRole(invalidRole);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    // ── SetReady ──────────────────────────────────────────────────────────────

    [Fact]
    public void SetReady_WhenCalledWithTrue_ReturnsNewPlayerWithIsReadyTrue()
    {
        // Arrange
        var player = Player.Create(PlayerId.New(), UserId.New(), PlayerRole.Player, DateTime.UtcNow, isReady: false);

        // Act
        var readyPlayer = player.SetReady(true);

        // Assert
        Assert.Equal(player.Id, readyPlayer.Id);
        Assert.Equal(player.UserId, readyPlayer.UserId);
        Assert.Equal(PlayerRole.Player, readyPlayer.Role);
        Assert.True(readyPlayer.IsReady);
    }

    [Fact]
    public void SetReady_WhenCalledWithFalse_ReturnsNewPlayerWithIsReadyFalse()
    {
        // Arrange
        var player = Player.Create(PlayerId.New(), UserId.New(), PlayerRole.Player, DateTime.UtcNow, isReady: true);

        // Act
        var notReadyPlayer = player.SetReady(false);

        // Assert
        Assert.False(notReadyPlayer.IsReady);
    }

    [Fact]
    public void SetReady_WhenCalled_PreservesAllOtherProperties()
    {
        // Arrange
        var playerId = PlayerId.New();
        var userId = UserId.New();
        var joinedAt = DateTime.UtcNow;
        var player = Player.Create(playerId, userId, PlayerRole.Host, joinedAt, isReady: false);

        // Act
        var readyPlayer = player.SetReady(true);

        // Assert
        Assert.Equal(playerId, readyPlayer.Id);
        Assert.Equal(userId, readyPlayer.UserId);
        Assert.Equal(PlayerRole.Host, readyPlayer.Role);
        Assert.Equal(joinedAt, readyPlayer.JoinedAtUtc);
    }
}
