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
    IRoomLobbyRealtimeClient roomLobbyRealtimeClient,
    INavigationService navigationService) : ObservableObject
{
    private const string UnknownValue = "-";

    private readonly SynchronizationContext? _capturedSynchronizationContext = SynchronizationContext.Current;
    private string _currentRoomCode = string.Empty;
    private string _subscribedRoomCode = string.Empty;
    private bool _hasLobbyData;
    private bool _isLobbyActive;
    private bool _isRealtimeHandlerAttached;
    private bool _refreshRequested;
    private bool _isRefreshLoopRunning;

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
            await StopRealtimeUpdatesAsync(CancellationToken.None);
            ResetLobbyState("Enter a valid room code to load the lobby.");
            return;
        }

        RoomCode = _currentRoomCode;
        await EnsureRealtimeSubscriptionAsync(cancellationToken);
        await QueueLobbyRefreshAsync(cancellationToken);
    }

    public async Task ActivateAsync(CancellationToken cancellationToken)
    {
        _isLobbyActive = true;
        AttachRealtimeHandler();

        try
        {
            await EnsureRealtimeSubscriptionAsync(cancellationToken);
        }
        catch
        {
            // Realtime is best-effort; HTTP remains the source of truth for lobby state.
        }
    }

    public async Task DeactivateAsync(CancellationToken cancellationToken)
    {
        _isLobbyActive = false;
        DetachRealtimeHandler();

        try
        {
            await StopRealtimeUpdatesAsync(cancellationToken);
        }
        catch
        {
            // Best-effort cleanup to avoid leaving background connections running.
        }
    }

    private bool CanRefresh() =>
        !string.IsNullOrWhiteSpace(_currentRoomCode)
        && !IsLoading
        && !IsRefreshing;

    [RelayCommand(CanExecute = nameof(CanRefresh), AllowConcurrentExecutions = false)]
    private Task RefreshAsync()
    {
        return QueueLobbyRefreshAsync(CancellationToken.None);
    }

    private Task HandleLobbyUpdatedAsync(string roomCode)
    {
        var normalizedRoomCode = NormalizeRoomCode(roomCode);
        if (!_isLobbyActive || !string.Equals(normalizedRoomCode, _currentRoomCode, StringComparison.Ordinal))
        {
            return Task.CompletedTask;
        }

        return QueueLobbyRefreshAsync(CancellationToken.None);
    }

    private async Task QueueLobbyRefreshAsync(CancellationToken cancellationToken)
    {
        await RunOnCapturedContextAsync(async () =>
        {
            _refreshRequested = true;
            if (_isRefreshLoopRunning)
            {
                return;
            }

            _isRefreshLoopRunning = true;
            try
            {
                while (_refreshRequested)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    _refreshRequested = false;
                    await ExecuteLobbyRefreshAsync(cancellationToken);
                }
            }
            finally
            {
                _isRefreshLoopRunning = false;
            }
        });
    }

    private async Task ExecuteLobbyRefreshAsync(CancellationToken cancellationToken)
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

    private async Task EnsureRealtimeSubscriptionAsync(CancellationToken cancellationToken)
    {
        if (!_isLobbyActive || string.IsNullOrWhiteSpace(_currentRoomCode))
        {
            return;
        }

        if (string.Equals(_subscribedRoomCode, _currentRoomCode, StringComparison.Ordinal))
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(_subscribedRoomCode))
        {
            await roomLobbyRealtimeClient.UnsubscribeAsync(_subscribedRoomCode, cancellationToken);
        }

        await roomLobbyRealtimeClient.SubscribeAsync(_currentRoomCode, cancellationToken);
        _subscribedRoomCode = _currentRoomCode;
    }

    private async Task StopRealtimeUpdatesAsync(CancellationToken cancellationToken)
    {
        await roomLobbyRealtimeClient.UnsubscribeAsync(_subscribedRoomCode, cancellationToken);
        _subscribedRoomCode = string.Empty;
    }

    private void AttachRealtimeHandler()
    {
        if (_isRealtimeHandlerAttached)
        {
            return;
        }

        roomLobbyRealtimeClient.LobbyUpdated += HandleLobbyUpdatedAsync;
        _isRealtimeHandlerAttached = true;
    }

    private void DetachRealtimeHandler()
    {
        if (!_isRealtimeHandlerAttached)
        {
            return;
        }

        roomLobbyRealtimeClient.LobbyUpdated -= HandleLobbyUpdatedAsync;
        _isRealtimeHandlerAttached = false;
    }

    private async Task RunOnCapturedContextAsync(Func<Task> callback)
    {
        if (_capturedSynchronizationContext is null || SynchronizationContext.Current == _capturedSynchronizationContext)
        {
            await callback();
            return;
        }

        var completionSource = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        _capturedSynchronizationContext.Post(static state =>
        {
            var callbackState = (PostedCallbackState)state!;
            _ = callbackState.ExecuteAsync();
        }, new PostedCallbackState(callback, completionSource));

        await completionSource.Task;
    }

    private sealed class PostedCallbackState(Func<Task> callback, TaskCompletionSource<object?> completionSource)
    {
        public async Task ExecuteAsync()
        {
            try
            {
                await callback();
                completionSource.TrySetResult(null);
            }
            catch (Exception exception)
            {
                completionSource.TrySetException(exception);
            }
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
