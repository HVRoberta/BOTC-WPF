namespace BOTC.Presentation.Api.Rooms.Realtime;

public interface IRoomLobbyNotifier
{
    Task NotifyLobbyUpdatedAsync(string roomCode, CancellationToken cancellationToken);
    Task NotifyLobbyClosedAsync(string roomCode, CancellationToken cancellationToken);
}
