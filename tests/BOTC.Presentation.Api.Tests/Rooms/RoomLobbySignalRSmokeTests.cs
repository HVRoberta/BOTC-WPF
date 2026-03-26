using System.Reflection;
using BOTC.Contracts.Rooms;
using BOTC.Presentation.Api.Rooms;

namespace BOTC.Presentation.Api.Tests.Rooms;

public sealed class RoomLobbySignalRSmokeTests
{
    [Fact]
    public void Program_MapsRoomLobbyHubRoute()
    {
        var repositoryRoot = FindRepositoryRoot();
        var programFilePath = Path.Combine(repositoryRoot, "src", "BOTC.Presentation.Api", "Program.cs");
        var programSource = File.ReadAllText(programFilePath);

        Assert.Contains("app.MapHub<RoomLobbyHub>(RoomLobbyHubContract.HubRoute);", programSource, StringComparison.Ordinal);
    }

    [Fact]
    public void RoomLobbyGroups_ForRoom_NormalizesRoomCodeAndUsesExpectedPrefix()
    {
        var roomLobbyGroupsType = typeof(RoomLobbyHub).Assembly.GetType("BOTC.Presentation.Api.Rooms.RoomLobbyGroups", throwOnError: true)!;
        var forRoomMethod = roomLobbyGroupsType.GetMethod("ForRoom", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(forRoomMethod);

        var normalizedGroupName = (string)forRoomMethod!.Invoke(null, ["  ab12cd  "])!;

        Assert.Equal("room-lobby:AB12CD", normalizedGroupName);
    }

    [Fact]
    public void RoomLobbyGroups_ForRoom_WhenRoomCodeIsWhitespace_ThrowsArgumentException()
    {
        var roomLobbyGroupsType = typeof(RoomLobbyHub).Assembly.GetType("BOTC.Presentation.Api.Rooms.RoomLobbyGroups", throwOnError: true)!;
        var forRoomMethod = roomLobbyGroupsType.GetMethod("ForRoom", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(forRoomMethod);

        var exception = Assert.Throws<TargetInvocationException>(() => forRoomMethod!.Invoke(null, ["   "]));
        Assert.IsType<ArgumentException>(exception.InnerException);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var solutionPath = Path.Combine(directory.FullName, "BOTC.slnx");
            if (File.Exists(solutionPath))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root containing BOTC.slnx.");
    }
}

