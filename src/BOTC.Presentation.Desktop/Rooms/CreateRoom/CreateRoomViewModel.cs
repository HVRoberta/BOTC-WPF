using System.Net.Http;
using BOTC.Contracts.Rooms;
using BOTC.Presentation.Desktop.Navigation;
using BOTC.Presentation.Desktop.Rooms.Shared;
using BOTC.Presentation.Desktop.Session;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BOTC.Presentation.Desktop.Rooms.CreateRoom;

public partial class CreateRoomViewModel(
    IRoomsApiClient roomsApiClient,
    IClientSessionService clientSessionService,
    INavigationService navigationService) : ObservableObject
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreateRoomCommand))]
    private string _hostDisplayName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ErrorMessage))]
    [NotifyPropertyChangedFor(nameof(HasScreenMessage))]
    private string _screenMessage = string.Empty;

    [ObservableProperty]
    private ScreenMessageKind _screenMessageKind = ScreenMessageKind.None;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreateRoomCommand))]
    [NotifyCanExecuteChangedFor(nameof(NavigateToJoinRoomCommand))]
    private bool _isBusy;

    public string ErrorMessage => ScreenMessage;

    public bool HasScreenMessage => !string.IsNullOrWhiteSpace(ScreenMessage);

    public string BusyText => "Creating room...";

    private bool CanCreateRoom() => !IsBusy && !string.IsNullOrWhiteSpace(HostDisplayName);

    private bool CanNavigateToJoinRoom() => !IsBusy;

    [RelayCommand(CanExecute = nameof(CanCreateRoom), AllowConcurrentExecutions = false)]
    private async Task CreateRoomAsync()
    {
        ClearScreenMessage();

        if (string.IsNullOrWhiteSpace(HostDisplayName))
        {
            ShowErrorMessage("Host display name is required.");
            return;
        }

        IsBusy = true;
        try
        {
            var request = new CreateRoomRequest(HostDisplayName.Trim());
            var response = await roomsApiClient.CreateRoomAsync(request, CancellationToken.None);
            clientSessionService.SetSession(response.RoomCode, response.PlayerId, request.HostDisplayName);
            navigationService.NavigateToRoomLobby();
        }
        catch (HttpRequestException)
        {
            ShowErrorMessage("Unable to contact the server. Please try again.");
        }
        catch (Exception)
        {
            ShowErrorMessage("Unexpected error occurred while creating room.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanNavigateToJoinRoom))]
    private void NavigateToJoinRoom()
    {
        navigationService.NavigateToJoinRoom();
    }

    private void ClearScreenMessage()
    {
        ScreenMessage = string.Empty;
        ScreenMessageKind = ScreenMessageKind.None;
    }

    private void ShowErrorMessage(string message)
    {
        ScreenMessage = message;
        ScreenMessageKind = ScreenMessageKind.Error;
    }
}
