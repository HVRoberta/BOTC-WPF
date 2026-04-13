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
        var userId = new UserId(guid);

        // Assert
        Assert.Equal(guid, userId.Value);
    }

    [Fact]
    public void New_WhenCalled_ReturnsIdWithNonEmptyGuid()
    {
        // Act
        var userId = UserId.New();

        // Assert
        Assert.NotEqual(Guid.Empty, userId.Value);
    }

    [Fact]
    public void New_WhenCalledTwice_ReturnsDifferentIds()
    {
        // Act
        var first = UserId.New();
        var second = UserId.New();

        // Assert
        Assert.NotEqual(first, second);
    }

    [Fact]
    public void ToString_ReturnsGuidString()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var userId = new UserId(guid);

        // Act & Assert
        Assert.Equal(guid.ToString(), userId.ToString());
    }

    [Fact]
    public void Equals_WhenSameGuid_ReturnsTrue()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var left = new UserId(guid);
        var right = new UserId(guid);

        // Act & Assert
        Assert.Equal(left, right);
    }

    [Fact]
    public void Equals_WhenDifferentGuid_ReturnsFalse()
    {
        // Arrange
        var left = UserId.New();
        var right = UserId.New();

        // Act & Assert
        Assert.NotEqual(left, right);
    }

    [Fact]
    public void GetHashCode_WhenSameGuid_ReturnsSameHash()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var left = new UserId(guid);
        var right = new UserId(guid);

        // Act & Assert
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }

    [Fact]
    public void EqualityOperator_WhenSameGuid_ReturnsTrue()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act & Assert
        Assert.True(new UserId(guid) == new UserId(guid));
    }

    [Fact]
    public void InequalityOperator_WhenDifferentGuids_ReturnsTrue()
    {
        // Act & Assert
        Assert.True(UserId.New() != UserId.New());
    }
}
