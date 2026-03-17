using BOTC.Domain.Rooms;
using Xunit;

namespace BOTC.Domain.Tests.Rooms;

public sealed class RoomIdTests
{
    [Fact]
    public void Constructor_WhenGuidIsEmpty_ThrowsArgumentException()
    {
        // Arrange
        var emptyGuid = Guid.Empty;

        // Act
        Action act = () =>
        {
            _ = new RoomId(emptyGuid);
        };

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_WhenGuidIsValid_SetsValue()
    {
        // Arrange
        var value = Guid.NewGuid();

        // Act
        var roomId = new RoomId(value);

        // Assert
        Assert.Equal(value, roomId.Value);
    }

    [Fact]
    public void New_WhenCalled_ReturnsNonEmptyRoomId()
    {
        // Arrange / Act
        var roomId = RoomId.New();

        // Assert
        Assert.NotEqual(Guid.Empty, roomId.Value);
    }
}
