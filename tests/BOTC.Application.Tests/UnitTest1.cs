using BOTC.Application.Features.Rooms.CreateRoom;
using BOTC.Domain.Rooms;

namespace BOTC.Application.Tests;

public sealed class CreateRoomContractsTests
{
    [Fact]
    public void CreateRoomCommand_WhenConstructed_PreservesHostDisplayName()
    {
        // Arrange
        const string hostDisplayName = "  Host  ";

        // Act
        var command = new CreateRoomCommand(hostDisplayName);

        // Assert
        Assert.Equal(hostDisplayName, command.HostDisplayName);
    }

    [Fact]
    public void CreateRoomResult_WhenConstructed_ExposesProvidedValues()
    {
        // Arrange
        var roomId = new RoomId(Guid.NewGuid());
        var roomCode = new RoomCode("AB12CD");
        var hostPlayerId = RoomPlayerId.New();
        const string hostDisplayName = "Host";
        const RoomStatus status = RoomStatus.WaitingForPlayers;
        var createdAtUtc = new DateTime(2026, 3, 17, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var result = new CreateRoomResult(roomId, roomCode, hostPlayerId, hostDisplayName, status, createdAtUtc);

        // Assert
        Assert.Equal(roomId, result.RoomId);
        Assert.Equal(roomCode, result.RoomCode);
        Assert.Equal(hostPlayerId, result.HostPlayerId);
        Assert.Equal(hostDisplayName, result.HostDisplayName);
        Assert.Equal(status, result.Status);
        Assert.Equal(createdAtUtc, result.CreatedAtUtc);
    }

    [Fact]
    public void RoomCodeGenerationExhaustedException_WhenCreated_HasExpectedMessageAndType()
    {
        // Arrange
        const int attempts = 10;

        // Act
        var exception = new RoomCodeGenerationExhaustedException(attempts);

        // Assert
        Assert.IsType<RoomCodeGenerationExhaustedException>(exception);
        Assert.IsAssignableFrom<InvalidOperationException>(exception);
        Assert.Contains(attempts.ToString(), exception.Message);
    }
}
