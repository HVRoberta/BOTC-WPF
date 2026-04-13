using System.Net.Http;
using BOTC.Contracts.Rooms;
using BOTC.Domain.Users;
using BOTC.Presentation.Desktop.Features.Rooms.Shared;
using BOTC.Presentation.Desktop.Infrastructure.Navigation;
using BOTC.Presentation.Desktop.Infrastructure.Session;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BOTC.Presentation.Desktop.Features.Rooms.CreateRoom;

public partial class CreateRoomViewModel(
    IRoomsApiClient roomsApiClient,
    IClientSessionService clientSessionService,
    INavigationService navigationService) : ObservableObject
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreateRoomCommand))]
    private string _hostName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasScreenMessage))]
    private string _screenMessage = string.Empty;

    [ObservableProperty]
    private ScreenMessageKind _screenMessageKind = ScreenMessageKind.None;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreateRoomCommand))]
    [NotifyCanExecuteChangedFor(nameof(NavigateToJoinRoomCommand))]
    private bool _isBusy;


    public bool HasScreenMessage => !string.IsNullOrWhiteSpace(ScreenMessage);

    public string BusyText => "Creating room...";

    private bool CanCreateRoom() => !IsBusy && !string.IsNullOrWhiteSpace(HostName);

    private bool CanNavigateToJoinRoom() => !IsBusy;

    [RelayCommand(CanExecute = nameof(CanCreateRoom), AllowConcurrentExecutions = false)]
    private async Task CreateRoomAsync()
    {
        ClearScreenMessage();

        if (string.IsNullOrWhiteSpace(HostName))
        {
            ShowErrorMessage("Host name is required.");
            return;
        }

        IsBusy = true;
        try
        {
            var request = new CreateRoomRequest(UserId.New(), HostName.Trim());
            var response = await roomsApiClient.CreateRoomAsync(request, CancellationToken.None);
            clientSessionService.SetSession(response.RoomCode, response.PlayerId, request.HostName);
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
