using BOTC.Application.Features.Rooms.CreateRoom;
using BOTC.Domain.Rooms.Players;
using BOTC.Domain.Rooms;
using BOTC.Domain.Users;

namespace BOTC.Application.Tests;

public sealed class CreateRoomContractsTests
{
    [Fact]
    public void CreateRoomCommand_WhenConstructed_PreservesHostUserIdAndRoomName()
    {
        // Arrange
        var hostUserId = UserId.New();
        const string roomName = "Test Room";

        // Act
        var command = new CreateRoomCommand(hostUserId, roomName);

        // Assert
        Assert.Equal(hostUserId, command.HostUserId);
        Assert.Equal(roomName, command.RoomName);
    }

    [Fact]
    public void CreateRoomResult_WhenConstructed_ExposesProvidedValues()
    {
        // Arrange
        var roomId = new RoomId(Guid.NewGuid());
        var roomCode = new RoomCode("AB12CD");
        const string roomName = "Test Room";
        const RoomStatus status = RoomStatus.WaitingForPlayers;
        var hostPlayerId = PlayerId.New();
        var createdAtUtc = new DateTime(2026, 3, 17, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var result = new CreateRoomResult(roomId, roomCode, roomName, status, hostPlayerId, createdAtUtc);

        // Assert
        Assert.Equal(roomId, result.RoomId);
        Assert.Equal(roomCode, result.RoomCode);
        Assert.Equal(roomName, result.RoomName);
        Assert.Equal(status, result.Status);
        Assert.Equal(hostPlayerId, result.HostPlayerId);
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
