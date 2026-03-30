using System.Net;
using BOTC.Contracts.Rooms;
using BOTC.Presentation.Desktop.Navigation;
using BOTC.Presentation.Desktop.Rooms;
using BOTC.Presentation.Desktop.Rooms.RoomLobby;

namespace BOTC.Presentation.Desktop.Tests.Rooms.RoomLobby;

public sealed class RoomLobbyViewModelTests
{
    [Fact]
    public async Task LobbyUpdated_WhenLobbyIsActive_ReloadsLobbyStateFromHttp()
    {
        var apiClient = new FakeRoomsApiClient([
            Task.FromResult(CreateLobbyResponse("AB12CD", ["Host"])),
            Task.FromResult(CreateLobbyResponse("AB12CD", ["Host", "Alice"]))
        ]);
        var realtimeClient = new FakeRoomLobbyRealtimeClient();
        var navigationService = new FakeNavigationService();
        var viewModel = CreateViewModel(apiClient, realtimeClient, navigationService);

        await viewModel.LoadAsync("AB12CD", Guid.NewGuid().ToString(), CancellationToken.None);
        await viewModel.ActivateAsync(CancellationToken.None);
        await realtimeClient.RaiseLobbyUpdatedAsync("AB12CD");

        Assert.Equal(2, apiClient.GetRoomLobbyCallCount);
        Assert.Equal(2, viewModel.Players.Count);
        Assert.Equal("Alice", viewModel.Players[1].DisplayName);
        Assert.Equal("AB12CD", realtimeClient.SubscribedRoomCodes.Single());
    }

    [Fact]
    public async Task LobbyUpdated_BurstWhileRefreshIsInFlight_CoalescesToSingleTrailingReload()
    {
        var refreshStarted = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var allowRefreshToComplete = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var apiClient = new FakeRoomsApiClient([
            Task.FromResult(CreateLobbyResponse("AB12CD", ["Host"])),
            WaitForRefreshAsync(refreshStarted, allowRefreshToComplete, CreateLobbyResponse("AB12CD", ["Host", "Alice"])),
            Task.FromResult(CreateLobbyResponse("AB12CD", ["Host", "Alice", "Bob"]))
        ]);
        var realtimeClient = new FakeRoomLobbyRealtimeClient();
        var navigationService = new FakeNavigationService();
        var viewModel = CreateViewModel(apiClient, realtimeClient, navigationService);

        await viewModel.LoadAsync("AB12CD", Guid.NewGuid().ToString(), CancellationToken.None);
        await viewModel.ActivateAsync(CancellationToken.None);

        var firstUpdateTask = realtimeClient.RaiseLobbyUpdatedAsync("AB12CD");
        await refreshStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        var secondUpdateTask = realtimeClient.RaiseLobbyUpdatedAsync("AB12CD");
        var thirdUpdateTask = realtimeClient.RaiseLobbyUpdatedAsync("AB12CD");

        allowRefreshToComplete.TrySetResult(null);

        await Task.WhenAll(firstUpdateTask, secondUpdateTask, thirdUpdateTask).WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(3, apiClient.GetRoomLobbyCallCount);
        Assert.Equal(3, viewModel.Players.Count);
        Assert.Equal("Bob", viewModel.Players[^1].DisplayName);
    }

    [Fact]
    public async Task LobbyUpdated_WhenReloadFails_PreservesLastSuccessfulLobbyState()
    {
        var apiClient = new FakeRoomsApiClient([
            Task.FromResult(CreateLobbyResponse("AB12CD", ["Host"])),
            Task.FromException<GetRoomLobbyResponse>(new HttpRequestException("Missing room.", null, HttpStatusCode.NotFound))
        ]);
        var realtimeClient = new FakeRoomLobbyRealtimeClient();
        var navigationService = new FakeNavigationService();
        var viewModel = CreateViewModel(apiClient, realtimeClient, navigationService);

        await viewModel.LoadAsync("AB12CD", Guid.NewGuid().ToString(), CancellationToken.None);
        await viewModel.ActivateAsync(CancellationToken.None);
        await realtimeClient.RaiseLobbyUpdatedAsync("AB12CD");

        Assert.Single(viewModel.Players);
        Assert.Equal("Host", viewModel.Players[0].DisplayName);
        Assert.Equal("Room was not found. Showing the last loaded data.", viewModel.ErrorMessage);
    }

    [Fact]
    public async Task LeaveRoom_WhenLeaveSucceeds_CallsApiWithCurrentPlayerIdAndNavigatesAway()
    {
        var playerId = Guid.NewGuid().ToString();
        var apiClient = new FakeRoomsApiClient(
            [Task.FromResult(CreateLobbyResponse("AB12CD", ["Host", "Alice"]))],
            leaveRoomResponse: new LeaveRoomResponse("AB12CD", playerId, false, null));
        var realtimeClient = new FakeRoomLobbyRealtimeClient();
        var navigationService = new FakeNavigationService();
        var viewModel = CreateViewModel(apiClient, realtimeClient, navigationService);

        await viewModel.LoadAsync("AB12CD", playerId, CancellationToken.None);
        await viewModel.ActivateAsync(CancellationToken.None);
        await viewModel.LeaveRoomCommand.ExecuteAsync(null);

        Assert.Equal("AB12CD", apiClient.LastLeaveRoomCode);
        Assert.Equal(playerId, apiClient.LastLeaveRequest!.PlayerId);
        Assert.Equal(1, navigationService.NavigateToCreateRoomCallCount);
        Assert.Equal("You left the room.", viewModel.ErrorMessage);
        Assert.Empty(viewModel.Players);
        Assert.Equal(["AB12CD"], realtimeClient.UnsubscribedRoomCodes);
    }

