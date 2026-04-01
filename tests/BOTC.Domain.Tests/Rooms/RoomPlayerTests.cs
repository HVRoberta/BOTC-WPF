using BOTC.Domain.Rooms;
using Xunit;

namespace BOTC.Domain.Tests.Rooms;

public sealed class RoomPlayerTests
{
    [Fact]
    public void Create_WhenInputIsValid_CreatesPlayerWithExpectedProperties()
    {
        // Arrange
        var playerId = RoomPlayerId.New();
        const string displayName = "TestPlayer";
        var createdAtUtc = DateTime.UtcNow;

        // Act
        var player = RoomPlayer.Create(playerId, displayName, RoomPlayerRole.Player, createdAtUtc);

        // Assert
        Assert.Equal(playerId, player.Id);
        Assert.Equal(displayName, player.DisplayName);
        Assert.Equal(RoomPlayerRole.Player, player.Role);
        Assert.Equal(createdAtUtc, player.JoinedAtUtc);
        Assert.False(player.IsReady);
    }

    [Fact]
    public void Create_WhenIsReadyProvided_SetsReadyState()
    {
        // Arrange
        var playerId = RoomPlayerId.New();
        const string displayName = "TestPlayer";
        var createdAtUtc = DateTime.UtcNow;

        // Act
        var player = RoomPlayer.Create(playerId, displayName, RoomPlayerRole.Host, createdAtUtc, isReady: true);

        // Assert
        Assert.True(player.IsReady);
    }

