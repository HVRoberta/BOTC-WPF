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
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WhenValueIsNullOrEmptyOrWhitespace_ThrowsArgumentException(string? value)
    {
        // Act
        Action act = () => _ = new RoomCode(value!);

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Theory]
    [InlineData("ABCDE")]
    [InlineData("ABCDEFG")]
    public void Constructor_WhenCodeLengthIsInvalid_ThrowsArgumentException(string value)
    {
        // Act
        Action act = () => _ = new RoomCode(value);

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
        Action act = () => _ = new RoomCode(value);

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_WhenCodeContainsLowercaseCharacters_ThrowsArgumentException()
    {
        // Arrange
        const string lowercaseValue = "Ab12CD";

        // Act
        Action act = () => _ = new RoomCode(lowercaseValue);

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
    public void ToString_ReturnsValue()
    {
        // Arrange
        var roomCode = new RoomCode("XY34ZW");

        // Act & Assert
        Assert.Equal("XY34ZW", roomCode.ToString());
    }

    [Fact]
    public void Equals_WhenSameValue_ReturnsTrue()
    {
        // Arrange
        var left = new RoomCode("AB12CD");
        var right = new RoomCode("AB12CD");

        // Act & Assert
        Assert.Equal(left, right);
    }

    [Fact]
    public void Equals_WhenDifferentValue_ReturnsFalse()
    {
        // Arrange
        var left = new RoomCode("AB12CD");
        var right = new RoomCode("XY34ZW");

        // Act & Assert
        Assert.NotEqual(left, right);
    }

    [Fact]
    public void GetHashCode_WhenSameValue_ReturnsSameHash()
    {
        // Arrange
        var left = new RoomCode("AB12CD");
        var right = new RoomCode("AB12CD");

        // Act & Assert
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }

    [Fact]
    public void EqualityOperator_WhenSameValue_ReturnsTrue()
    {
        // Act & Assert
        Assert.True(new RoomCode("AB12CD") == new RoomCode("AB12CD"));
    }

    [Fact]
    public void InequalityOperator_WhenDifferentValues_ReturnsTrue()
    {
        // Act & Assert
        Assert.True(new RoomCode("AB12CD") != new RoomCode("XY34ZW"));
    }
}
