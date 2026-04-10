namespace BOTC.Presentation.Api.Rooms.Realtime;

internal static class RoomLobbyGroups
{
    private const string RoomLobbyGroupPrefix = "room-lobby:";

    public static string ForRoom(string roomCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roomCode);

        return $"{RoomLobbyGroupPrefix}{NormalizeRoomCode(roomCode)}";
    }

    public static string NormalizeRoomCode(string roomCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roomCode);
        return roomCode.Trim().ToUpperInvariant();
    }
}

