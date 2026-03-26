namespace BOTC.Presentation.Desktop.Rooms.RoomLobby;

public interface IRoomLobbyRealtimeClient : IAsyncDisposable
{
    event Func<string, Task>? LobbyUpdated;

    Task SubscribeAsync(string roomCode, CancellationToken cancellationToken);

    Task UnsubscribeAsync(string roomCode, CancellationToken cancellationToken);
}

