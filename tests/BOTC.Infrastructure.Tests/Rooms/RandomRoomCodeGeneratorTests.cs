using BOTC.Domain.Rooms;
using BOTC.Infrastructure.Rooms;

namespace BOTC.Infrastructure.Tests.Rooms;

public sealed class RandomRoomCodeGeneratorTests
{
    private readonly RandomRoomCodeGenerator generator = new();

    [Fact]
    public void Generate_WhenCalled_ReturnsCodeOfLengthSix()
    {
        // Act
        var code = generator.Generate();

        // Assert
        Assert.Equal(6, code.Length);
    }

    [Fact]
    public void Generate_WhenCalled_ReturnsUppercaseAlphanumericCode()
    {
        // Act
        var code = generator.Generate();

        // Assert – all characters must be uppercase letters A-Z or digits 0-9.
        Assert.All(code, c => Assert.True(
            (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9'),
            $"Character '{c}' is not an uppercase alphanumeric character."));
    }

    [Fact]
    public void Generate_WhenCalled_ReturnsValidRoomCode()
    {
        // Act – if the generated string is not a valid RoomCode, the constructor will throw.
        var code = generator.Generate();
        Action act = () => { _ = new RoomCode(code); };

        // Assert
        var exception = Record.Exception(act);
        Assert.Null(exception);
    }

    [Fact]
    public void Generate_WhenCalledRepeatedly_ProducesDistinctCodes()
    {
        // Arrange
        const int sampleSize = 100;

        // Act
        var codes = Enumerable.Range(0, sampleSize)
            .Select(_ => generator.Generate())
            .ToHashSet();

        // Assert – with 36^6 (~2.2 billion) possible codes, 100 collisions in a row is astronomically unlikely.
        Assert.True(codes.Count > sampleSize / 2,
            $"Expected mostly distinct codes across {sampleSize} calls, but got only {codes.Count} unique codes.");
    }
}


