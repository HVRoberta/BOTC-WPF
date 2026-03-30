namespace BOTC.Presentation.Desktop.Session;

public interface IClientSessionService
{
    string? CurrentRoomCode { get; }

    string? CurrentPlayerId { get; }

    string? DisplayName { get; }

    bool HasActiveSession { get; }

    void SetSession(string roomCode, string playerId, string displayName);

    void ClearSession();
}


