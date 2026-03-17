using System.Net.Http;
using BOTC.Contracts.Rooms;
using BOTC.Presentation.Desktop.Navigation;
using BOTC.Presentation.Desktop.Rooms;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BOTC.Presentation.Desktop.Rooms.CreateRoom;

public partial class CreateRoomViewModel(
    IRoomsApiClient roomsApiClient,
    INavigationService navigationService) : ObservableObject
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreateRoomCommand))]
    private string _hostDisplayName = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreateRoomCommand))]
    private bool _isBusy;

    private bool CanCreateRoom() => !IsBusy && !string.IsNullOrWhiteSpace(HostDisplayName);

    [RelayCommand(CanExecute = nameof(CanCreateRoom), AllowConcurrentExecutions = false)]
    private async Task CreateRoomAsync()
    {
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(HostDisplayName))
        {
            ErrorMessage = "Host display name is required.";
            return;
        }

        IsBusy = true;
        try
        {
            var request = new CreateRoomRequest(HostDisplayName.Trim());
            var response = await roomsApiClient.CreateRoomAsync(request, CancellationToken.None);
            navigationService.NavigateToRoomLobby(response);
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Unable to contact the server. Please try again.";
        }
        catch (Exception)
        {
            ErrorMessage = "Unexpected error occurred while creating room.";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
