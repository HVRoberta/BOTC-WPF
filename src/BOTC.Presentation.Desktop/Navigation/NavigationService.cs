using BOTC.Contracts.Rooms;
using BOTC.Presentation.Desktop.Rooms.CreateRoom;
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

    public void NavigateToRoomLobby(CreateRoomResponse response)
    {
        var roomLobbyViewModel = serviceProvider.GetRequiredService<RoomLobbyViewModel>();
        roomLobbyViewModel.LoadRoom(response);
        mainWindowViewModel.CurrentViewModel = roomLobbyViewModel;
    }
}

