namespace BOTC.Presentation.Desktop.Infrastructure.Session;

public interface IClientSessionService
{
    string? CurrentRoomCode { get; }

    string? CurrentPlayerId { get; }

    string? Name { get; }

    bool HasActiveSession { get; }

    void SetName(string name);

    void SetSession(string roomCode, string playerId, string name);

    void ClearSession();
}


