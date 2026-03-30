using BOTC.Presentation.Desktop.Rooms.CreateRoom;
using BOTC.Presentation.Desktop.Rooms.JoinRoom;
using BOTC.Presentation.Desktop.Rooms.RoomLobby;
using Microsoft.Extensions.DependencyInjection;

namespace BOTC.Presentation.Desktop.Navigation;

public sealed class NavigationService(
    MainWindowViewModel mainWindowViewModel,
    IServiceProvider serviceProvider) : INavigationService
{
    public void NavigateToCreateRoom()
    {
        mainWindowViewModel.CurrentViewModel = serviceProvider.GetRequiredService<CreateRoomViewModel>();
    }

    public void NavigateToJoinRoom()
    {
        mainWindowViewModel.CurrentViewModel = serviceProvider.GetRequiredService<JoinRoomViewModel>();
    }

    public async Task NavigateToRoomLobbyAsync(string roomCode, string playerId, CancellationToken cancellationToken)
    {
        var roomLobbyViewModel = serviceProvider.GetRequiredService<RoomLobbyViewModel>();
        mainWindowViewModel.CurrentViewModel = roomLobbyViewModel;
        await roomLobbyViewModel.LoadAsync(roomCode, playerId, cancellationToken);
    }
}
