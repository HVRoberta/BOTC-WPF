using System.Threading;
using System.Threading.Tasks;

namespace BOTC.Presentation.Desktop.Navigation;

public interface INavigationService
{
    void NavigateToCreateRoom();

    Task NavigateToRoomLobbyAsync(string roomCode, CancellationToken cancellationToken);
}
