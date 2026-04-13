namespace BOTC.Presentation.Desktop.Infrastructure.Session;

public sealed class ClientSessionService : IClientSessionService
{
    public string? CurrentRoomCode { get; private set; }

    public string? CurrentPlayerId { get; private set; }

    public string? Name { get; private set; }

    public bool HasActiveSession =>
        !string.IsNullOrWhiteSpace(CurrentRoomCode) &&
        !string.IsNullOrWhiteSpace(CurrentPlayerId);

    public void SetName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = NormalizeName(name);
    }

    public void SetSession(string roomCode, string playerId, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roomCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(playerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var normalizedRoomCode = NormalizeRoomCode(roomCode);
        var normalizedPlayerId = NormalizePlayerId(playerId);
        var normalizedName = NormalizeName(name);


        CurrentRoomCode = normalizedRoomCode;
        CurrentPlayerId = normalizedPlayerId;
        Name = normalizedName;
    }

    public void ClearSession()
    {
        CurrentRoomCode = null;
        CurrentPlayerId = null;
        Name = null;
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

    private static string NormalizeName(string name)
    {
        return string.IsNullOrWhiteSpace(name)
            ? string.Empty
            : name.Trim();
    }
}