    [Fact]
    public async Task LobbyClosed_WhenCurrentRoomIsClosed_ResetsLobbyAndNavigatesAway()
    {
        var apiClient = new FakeRoomsApiClient([
            Task.FromResult(CreateLobbyResponse("AB12CD", ["Host", "Alice"]))
        ]);
        var realtimeClient = new FakeRoomLobbyRealtimeClient();
        var navigationService = new FakeNavigationService();
        var viewModel = CreateViewModel(apiClient, realtimeClient, navigationService);

        await viewModel.LoadAsync("AB12CD", Guid.NewGuid().ToString(), CancellationToken.None);
        await viewModel.ActivateAsync(CancellationToken.None);
        await realtimeClient.RaiseLobbyClosedAsync("AB12CD");

        Assert.Equal(1, navigationService.NavigateToCreateRoomCallCount);
        Assert.Equal("The room was closed.", viewModel.ErrorMessage);
        Assert.Empty(viewModel.Players);
        Assert.Equal(["AB12CD"], realtimeClient.UnsubscribedRoomCodes);
    }

    private static RoomLobbyViewModel CreateViewModel(
        FakeRoomsApiClient apiClient,
        FakeRoomLobbyRealtimeClient realtimeClient,
        FakeNavigationService navigationService)
    {
        return new RoomLobbyViewModel(apiClient, realtimeClient, navigationService);
    }

    private static GetRoomLobbyResponse CreateLobbyResponse(string roomCode, IReadOnlyList<string> players)
    {
        return new GetRoomLobbyResponse(
            roomCode,
            players.Select((playerName, index) => new LobbyPlayerResponse(index.ToString(), playerName, index == 0)).ToArray(),
            RoomStatusContract.WaitingForPlayers);
    }

    private static async Task<GetRoomLobbyResponse> WaitForRefreshAsync(
        TaskCompletionSource<object?> refreshStarted,
        TaskCompletionSource<object?> allowRefreshToComplete,
        GetRoomLobbyResponse response)
    {
        refreshStarted.TrySetResult(null);
        await allowRefreshToComplete.Task;
        return response;
    }

    private sealed class FakeRoomsApiClient : IRoomsApiClient
    {
        private readonly Queue<Task<GetRoomLobbyResponse>> _lobbyResponses;
        private readonly LeaveRoomResponse _leaveRoomResponse;

        public FakeRoomsApiClient(
            IEnumerable<Task<GetRoomLobbyResponse>> lobbyResponses,
            LeaveRoomResponse? leaveRoomResponse = null)
        {
            _lobbyResponses = new Queue<Task<GetRoomLobbyResponse>>(lobbyResponses);
            _leaveRoomResponse = leaveRoomResponse ?? new LeaveRoomResponse("AB12CD", Guid.NewGuid().ToString(), false, null);
        }

        public int GetRoomLobbyCallCount { get; private set; }

        public string? LastLeaveRoomCode { get; private set; }

        public LeaveRoomRequest? LastLeaveRequest { get; private set; }

        public Task<CreateRoomResponse> CreateRoomAsync(CreateRoomRequest request, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<JoinRoomResponse> JoinRoomAsync(string roomCode, JoinRoomRequest request, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<LeaveRoomResponse> LeaveRoomAsync(string roomCode, LeaveRoomRequest request, CancellationToken cancellationToken)
        {
            LastLeaveRoomCode = roomCode;
            LastLeaveRequest = request;
            return Task.FromResult(_leaveRoomResponse);
        }

        public Task<GetRoomLobbyResponse> GetRoomLobbyAsync(string roomCode, CancellationToken cancellationToken)
        {
            GetRoomLobbyCallCount++;
            if (_lobbyResponses.Count == 0)
            {
                throw new InvalidOperationException("No more lobby responses configured for test.");
            }

            return _lobbyResponses.Dequeue();
        }
    }

    private sealed class FakeRoomLobbyRealtimeClient : IRoomLobbyRealtimeClient
    {
        public event Func<string, Task>? LobbyUpdated;
        public event Func<string, Task>? LobbyClosed;

        public List<string> SubscribedRoomCodes { get; } = [];
        public List<string> UnsubscribedRoomCodes { get; } = [];

        public Task SubscribeAsync(string roomCode, CancellationToken cancellationToken)
        {
            SubscribedRoomCodes.Add(roomCode);
            return Task.CompletedTask;
        }

        public Task UnsubscribeAsync(string roomCode, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(roomCode))
            {
                UnsubscribedRoomCodes.Add(roomCode);
            }

            return Task.CompletedTask;
        }

        public async Task RaiseLobbyUpdatedAsync(string roomCode)
        {
            var handler = LobbyUpdated;
            if (handler is null)
            {
                return;
            }

            foreach (var callback in handler.GetInvocationList().Cast<Func<string, Task>>())
            {
                await callback(roomCode);
            }
        }

        public async Task RaiseLobbyClosedAsync(string roomCode)
        {
            var handler = LobbyClosed;
            if (handler is null)
            {
                return;
            }

            foreach (var callback in handler.GetInvocationList().Cast<Func<string, Task>>())
            {
                await callback(roomCode);
            }
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }

    private sealed class FakeNavigationService : INavigationService
    {
        public int NavigateToCreateRoomCallCount { get; private set; }

        public void NavigateToCreateRoom()
        {
            NavigateToCreateRoomCallCount++;
        }

        public void NavigateToJoinRoom()
        {
        }

        public Task NavigateToRoomLobbyAsync(string roomCode, string playerId, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
