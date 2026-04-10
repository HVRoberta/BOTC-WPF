using BOTC.Domain.Rooms.Players;
using BOTC.Domain.Users;
using Xunit;

namespace BOTC.Domain.Tests.Rooms;

public sealed class PlayerTests
{
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
        var createdAtUtc = DateTime.UtcNow;

        // Act
        var player = Player.Create(playerId, userId, PlayerRole.Host, createdAtUtc, isReady: true);

        // Assert
        Assert.True(player.IsReady);
    }

    [Fact]
    public void Create_WhenCreatedAtKindIsUnspecified_ThrowsArgumentException()
    {
        // Arrange
        var playerId = PlayerId.New();
        var userId = UserId.New();
        var unspecifiedDateTime = new DateTime(2026, 3, 17, 13, 0, 0, DateTimeKind.Unspecified);

        // Act
        Action act = () =>
        {
            _ = Player.Create(playerId, userId, PlayerRole.Player, unspecifiedDateTime);
        };

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Create_WhenCreatedAtIsLocal_ConvertsToUtc()
    {
        // Arrange
        var playerId = PlayerId.New();
        var userId = UserId.New();
        var localDateTime = new DateTime(2026, 3, 17, 13, 0, 0, DateTimeKind.Local);

        // Act
        var player = Player.Create(playerId, userId, PlayerRole.Player, localDateTime);

        // Assert
        Assert.Equal(DateTimeKind.Utc, player.JoinedAtUtc.Kind);
        Assert.Equal(localDateTime.ToUniversalTime(), player.JoinedAtUtc);
    }

    [Fact]
    public void Create_WhenRoleIsInvalid_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var playerId = PlayerId.New();
        var userId = UserId.New();
        var invalidRole = (PlayerRole)999;

        // Act
        Action act = () =>
        {
            _ = Player.Create(playerId, userId, invalidRole, DateTime.UtcNow);
        };

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

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
    public void SetReady_WhenCalledWithTrue_ReturnsNewPlayerWithIsReadyTrue()
    {
        // Arrange
        var player = Player.Create(PlayerId.New(), UserId.New(), PlayerRole.Player, DateTime.UtcNow, isReady: false);

        // Act
        var readyPlayer = player.SetReady(true);

        // Assert
        Assert.Equal(player.Id, readyPlayer.Id);
        Assert.Equal(player.UserId, readyPlayer.UserId);
        Assert.True(readyPlayer.IsReady);
        Assert.Equal(PlayerRole.Player, readyPlayer.Role);
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
}
