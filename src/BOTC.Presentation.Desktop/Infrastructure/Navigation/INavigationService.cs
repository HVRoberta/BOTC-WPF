namespace BOTC.Presentation.Desktop.Infrastructure.Navigation;

public interface INavigationService
{
    void NavigateToEntry();
    
    void NavigateToCreateRoom();

    void NavigateToJoinRoom();

    void NavigateToRoomLobby();
}
