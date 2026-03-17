using BOTC.Domain.Rooms;

namespace BOTC.Domain.Tests.Rooms;

public sealed class RoomCodeTests
{
    [Fact]
    public void Constructor_WhenCodeIsSixCharacterUppercaseAlphanumeric_AcceptsCode()
    {
        // Arrange
        const string value = "AB12CD";

        // Act
        var roomCode = new RoomCode(value);

        // Assert
        Assert.Equal(value, roomCode.Value);
    }

    [Theory]
    [InlineData("ABCDE")]
    [InlineData("ABCDEFG")]
    public void Constructor_WhenCodeLengthIsInvalid_ThrowsArgumentException(string value)
    {
        // Act
        Action act = () =>
        {
            _ = new RoomCode(value);
        };

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Theory]
    [InlineData("AB-2CD")]
    [InlineData("AB12C_")]
    [InlineData("AB12C!")]
    public void Constructor_WhenCodeContainsInvalidCharacters_ThrowsArgumentException(string value)
    {
        // Act
        Action act = () =>
        {
            _ = new RoomCode(value);
        };

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_WhenCodeHasSurroundingWhitespace_TrimsWhitespace()
    {
        // Arrange
        const string valueWithWhitespace = "  AB12CD  ";

        // Act
        var roomCode = new RoomCode(valueWithWhitespace);

        // Assert
        Assert.Equal("AB12CD", roomCode.Value);
    }

    [Fact]
    public void Constructor_WhenCodeContainsLowercaseCharacters_ThrowsArgumentException_CurrentBehavior()
    {
        // Arrange
        const string lowercaseValue = "Ab12CD";

        // Act
        Action act = () =>
        {
            _ = new RoomCode(lowercaseValue);
        };

        // Assert
        Assert.Throws<ArgumentException>(act);
    }
}
