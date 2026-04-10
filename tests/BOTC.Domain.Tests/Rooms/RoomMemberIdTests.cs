using BOTC.Domain.Rooms.Players;

namespace BOTC.Domain.Tests.Rooms;

public sealed class PlayerIdTests
{
    [Fact]
    public void Constructor_WhenGuidIsEmpty_ThrowsArgumentException()
    {
        // Act
        Action act = () => _ = new PlayerId(Guid.Empty);

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_WhenGuidIsValid_SetsValue()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        var id = new PlayerId(guid);

        // Assert
        Assert.Equal(guid, id.Value);
    }

    [Fact]
    public void New_WhenCalled_ReturnsIdWithNonEmptyGuid()
    {
        // Act
        var id = PlayerId.New();

        // Assert
        Assert.NotEqual(Guid.Empty, id.Value);
    }

    [Fact]
    public void New_WhenCalledTwice_ReturnsDifferentIds()
    {
        // Act
        var first = PlayerId.New();
        var second = PlayerId.New();

        // Assert
        Assert.NotEqual(first, second);
    }

    [Fact]
    public void ToString_ReturnsGuidString()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var id = new PlayerId(guid);

        // Act & Assert
        Assert.Equal(guid.ToString(), id.ToString());
    }

    [Fact]
    public void Equals_WhenOtherHasSameGuid_ReturnsTrue()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var left = new PlayerId(guid);
        var right = new PlayerId(guid);

        // Act & Assert
        Assert.True(left.Equals(right));
    }

    [Fact]
    public void Equals_WhenOtherHasDifferentGuid_ReturnsFalse()
    {
        // Arrange
        var left = PlayerId.New();
        var right = PlayerId.New();

        // Act & Assert
        Assert.False(left.Equals(right));
    }

    [Fact]
    public void Equals_WhenOtherIsNull_ReturnsFalse()
    {
        // Arrange
        var id = PlayerId.New();

        // Act & Assert
        Assert.False(id.Equals(null));
    }

    [Fact]
    public void EqualityOperator_WhenBothHaveSameGuid_ReturnsTrue()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act & Assert
        Assert.True(new PlayerId(guid) == new PlayerId(guid));
    }

    [Fact]
    public void EqualityOperator_WhenValuesAreDifferent_ReturnsFalse()
    {
        // Act & Assert
        Assert.False(PlayerId.New() == PlayerId.New());
    }

    [Fact]
    public void InequalityOperator_WhenValuesAreDifferent_ReturnsTrue()
    {
        // Act & Assert
        Assert.True(PlayerId.New() != PlayerId.New());
    }

    [Fact]
    public void InequalityOperator_WhenValuesAreEqual_ReturnsFalse()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act & Assert
        Assert.False(new PlayerId(guid) != new PlayerId(guid));
    }

    [Fact]
    public void GetHashCode_WhenSameGuid_ReturnsSameHash()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var left = new PlayerId(guid);
        var right = new PlayerId(guid);

        // Act & Assert
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }
}
