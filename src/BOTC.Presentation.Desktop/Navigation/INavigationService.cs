namespace BOTC.Presentation.Desktop.Navigation;

public interface INavigationService
{
    void NavigateToCreateRoom();

    void NavigateToJoinRoom();

    Task NavigateToRoomLobbyAsync(CancellationToken cancellationToken);
}
