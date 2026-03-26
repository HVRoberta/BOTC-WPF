using System.Net;
using System.Net.Http;
using BOTC.Contracts.Rooms;
using BOTC.Presentation.Desktop.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BOTC.Presentation.Desktop.Rooms.JoinRoom;

public partial class JoinRoomViewModel(
    IRoomsApiClient roomsApiClient,
    INavigationService navigationService) : ObservableObject
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(JoinRoomCommand))]
    private string _roomCode = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(JoinRoomCommand))]
    private string _displayName = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(JoinRoomCommand))]
    private bool _isBusy;

    private bool CanJoinRoom() =>
        !IsBusy
        && !string.IsNullOrWhiteSpace(RoomCode)
        && !string.IsNullOrWhiteSpace(DisplayName);

    [RelayCommand(CanExecute = nameof(CanJoinRoom), AllowConcurrentExecutions = false)]
    private async Task JoinRoomAsync()
    {
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(RoomCode))
        {
            ErrorMessage = "Room code is required.";
            return;
        }

        if (string.IsNullOrWhiteSpace(DisplayName))
        {
            ErrorMessage = "Display name is required.";
            return;
        }

        IsBusy = true;
        try
        {
            var request = new JoinRoomRequest(DisplayName.Trim());
            var response = await roomsApiClient.JoinRoomAsync(RoomCode.Trim(), request, CancellationToken.None);
            await navigationService.NavigateToRoomLobbyAsync(response.RoomCode, CancellationToken.None);
        }
        catch (HttpRequestException exception) when (exception.StatusCode == HttpStatusCode.BadRequest)
        {
            ErrorMessage = "Enter a valid room code and display name.";
        }
        catch (HttpRequestException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            ErrorMessage = "Room was not found.";
        }
        catch (HttpRequestException exception) when (exception.StatusCode == HttpStatusCode.Conflict)
        {
            ErrorMessage = "Unable to join room with that display name.";
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Unable to contact the server. Please try again.";
        }
        catch (Exception)
        {
            ErrorMessage = "Unexpected error occurred while joining room.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void BackToCreateRoom()
    {
        navigationService.NavigateToCreateRoom();
    }
}

