using BOTC.Presentation.Desktop.Features.Entry;
using BOTC.Presentation.Desktop.Features.Rooms.CreateRoom;
using BOTC.Presentation.Desktop.Features.Rooms.JoinRoom;
using BOTC.Presentation.Desktop.Features.Rooms.RoomLobby;
using Microsoft.Extensions.DependencyInjection;

namespace BOTC.Presentation.Desktop.Infrastructure.Navigation;

public sealed class NavigationService(
    App.MainWindowViewModel mainWindowViewModel,
    IServiceProvider serviceProvider) : INavigationService
{
    public void NavigateToEntry()
    {
        mainWindowViewModel.CurrentViewModel = serviceProvider.GetRequiredService<EntryViewModel>();
    }
    
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
