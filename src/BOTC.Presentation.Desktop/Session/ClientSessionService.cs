namespace BOTC.Presentation.Desktop.Session;

public sealed class ClientSessionService : IClientSessionService
{
    public string? CurrentRoomCode { get; private set; }

    public string? CurrentPlayerId { get; private set; }

    public string? DisplayName { get; private set; }

    public bool HasActiveSession =>
        !string.IsNullOrWhiteSpace(CurrentRoomCode) &&
        !string.IsNullOrWhiteSpace(CurrentPlayerId);

    public void SetSession(string roomCode, string playerId, string displayName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roomCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(playerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        var normalizedRoomCode = NormalizeRoomCode(roomCode);
        var normalizedPlayerId = NormalizePlayerId(playerId);
        var normalizedDisplayName = NormalizeDisplayName(displayName);


        CurrentRoomCode = normalizedRoomCode;
        CurrentPlayerId = normalizedPlayerId;
        DisplayName = normalizedDisplayName;
    }

    public void ClearSession()
    {
        CurrentRoomCode = null;
        CurrentPlayerId = null;
        DisplayName = null;
    }

    private static string NormalizeRoomCode(string roomCode)
    {
        return string.IsNullOrWhiteSpace(roomCode)
            ? string.Empty
            : roomCode.Trim().ToUpperInvariant();
    }

    private static string NormalizePlayerId(string playerId)
    {
        return string.IsNullOrWhiteSpace(playerId)
            ? string.Empty
            : playerId.Trim();
    }

    private static string NormalizeDisplayName(string displayName)
    {
        return string.IsNullOrWhiteSpace(displayName)
            ? string.Empty
            : displayName.Trim();
    }
}


