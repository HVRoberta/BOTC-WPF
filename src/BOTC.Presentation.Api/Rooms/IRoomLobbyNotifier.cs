﻿namespace BOTC.Presentation.Api.Rooms;

public interface IRoomLobbyNotifier
{
    Task NotifyLobbyUpdatedAsync(string roomCode, CancellationToken cancellationToken);

    Task NotifyLobbyClosedAsync(string roomCode, CancellationToken cancellationToken);
}
