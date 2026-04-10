using BOTC.Domain.Users;

namespace BOTC.Domain.Tests.Players;

public sealed class UserIdTests
{
    [Fact]
    public void Constructor_WhenGuidIsEmpty_ThrowsArgumentException()
    {
        // Act
        Action act = () => _ = new UserId(Guid.Empty);

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_WhenGuidIsValid_SetsValue()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        var playerId = new UserId(guid);

        // Assert
        Assert.Equal(guid, playerId.Value);
    }

    [Fact]
    public void New_WhenCalled_ReturnsIdWithNonEmptyGuid()
    {
        // Act
        var playerId = UserId.New();

        // Assert
        Assert.NotEqual(Guid.Empty, playerId.Value);
    }

    [Fact]
    public void New_WhenCalledTwice_ReturnsDifferentIds()
    {
        // Act
        var first = UserId.New();
        var second = UserId.New();

        // Assert
        Assert.NotEqual(first.Value, second.Value);
    }

    [Fact]
    public void ToString_ReturnsGuidString()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var playerId = new UserId(guid);

        // Act & Assert
        Assert.Equal(guid.ToString(), playerId.ToString());
    }
}

