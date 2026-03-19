using BOTC.Contracts.Rooms;

namespace BOTC.Presentation.Desktop.Navigation;

public interface INavigationService
{
    void NavigateToCreateRoom();

    void NavigateToRoomLobby(CreateRoomResponse response);
}

