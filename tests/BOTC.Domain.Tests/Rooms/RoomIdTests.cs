using BOTC.Domain.Rooms;

namespace BOTC.Domain.Tests.Rooms;

public sealed class RoomIdTests
{
    [Fact]
    public void Constructor_WhenGuidIsEmpty_ThrowsArgumentException()
    {
        // Act
        Action act = () => _ = new RoomId(Guid.Empty);

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_WhenGuidIsValid_SetsValue()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        var roomId = new RoomId(guid);

        // Assert
        Assert.Equal(guid, roomId.Value);
    }

    [Fact]
    public void New_WhenCalled_ReturnsNonEmptyRoomId()
    {
        // Act
        var roomId = RoomId.New();

        // Assert
        Assert.NotEqual(Guid.Empty, roomId.Value);
    }

    [Fact]
    public void New_WhenCalledTwice_ReturnsDifferentIds()
    {
        // Act
        var first = RoomId.New();
        var second = RoomId.New();

        // Assert
        Assert.NotEqual(first, second);
    }

    [Fact]
    public void ToString_ReturnsGuidString()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var roomId = new RoomId(guid);

        // Act & Assert
        Assert.Equal(guid.ToString(), roomId.ToString());
    }

    [Fact]
    public void Equals_WhenSameGuid_ReturnsTrue()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var left = new RoomId(guid);
        var right = new RoomId(guid);

        // Act & Assert
        Assert.Equal(left, right);
    }

    [Fact]
    public void Equals_WhenDifferentGuid_ReturnsFalse()
    {
        // Arrange
        var left = RoomId.New();
        var right = RoomId.New();

        // Act & Assert
        Assert.NotEqual(left, right);
    }

    [Fact]
    public void GetHashCode_WhenSameGuid_ReturnsSameHash()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var left = new RoomId(guid);
        var right = new RoomId(guid);

        // Act & Assert
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }

    [Fact]
    public void EqualityOperator_WhenSameGuid_ReturnsTrue()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act & Assert
        Assert.True(new RoomId(guid) == new RoomId(guid));
    }

    [Fact]
    public void InequalityOperator_WhenDifferentGuids_ReturnsTrue()
    {
        // Act & Assert
        Assert.True(RoomId.New() != RoomId.New());
    }
}

