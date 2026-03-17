using BOTC.Contracts.Rooms;
using BOTC.Presentation.Desktop.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BOTC.Presentation.Desktop.Rooms.RoomLobby;

public partial class RoomLobbyViewModel(INavigationService navigationService) : ObservableObject
{
    [ObservableProperty]
    private string _roomId = "-";

    [ObservableProperty]
    private string _roomCode = "-";

    [ObservableProperty]
    private string _createdAtUtc = "-";

    [RelayCommand]
    private void BackToCreateRoom()
    {
        navigationService.NavigateToCreateRoom();
    }

    public void LoadRoom(CreateRoomResponse response)
    {
        RoomId = response.RoomId;
        RoomCode = response.RoomCode;
        CreatedAtUtc = response.CreatedAtUtc.ToString("u");
    }
}
