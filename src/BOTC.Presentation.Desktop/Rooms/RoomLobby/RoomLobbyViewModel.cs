using System.Collections.ObjectModel;
using System.Net;
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
    private const string UnknownValue = "-";

    private string _currentRoomCode = string.Empty;
    private bool _hasLobbyData;

    public ObservableCollection<LobbyPlayerItemViewModel> Players { get; } = new();

    [ObservableProperty]
    private string _roomCode = UnknownValue;

    [ObservableProperty]
    private string _roomStatus = UnknownValue;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RefreshCommand))]
    private bool _isLoading;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RefreshCommand))]
    private bool _isRefreshing;

    [RelayCommand]
    private void BackToCreateRoom()
    {
        navigationService.NavigateToCreateRoom();
    }

    public async Task LoadAsync(string roomCode, CancellationToken cancellationToken)
    {
        _currentRoomCode = NormalizeRoomCode(roomCode);
        RefreshCommand.NotifyCanExecuteChanged();

        if (string.IsNullOrWhiteSpace(_currentRoomCode))
        {
            ResetLobbyState("Enter a valid room code to load the lobby.");
            return;
        }

        RoomCode = _currentRoomCode;
        await LoadLobbyStateAsync(cancellationToken);
    }

    private bool CanRefresh() =>
        !string.IsNullOrWhiteSpace(_currentRoomCode)
        && !IsLoading
        && !IsRefreshing;

    [RelayCommand(CanExecute = nameof(CanRefresh), AllowConcurrentExecutions = false)]
    private Task RefreshAsync()
    {
        return LoadLobbyStateAsync(CancellationToken.None);
    }

    private async Task LoadLobbyStateAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = string.Empty;

        var preserveCurrentLobbyState = _hasLobbyData;
        SetBusyState(preserveCurrentLobbyState);

        try
        {
            var response = await roomsApiClient.GetRoomLobbyAsync(_currentRoomCode, cancellationToken);
            ApplyLobbyState(response);
            _hasLobbyData = true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (HttpRequestException exception)
        {
            ErrorMessage = BuildLobbyErrorMessage(exception.StatusCode, preserveCurrentLobbyState);
        }
        catch (Exception)
        {
            ErrorMessage = preserveCurrentLobbyState
                ? "Couldn't refresh the lobby. Showing the last loaded data."
                : "Unexpected error occurred while loading the room lobby.";
        }
        finally
        {
            IsLoading = false;
            IsRefreshing = false;
        }
    }

    private void ApplyLobbyState(GetRoomLobbyResponse response)
    {
        _currentRoomCode = NormalizeRoomCode(response.RoomCode);
        RoomCode = string.IsNullOrWhiteSpace(_currentRoomCode) ? UnknownValue : _currentRoomCode;
        RoomStatus = ToDisplayStatus(response.Status);

        Players.Clear();
        foreach (var player in response.Players)
        {
            Players.Add(new LobbyPlayerItemViewModel(player.DisplayName, player.IsHost));
        }

        RefreshCommand.NotifyCanExecuteChanged();
    }

    private void SetBusyState(bool preserveCurrentLobbyState)
    {
        IsLoading = !preserveCurrentLobbyState;
        IsRefreshing = preserveCurrentLobbyState;
    }

    private void ResetLobbyState(string errorMessage)
    {
        Players.Clear();
        RoomCode = UnknownValue;
        RoomStatus = UnknownValue;
        ErrorMessage = errorMessage;
        IsLoading = false;
        IsRefreshing = false;
        _hasLobbyData = false;
        _currentRoomCode = string.Empty;
        RefreshCommand.NotifyCanExecuteChanged();
    }

    private static string NormalizeRoomCode(string roomCode)
    {
        return string.IsNullOrWhiteSpace(roomCode)
            ? string.Empty
            : roomCode.Trim().ToUpperInvariant();
    }

    private static string BuildLobbyErrorMessage(HttpStatusCode? statusCode, bool preserveCurrentLobbyState)
    {
        if (preserveCurrentLobbyState)
        {
            return statusCode switch
            {
                HttpStatusCode.NotFound => "Room was not found. Showing the last loaded data.",
                HttpStatusCode.BadRequest => "Room code is invalid. Showing the last loaded data.",
                _ => "Couldn't refresh the lobby. Showing the last loaded data."
            };
        }

        return statusCode switch
        {
            HttpStatusCode.NotFound => "Room was not found.",
            HttpStatusCode.BadRequest => "Enter a valid room code.",
            _ => "Unable to load the room lobby. Please try again."
        };
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
