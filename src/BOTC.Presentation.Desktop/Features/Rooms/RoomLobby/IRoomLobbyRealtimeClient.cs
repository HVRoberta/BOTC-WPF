namespace BOTC.Presentation.Desktop.Features.Rooms.RoomLobby;

public interface IRoomLobbyRealtimeClient : IAsyncDisposable
{
    event Func<string, Task>? LobbyUpdated;

    event Func<string, Task>? LobbyClosed;

    event Action<RealtimeConnectionState>? ConnectionStateChanged;

    RealtimeConnectionState ConnectionState { get; }

    Task SubscribeAsync(string roomCode, CancellationToken cancellationToken);

    Task UnsubscribeAsync(string roomCode, CancellationToken cancellationToken);
}
