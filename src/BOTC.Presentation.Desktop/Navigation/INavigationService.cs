﻿using System.Threading;
using System.Threading.Tasks;

namespace BOTC.Presentation.Desktop.Navigation;

public interface INavigationService
{
    void NavigateToCreateRoom();

    void NavigateToJoinRoom();

    Task NavigateToRoomLobbyAsync(string roomCode, string playerId, CancellationToken cancellationToken);
}
