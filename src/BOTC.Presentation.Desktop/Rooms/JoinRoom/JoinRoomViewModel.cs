using System.Net;
using System.Net.Http;
using BOTC.Contracts.Rooms;
using BOTC.Presentation.Desktop.Navigation;
using BOTC.Presentation.Desktop.Rooms.Shared;
using BOTC.Presentation.Desktop.Session;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BOTC.Presentation.Desktop.Rooms.JoinRoom;

public partial class JoinRoomViewModel(
    IRoomsApiClient roomsApiClient,
    IClientSessionService clientSessionService,
    INavigationService navigationService) : ObservableObject
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(JoinRoomCommand))]
    private string _roomCode = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(JoinRoomCommand))]
    private string _displayName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ErrorMessage))]
    [NotifyPropertyChangedFor(nameof(HasScreenMessage))]
    private string _screenMessage = string.Empty;

    [ObservableProperty]
    private ScreenMessageKind _screenMessageKind = ScreenMessageKind.None;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(JoinRoomCommand))]
    [NotifyCanExecuteChangedFor(nameof(BackToCreateRoomCommand))]
    private bool _isBusy;

    public string ErrorMessage => ScreenMessage;

    public bool HasScreenMessage => !string.IsNullOrWhiteSpace(ScreenMessage);

    public string BusyText => "Joining room...";

    private bool CanJoinRoom() =>
        !IsBusy
        && !string.IsNullOrWhiteSpace(RoomCode)
        && !string.IsNullOrWhiteSpace(DisplayName);

    private bool CanBackToCreateRoom() => !IsBusy;

    [RelayCommand(CanExecute = nameof(CanJoinRoom), AllowConcurrentExecutions = false)]
    private async Task JoinRoomAsync()
    {
        ClearScreenMessage();

        if (string.IsNullOrWhiteSpace(RoomCode))
        {
            ShowErrorMessage("Room code is required.");
            return;
        }

        if (string.IsNullOrWhiteSpace(DisplayName))
        {
            ShowErrorMessage("Display name is required.");
            return;
        }

        IsBusy = true;
        try
        {
            var request = new JoinRoomRequest(DisplayName.Trim());
            var response = await roomsApiClient.JoinRoomAsync(RoomCode.Trim(), request, CancellationToken.None);
            clientSessionService.SetSession(response.RoomCode, response.PlayerId, response.DisplayName);
            await navigationService.NavigateToRoomLobbyAsync(CancellationToken.None);
        }
        catch (HttpRequestException exception) when (exception.StatusCode == HttpStatusCode.BadRequest)
        {
            ShowErrorMessage("Enter a valid room code and display name.");
        }
        catch (HttpRequestException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            ShowErrorMessage("Room was not found.");
        }
        catch (HttpRequestException exception) when (exception.StatusCode == HttpStatusCode.Conflict)
        {
            ShowErrorMessage("Unable to join room due to a conflict. Please try again.");
        }
        catch (HttpRequestException)
        {
            ShowErrorMessage("Unable to contact the server. Please try again.");
        }
        catch (Exception)
        {
            ShowErrorMessage("Unexpected error occurred while joining room.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanBackToCreateRoom))]
    private void BackToCreateRoom()
    {
        navigationService.NavigateToCreateRoom();
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