    [Fact]
    public void Create_WhenDisplayNameHasSurroundingWhitespace_TrimsDisplayName()
    {
        // Arrange
        var playerId = RoomPlayerId.New();
        const string displayName = "  TestPlayer  ";
        var createdAtUtc = DateTime.UtcNow;

        // Act
        var player = RoomPlayer.Create(playerId, displayName, RoomPlayerRole.Player, createdAtUtc);

        // Assert
        Assert.Equal("TestPlayer", player.DisplayName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WhenDisplayNameIsEmptyOrWhitespace_ThrowsArgumentException(string displayName)
    {
        // Arrange
        var playerId = RoomPlayerId.New();
        var createdAtUtc = DateTime.UtcNow;

        // Act
        Action act = () =>
        {
            _ = RoomPlayer.Create(playerId, displayName!, RoomPlayerRole.Player, createdAtUtc);
        };

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Create_WhenDisplayNameExceedsMaximumLength_ThrowsArgumentException()
    {
        // Arrange
        var playerId = RoomPlayerId.New();
        var tooLongDisplayName = new string('A', 51);
        var createdAtUtc = DateTime.UtcNow;

        // Act
        Action act = () =>
        {
            _ = RoomPlayer.Create(playerId, tooLongDisplayName, RoomPlayerRole.Player, createdAtUtc);
        };

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Create_WhenCreatedAtKindIsUnspecified_ThrowsArgumentException()
    {
        // Arrange
        var playerId = RoomPlayerId.New();
        var unspecifiedDateTime = new DateTime(2026, 3, 17, 13, 0, 0, DateTimeKind.Unspecified);

        // Act
        Action act = () =>
        {
            _ = RoomPlayer.Create(playerId, "Player", RoomPlayerRole.Player, unspecifiedDateTime);
        };

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Create_WhenCreatedAtIsLocal_ConvertsToUtc()
    {
        // Arrange
        var playerId = RoomPlayerId.New();
        var localDateTime = new DateTime(2026, 3, 17, 13, 0, 0, DateTimeKind.Local);

        // Act
        var player = RoomPlayer.Create(playerId, "Player", RoomPlayerRole.Player, localDateTime);

        // Assert
        Assert.Equal(DateTimeKind.Utc, player.JoinedAtUtc.Kind);
        Assert.Equal(localDateTime.ToUniversalTime(), player.JoinedAtUtc);
    }

    [Fact]
    public void NormalizeDisplayName_WhenGivenDisplayName_ReturnsUpperInvariantTrimmed()
    {
        // Arrange
        const string displayName = "  testPlayer  ";

        // Act
        var normalized = RoomPlayer.NormalizeDisplayName(displayName);

        // Assert
        Assert.Equal("TESTPLAYER", normalized);
    }

    [Fact]
    public void NormalizeDisplayName_WhenDisplayNameIsNull_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => RoomPlayer.NormalizeDisplayName(null!));
    }

    [Fact]
    public void NormalizeDisplayName_WhenDisplayNameIsWhitespace_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => RoomPlayer.NormalizeDisplayName("   "));
    }

    [Fact]
    public void WithRole_WhenCalled_CreatesNewPlayerWithUpdatedRole()
    {
        // Arrange
        var playerId = RoomPlayerId.New();
        const string displayName = "Player";
        var createdAtUtc = DateTime.UtcNow;
        var player = RoomPlayer.Create(playerId, displayName, RoomPlayerRole.Player, createdAtUtc);

        // Act
        var updatedPlayer = player.WithRole(RoomPlayerRole.Host);

        // Assert
        Assert.Equal(playerId, updatedPlayer.Id);
        Assert.Equal(displayName, updatedPlayer.DisplayName);
        Assert.Equal(RoomPlayerRole.Host, updatedPlayer.Role);
        Assert.Equal(createdAtUtc, updatedPlayer.JoinedAtUtc);
        Assert.Equal(player.IsReady, updatedPlayer.IsReady);
    }

    [Fact]
    public void WithDisplayName_WhenCalled_CreatesNewPlayerWithUpdatedDisplayName()
    {
        // Arrange
        var playerId = RoomPlayerId.New();
        var createdAtUtc = DateTime.UtcNow;
        var player = RoomPlayer.Create(playerId, "OldName", RoomPlayerRole.Player, createdAtUtc);

        // Act
        var updatedPlayer = player.WithDisplayName("NewName");

        // Assert
        Assert.Equal(playerId, updatedPlayer.Id);
        Assert.Equal("NewName", updatedPlayer.DisplayName);
        Assert.Equal(RoomPlayerRole.Player, updatedPlayer.Role);
        Assert.Equal(createdAtUtc, updatedPlayer.JoinedAtUtc);
    }

    [Fact]
    public void WithReadyState_WhenCalled_CreatesNewPlayerWithUpdatedReadiness()
    {
        // Arrange
        var playerId = RoomPlayerId.New();
        var createdAtUtc = DateTime.UtcNow;
        var player = RoomPlayer.Create(playerId, "Player", RoomPlayerRole.Player, createdAtUtc, isReady: false);

        // Act
        var readyPlayer = player.WithReadyState(true);

        // Assert
        Assert.Equal(playerId, readyPlayer.Id);
        Assert.True(readyPlayer.IsReady);
        Assert.Equal("Player", readyPlayer.DisplayName);
        Assert.Equal(RoomPlayerRole.Player, readyPlayer.Role);
    }

    [Fact]
    public void Rehydrate_WhenNormalizedDisplayNameMatches_CreatesPlayer()
    {
        // Arrange
        var playerId = RoomPlayerId.New();
        const string displayName = "TestPlayer";
        const string normalizedDisplayName = "TESTPLAYER";
        var createdAtUtc = DateTime.UtcNow;

        // Act
        var player = RoomPlayer.Rehydrate(
            playerId,
            displayName,
            normalizedDisplayName,
            RoomPlayerRole.Player,
            createdAtUtc,
            isReady: true);

        // Assert
        Assert.Equal(playerId, player.Id);
        Assert.Equal(displayName, player.DisplayName);
        Assert.Equal(normalizedDisplayName, player.NormalizedDisplayName);
        Assert.True(player.IsReady);
    }

    [Fact]
    public void Rehydrate_WhenNormalizedDisplayNameDoesNotMatch_ThrowsArgumentException()
    {
        // Arrange
        var playerId = RoomPlayerId.New();
        const string displayName = "TestPlayer";
        const string invalidNormalizedDisplayName = "WRONGNAME";
        var createdAtUtc = DateTime.UtcNow;

        // Act
        Action act = () =>
        {
            _ = RoomPlayer.Rehydrate(
                playerId,
                displayName,
                invalidNormalizedDisplayName,
                RoomPlayerRole.Player,
                createdAtUtc,
                isReady: false);
        };

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void NormalizedDisplayName_WhenPlayerCreated_IsConsistentWithDisplayName()
    {
        // Arrange
        var playerId = RoomPlayerId.New();
        const string displayName = "  TestPlayer123  ";

        // Act
        var player = RoomPlayer.Create(playerId, displayName, RoomPlayerRole.Player, DateTime.UtcNow);
        var expectedNormalized = RoomPlayer.NormalizeDisplayName(displayName);

        // Assert
        Assert.Equal(expectedNormalized, player.NormalizedDisplayName);
    }

    [Fact]
    public void Create_WhenRoleIsInvalid_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var playerId = RoomPlayerId.New();
        var invalidRole = (RoomPlayerRole)999;

        // Act
        Action act = () =>
        {
            _ = RoomPlayer.Create(playerId, "Player", invalidRole, DateTime.UtcNow);
        };

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void MaxDisplayNameLength_IsRespected_InValidation()
    {
        // Arrange
        var playerId = RoomPlayerId.New();
        var exactLengthName = new string('A', 50);

        // Act
        var player = RoomPlayer.Create(playerId, exactLengthName, RoomPlayerRole.Player, DateTime.UtcNow);

        // Assert
        Assert.Equal(50, player.DisplayName.Length);
    }
}





