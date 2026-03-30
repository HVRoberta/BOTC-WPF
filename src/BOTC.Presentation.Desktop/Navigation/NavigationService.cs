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

    public void NavigateToRoomLobby()
    {
        mainWindowViewModel.CurrentViewModel = serviceProvider.GetRequiredService<RoomLobbyViewModel>();
    }
}
