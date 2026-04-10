namespace BOTC.Application.Abstractions.Realtime;

/// <summary>
/// Abstraction for notifying clients about room lobby state changes.
/// This abstraction decouples the application layer from SignalR implementation details.
/// </summary>
public interface IRoomLobbyNotifier
{
    /// <summary>
    /// Notifies connected clients that a room lobby has been updated.
    /// </summary>
    Task NotifyLobbyUpdatedAsync(string roomCode, CancellationToken cancellationToken);

    /// <summary>
    /// Notifies connected clients that a room lobby has been closed.
    /// </summary>
    Task NotifyLobbyClosedAsync(string roomCode, CancellationToken cancellationToken);
}

