using System.Net.Http;
using BOTC.Contracts.Rooms;
using BOTC.Presentation.Desktop.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BOTC.Presentation.Desktop.Rooms.RoomLobby;

public partial class RoomLobbyViewModel(
    IRoomsApiClient roomsApiClient,
    INavigationService navigationService) : ObservableObject
{
    [ObservableProperty]
    private string _roomCode = "-";

    [ObservableProperty]
    private string _hostDisplayName = "-";

    [ObservableProperty]
    private string _roomStatus = "-";

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [RelayCommand]
    private void BackToCreateRoom()
    {
        navigationService.NavigateToCreateRoom();
    }

    public async Task LoadAsync(string roomCode, CancellationToken cancellationToken)
    {
        ErrorMessage = string.Empty;
        RoomCode = string.IsNullOrWhiteSpace(roomCode) ? "-" : roomCode.Trim();
        HostDisplayName = "-";
        RoomStatus = "-";

        IsBusy = true;
        try
        {
            var response = await roomsApiClient.GetRoomLobbyAsync(roomCode, cancellationToken);
            RoomCode = response.RoomCode;
            HostDisplayName = response.HostDisplayName;
            RoomStatus = ToDisplayStatus(response.Status);
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Unable to load room lobby from server.";
        }
        catch (Exception)
        {
            ErrorMessage = "Unexpected error occurred while loading room lobby.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static string ToDisplayStatus(RoomStatusContract status)
    {
        return status switch
        {
            RoomStatusContract.WaitingForPlayers => "Waiting for players",
            _ => "Unknown"
        };
    }
}
