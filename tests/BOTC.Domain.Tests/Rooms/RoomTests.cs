using BOTC.Domain.Rooms;

namespace BOTC.Domain.Tests.Rooms;

public sealed class RoomTests
{
    [Fact]
    public void Create_WhenInputIsValid_SetsAllPropertiesAndWaitingForPlayersStatus()
    {
        // Arrange
        var id = new RoomId(Guid.NewGuid());
        var code = new RoomCode("AB12CD");
        const string hostDisplayName = "Host";
        var createdAtUtc = new DateTime(2026, 3, 17, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var room = Room.Create(id, code, hostDisplayName, createdAtUtc);

        // Assert
        Assert.Equal(id, room.Id);
        Assert.Equal(code, room.Code);
        Assert.Equal(hostDisplayName, room.HostDisplayName);
        Assert.Equal(createdAtUtc, room.CreatedAtUtc);
        Assert.Equal(RoomStatus.WaitingForPlayers, room.Status);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WhenHostDisplayNameIsNullEmptyOrWhitespace_ThrowsArgumentException(string? hostDisplayName)
    {
        // Arrange
        var id = new RoomId(Guid.NewGuid());
        var code = new RoomCode("AB12CD");
        var createdAtUtc = new DateTime(2026, 3, 17, 12, 0, 0, DateTimeKind.Utc);

        // Act
        Action act = () =>
        {
            _ = Room.Create(id, code, hostDisplayName!, createdAtUtc);
        };

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Create_WhenHostDisplayNameExceedsMaximumLength_ThrowsArgumentException()
    {
        // Arrange
        var id = new RoomId(Guid.NewGuid());
        var code = new RoomCode("AB12CD");
        var tooLongHostDisplayName = new string('A', 51);
        var createdAtUtc = new DateTime(2026, 3, 17, 12, 0, 0, DateTimeKind.Utc);

        // Act
        Action act = () =>
        {
            _ = Room.Create(id, code, tooLongHostDisplayName, createdAtUtc);
        };

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Create_WhenHostDisplayNameHasSurroundingWhitespace_TrimsHostDisplayName()
    {
        // Arrange
        var id = new RoomId(Guid.NewGuid());
        var code = new RoomCode("AB12CD");
        const string hostDisplayName = "  Host Name  ";
        var createdAtUtc = new DateTime(2026, 3, 17, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var room = Room.Create(id, code, hostDisplayName, createdAtUtc);

        // Assert
        Assert.Equal("Host Name", room.HostDisplayName);
    }

    [Fact]
    public void Create_WhenCreatedAtIsLocal_ConvertsCreatedAtToUtc()
    {
        // Arrange
        var id = new RoomId(Guid.NewGuid());
        var code = new RoomCode("AB12CD");
        const string hostDisplayName = "Host";
        var localDateTime = new DateTime(2026, 3, 17, 13, 0, 0, DateTimeKind.Local);

        // Act
        var room = Room.Create(id, code, hostDisplayName, localDateTime);

        // Assert
        Assert.Equal(DateTimeKind.Utc, room.CreatedAtUtc.Kind);
        Assert.Equal(localDateTime.ToUniversalTime(), room.CreatedAtUtc);
    }

    [Fact]
    public void Create_WhenCreatedAtKindIsUnspecified_ThrowsArgumentException()
    {
        // Arrange
        var id = new RoomId(Guid.NewGuid());
        var code = new RoomCode("AB12CD");
        const string hostDisplayName = "Host";
        var unspecifiedDateTime = new DateTime(2026, 3, 17, 13, 0, 0, DateTimeKind.Unspecified);

        // Act
        Action act = () =>
        {
            _ = Room.Create(id, code, hostDisplayName, unspecifiedDateTime);
        };

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void RenameHost_WhenInputIsValid_UpdatesHostDisplayName()
    {
        // Arrange
        var room = Room.Create(
            new RoomId(Guid.NewGuid()),
            new RoomCode("AB12CD"),
            "Original Host",
            new DateTime(2026, 3, 17, 12, 0, 0, DateTimeKind.Utc));

        // Act
        room.RenameHost("New Host");

        // Assert
        Assert.Equal("New Host", room.HostDisplayName);
    }

    [Fact]
    public void RenameHost_WhenInputHasSurroundingWhitespace_TrimsHostDisplayName()
    {
        // Arrange
        var room = Room.Create(
            new RoomId(Guid.NewGuid()),
            new RoomCode("AB12CD"),
            "Original Host",
            new DateTime(2026, 3, 17, 12, 0, 0, DateTimeKind.Utc));

        // Act
        room.RenameHost("  New Host  ");

        // Assert
        Assert.Equal("New Host", room.HostDisplayName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RenameHost_WhenHostDisplayNameIsInvalid_ThrowsArgumentException(string? hostDisplayName)
    {
        // Arrange
        var room = Room.Create(
            new RoomId(Guid.NewGuid()),
            new RoomCode("AB12CD"),
            "Original Host",
            new DateTime(2026, 3, 17, 12, 0, 0, DateTimeKind.Utc));

        // Act
        Action act = () =>
        {
            room.RenameHost(hostDisplayName!);
        };

        // Assert
        Assert.Throws<ArgumentException>(act);
    }
}
