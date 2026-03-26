namespace BOTC.Presentation.Api.Rooms.Realtime;

public interface IRoomLobbyNotifier
{
    Task NotifyLobbyUpdatedAsync(string roomCode, CancellationToken cancellationToken);
}

