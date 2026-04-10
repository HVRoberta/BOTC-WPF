using System.Net;
using BOTC.Contracts.Rooms;
using BOTC.Presentation.Desktop.Navigation;
using BOTC.Presentation.Desktop.Rooms;
using BOTC.Presentation.Desktop.Rooms.RoomLobby;
using BOTC.Presentation.Desktop.Rooms.Shared;
using BOTC.Presentation.Desktop.Session;

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
        var playerId = Guid.NewGuid().ToString();
        var sessionService = new FakeClientSessionService();
        sessionService.SetSession("AB12CD", playerId, "Host");
        var viewModel = CreateViewModel(apiClient, realtimeClient, navigationService, sessionService);

        await viewModel.ActivateAsync(CancellationToken.None);
        await realtimeClient.RaiseLobbyUpdatedAsync("AB12CD");

        Assert.Equal(2, apiClient.GetRoomLobbyCallCount);
        Assert.Equal(2, viewModel.Players.Count);
        Assert.Equal("Alice", viewModel.Players[1].DisplayName);
        Assert.Equal("AB12CD", realtimeClient.SubscribedRoomCodes.Single());
        Assert.True(viewModel.HasRealtimeSubscription);
        Assert.Equal("Connected", viewModel.RealtimeStateText);
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
        var playerId = Guid.NewGuid().ToString();
        var sessionService = new FakeClientSessionService();
        sessionService.SetSession("AB12CD", playerId, "Host");
        var viewModel = CreateViewModel(apiClient, realtimeClient, navigationService, sessionService);

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
        var playerId = Guid.NewGuid().ToString();
        var sessionService = new FakeClientSessionService();
        sessionService.SetSession("AB12CD", playerId, "Host");
        var viewModel = CreateViewModel(apiClient, realtimeClient, navigationService, sessionService);

        await viewModel.ActivateAsync(CancellationToken.None);
        await realtimeClient.RaiseLobbyUpdatedAsync("AB12CD");

        Assert.Single(viewModel.Players);
        Assert.Equal("Host", viewModel.Players[0].DisplayName);
        Assert.Equal("Refresh failed because the room was not found. Showing the last loaded lobby snapshot.", viewModel.ErrorMessage);
        Assert.Equal(ScreenMessageKind.Error, viewModel.ScreenMessageKind);
        Assert.True(viewModel.HasScreenMessage);
    }

    [Fact]
    public async Task ToggleReady_WhenExecuted_UpdatesReadyStateAndRefreshesLobby()
    {
        const string roomCode = "AB12CD";
        const string hostPlayerId = "host-1";
        const string currentPlayerId = "player-1";
        var apiClient = new FakeRoomsApiClient(
            [
                Task.FromResult(CreateLobbyResponse(
                    roomCode,
                    [
                        CreateLobbyPlayer(hostPlayerId, "Host", isHost: true, isReady: false),
                        CreateLobbyPlayer(currentPlayerId, "Alice", isHost: false, isReady: false)
                    ])),
                Task.FromResult(CreateLobbyResponse(
                    roomCode,
                    [
                        CreateLobbyPlayer(hostPlayerId, "Host", isHost: true, isReady: false),
                        CreateLobbyPlayer(currentPlayerId, "Alice", isHost: false, isReady: true)
                    ]))
            ],
            setPlayerReadyResponse: new SetPlayerReadyResponse(roomCode, currentPlayerId, true));
        var realtimeClient = new FakeRoomLobbyRealtimeClient();
        var navigationService = new FakeNavigationService();
        var sessionService = new FakeClientSessionService();
        sessionService.SetSession(roomCode, currentPlayerId, "Alice");
        var viewModel = CreateViewModel(apiClient, realtimeClient, navigationService, sessionService);

        await viewModel.ActivateAsync(CancellationToken.None);
        await viewModel.ToggleReadyCommand.ExecuteAsync(null);

        Assert.Equal(roomCode, apiClient.LastSetReadyRoomCode);
        Assert.NotNull(apiClient.LastSetReadyRequest);
        Assert.Equal(currentPlayerId, apiClient.LastSetReadyRequest!.PlayerId);
        Assert.True(apiClient.LastSetReadyRequest.IsReady);
        Assert.Equal(2, apiClient.GetRoomLobbyCallCount);
        Assert.True(viewModel.IsCurrentUserReady);
        Assert.Equal("1/1 players ready", viewModel.NonHostReadinessSummary);
    }

    [Fact]
    public async Task ToggleReady_WhenCurrentUserIsHost_CommandIsDisabledAndHostSummaryIsShown()
    {
        const string roomCode = "AB12CD";
        const string hostPlayerId = "host-1";
        var apiClient = new FakeRoomsApiClient(
            [
                Task.FromResult(CreateLobbyResponse(
                    roomCode,
                    [
                        CreateLobbyPlayer(hostPlayerId, "Host", isHost: true, isReady: false),
                        CreateLobbyPlayer("player-1", "Alice", isHost: false, isReady: true)
                    ]))
            ]);
        var realtimeClient = new FakeRoomLobbyRealtimeClient();
        var navigationService = new FakeNavigationService();
        var sessionService = new FakeClientSessionService();
        sessionService.SetSession(roomCode, hostPlayerId, "Host");
        var viewModel = CreateViewModel(apiClient, realtimeClient, navigationService, sessionService);

        await viewModel.ActivateAsync(CancellationToken.None);

        Assert.False(viewModel.ToggleReadyCommand.CanExecute(null));
        Assert.Equal("Host does not use ready state", viewModel.CurrentUserReadyStateText);
    }

    [Fact]
    public async Task StartGame_WhenCurrentUserIsNotHost_CommandIsDisabled()
    {
        const string roomCode = "AB12CD";
        const string hostPlayerId = "host-1";
        const string currentPlayerId = "player-1";
        var apiClient = new FakeRoomsApiClient(
        [
            Task.FromResult(CreateLobbyResponse(
                roomCode,
                [
                    CreateLobbyPlayer(hostPlayerId, "Host", isHost: true, isReady: false),
                    CreateLobbyPlayer(currentPlayerId, "Alice", isHost: false, isReady: true)
                ]))
        ]);
        var realtimeClient = new FakeRoomLobbyRealtimeClient();
        var navigationService = new FakeNavigationService();
        var sessionService = new FakeClientSessionService();
        sessionService.SetSession(roomCode, currentPlayerId, "Alice");
        var viewModel = CreateViewModel(apiClient, realtimeClient, navigationService, sessionService);

        await viewModel.ActivateAsync(CancellationToken.None);

        Assert.False(viewModel.StartGameCommand.CanExecute(null));
    }

    [Fact]
    public async Task StartGame_WhenEligibleAndHostedByCurrentUser_CallsApiAndRefreshesLobby()
    {
        const string roomCode = "AB12CD";
        const string hostPlayerId = "host-1";
        var apiClient = new FakeRoomsApiClient(
            [
                Task.FromResult(CreateLobbyResponse(
                    roomCode,
                    [
                        CreateLobbyPlayer(hostPlayerId, "Host", isHost: true, isReady: false),
                        CreateLobbyPlayer("player-1", "Alice", isHost: false, isReady: true)
                    ])),
                Task.FromResult(CreateLobbyResponse(
                    roomCode,
                    [
                        CreateLobbyPlayer(hostPlayerId, "Host", isHost: true, isReady: false),
                        CreateLobbyPlayer("player-1", "Alice", isHost: false, isReady: true)
                    ],
                    RoomStatusContract.InProgress))
            ],
            startGameResponse: new StartGameResponse(
                roomCode,
                hostPlayerId,
                true,
                null,
                RoomStatusContract.InProgress));
        var realtimeClient = new FakeRoomLobbyRealtimeClient();
        var navigationService = new FakeNavigationService();
        var sessionService = new FakeClientSessionService();
        sessionService.SetSession(roomCode, hostPlayerId, "Host");
        var viewModel = CreateViewModel(apiClient, realtimeClient, navigationService, sessionService);

        await viewModel.ActivateAsync(CancellationToken.None);
        await viewModel.StartGameCommand.ExecuteAsync(null);

        Assert.Equal(roomCode, apiClient.LastStartGameRoomCode);
        Assert.NotNull(apiClient.LastStartGameRequest);
        Assert.Equal(hostPlayerId, apiClient.LastStartGameRequest!.StarterPlayerId);
        Assert.Equal(1, apiClient.StartGameCallCount);
        Assert.Equal(2, apiClient.GetRoomLobbyCallCount);
        Assert.Equal("In progress", viewModel.RoomStatus);
    }

    [Fact]
    public async Task StartGame_WhenServerReturnsBlockedReason_ShowsUserFriendlyMessageAndRefreshes()
    {
        const string roomCode = "AB12CD";
        const string hostPlayerId = "host-1";
        var apiClient = new FakeRoomsApiClient(
            [
                Task.FromResult(CreateLobbyResponse(
                    roomCode,
                    [
                        CreateLobbyPlayer(hostPlayerId, "Host", isHost: true, isReady: false),
                        CreateLobbyPlayer("player-1", "Alice", isHost: false, isReady: true)
                    ])),
                Task.FromResult(CreateLobbyResponse(
                    roomCode,
                    [
                        CreateLobbyPlayer(hostPlayerId, "Host", isHost: true, isReady: false),
                        CreateLobbyPlayer("player-1", "Alice", isHost: false, isReady: false)
                    ]))
            ],
            startGameResponse: new StartGameResponse(
                roomCode,
                hostPlayerId,
                false,
                StartGameBlockedReasonContract.NonHostPlayersNotReady,
                RoomStatusContract.WaitingForPlayers));
        var realtimeClient = new FakeRoomLobbyRealtimeClient();
        var navigationService = new FakeNavigationService();
        var sessionService = new FakeClientSessionService();
        sessionService.SetSession(roomCode, hostPlayerId, "Host");
        var viewModel = CreateViewModel(apiClient, realtimeClient, navigationService, sessionService);

        await viewModel.ActivateAsync(CancellationToken.None);
        await viewModel.StartGameCommand.ExecuteAsync(null);

        Assert.Equal(1, apiClient.StartGameCallCount);
        Assert.Equal(2, apiClient.GetRoomLobbyCallCount);
        Assert.Equal("Unable to start because not all non-host players are ready.", viewModel.ErrorMessage);
        Assert.Equal(ScreenMessageKind.Error, viewModel.ScreenMessageKind);
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
        var sessionService = new FakeClientSessionService();
        sessionService.SetSession("AB12CD", playerId, "Alice");
        var viewModel = CreateViewModel(apiClient, realtimeClient, navigationService, sessionService);

        await viewModel.ActivateAsync(CancellationToken.None);
        await viewModel.LeaveRoomCommand.ExecuteAsync(null);

        Assert.Equal("AB12CD", apiClient.LastLeaveRoomCode);
        Assert.Equal(playerId, apiClient.LastLeaveRequest!.PlayerId);
        Assert.Equal(1, navigationService.NavigateToCreateRoomCallCount);
        Assert.Equal("You left room AB12CD successfully.", viewModel.ErrorMessage);
        Assert.Equal(ScreenMessageKind.Info, viewModel.ScreenMessageKind);
        Assert.False(sessionService.HasActiveSession);
        Assert.Empty(viewModel.Players);
        Assert.Equal(["AB12CD"], realtimeClient.UnsubscribedRoomCodes);
    }

    [Fact]
    public async Task LeaveRoom_WhenRoomIsRemoved_PreservesExistingInfoMessage()
    {
        var playerId = Guid.NewGuid().ToString();
        var apiClient = new FakeRoomsApiClient(
            [Task.FromResult(CreateLobbyResponse("AB12CD", ["Host"]))],
            leaveRoomResponse: new LeaveRoomResponse("AB12CD", playerId, true, null));
        var realtimeClient = new FakeRoomLobbyRealtimeClient();
        var navigationService = new FakeNavigationService();
        var sessionService = new FakeClientSessionService();
        sessionService.SetSession("AB12CD", playerId, "Host");
        var viewModel = CreateViewModel(apiClient, realtimeClient, navigationService, sessionService);

        await viewModel.ActivateAsync(CancellationToken.None);
        await viewModel.LeaveRoomCommand.ExecuteAsync(null);

        Assert.Equal(1, navigationService.NavigateToCreateRoomCallCount);
        Assert.Equal("The room was closed while you were leaving. Returned to room setup.", viewModel.ErrorMessage);
        Assert.Equal(ScreenMessageKind.Info, viewModel.ScreenMessageKind);
        Assert.False(sessionService.HasActiveSession);
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
        var playerId = Guid.NewGuid().ToString();
        var sessionService = new FakeClientSessionService();
        sessionService.SetSession("AB12CD", playerId, "Host");
        var viewModel = CreateViewModel(apiClient, realtimeClient, navigationService, sessionService);

        await viewModel.ActivateAsync(CancellationToken.None);
        await realtimeClient.RaiseLobbyClosedAsync("AB12CD");

        Assert.Equal(1, navigationService.NavigateToCreateRoomCallCount);
        Assert.Equal("The room AB12CD was closed by the host. Returned to room setup.", viewModel.ErrorMessage);
        Assert.Equal(ScreenMessageKind.Info, viewModel.ScreenMessageKind);
        Assert.Empty(viewModel.Players);
        Assert.Equal(["AB12CD"], realtimeClient.UnsubscribedRoomCodes);
    }

    private static RoomLobbyViewModel CreateViewModel(
        FakeRoomsApiClient apiClient,
        FakeRoomLobbyRealtimeClient realtimeClient,
        FakeNavigationService navigationService,
        IClientSessionService clientSessionService)
    {
        return new RoomLobbyViewModel(apiClient, realtimeClient, clientSessionService, navigationService);
    }

    private static GetRoomLobbyResponse CreateLobbyResponse(string roomCode, IReadOnlyList<string> players)
    {
        return new GetRoomLobbyResponse(
            roomCode,
            players.Select((playerName, index) => new LobbyPlayerResponse(index.ToString(), playerName, index == 0, false)).ToArray(),
            RoomStatusContract.WaitingForPlayers);
    }

    private static GetRoomLobbyResponse CreateLobbyResponse(
        string roomCode,
        IReadOnlyList<LobbyPlayerResponse> players,
        RoomStatusContract status = RoomStatusContract.WaitingForPlayers)
    {
        return new GetRoomLobbyResponse(roomCode, players, status);
    }

    private static LobbyPlayerResponse CreateLobbyPlayer(string playerId, string displayName, bool isHost, bool isReady)
    {
        return new LobbyPlayerResponse(playerId, displayName, isHost, isReady);
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
        private readonly SetPlayerReadyResponse _setPlayerReadyResponse;
        private readonly StartGameResponse _startGameResponse;

        public FakeRoomsApiClient(
            IEnumerable<Task<GetRoomLobbyResponse>> lobbyResponses,
            LeaveRoomResponse? leaveRoomResponse = null,
            SetPlayerReadyResponse? setPlayerReadyResponse = null,
            StartGameResponse? startGameResponse = null)
        {
            _lobbyResponses = new Queue<Task<GetRoomLobbyResponse>>(lobbyResponses);
            _leaveRoomResponse = leaveRoomResponse ?? new LeaveRoomResponse("AB12CD", Guid.NewGuid().ToString(), false, null);
            _setPlayerReadyResponse = setPlayerReadyResponse ?? new SetPlayerReadyResponse("AB12CD", Guid.NewGuid().ToString(), true);
            _startGameResponse = startGameResponse ?? new StartGameResponse(
                "AB12CD",
                Guid.NewGuid().ToString(),
                true,
                null,
                RoomStatusContract.InProgress);
        }

        public int GetRoomLobbyCallCount { get; private set; }

        public string? LastLeaveRoomCode { get; private set; }

        public LeaveRoomRequest? LastLeaveRequest { get; private set; }

        public int StartGameCallCount { get; private set; }

        public string? LastSetReadyRoomCode { get; private set; }

        public SetPlayerReadyRequest? LastSetReadyRequest { get; private set; }

        public string? LastStartGameRoomCode { get; private set; }

        public StartGameRequest? LastStartGameRequest { get; private set; }

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

        public Task<SetPlayerReadyResponse> SetPlayerReadyAsync(string roomCode, SetPlayerReadyRequest request, CancellationToken cancellationToken)
        {
            LastSetReadyRoomCode = roomCode;
            LastSetReadyRequest = request;
            return Task.FromResult(_setPlayerReadyResponse);
        }

        public Task<StartGameResponse> StartGameAsync(string roomCode, StartGameRequest request, CancellationToken cancellationToken)
        {
            StartGameCallCount++;
            LastStartGameRoomCode = roomCode;
            LastStartGameRequest = request;
            return Task.FromResult(_startGameResponse);
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
        public event Action<RealtimeConnectionState>? ConnectionStateChanged;

        public List<string> SubscribedRoomCodes { get; } = [];
        public List<string> UnsubscribedRoomCodes { get; } = [];

        public RealtimeConnectionState ConnectionState { get; private set; } = RealtimeConnectionState.Disconnected;

        public Task SubscribeAsync(string roomCode, CancellationToken cancellationToken)
        {
            SubscribedRoomCodes.Add(roomCode);
            ConnectionState = RealtimeConnectionState.Connected;
            ConnectionStateChanged?.Invoke(ConnectionState);
            return Task.CompletedTask;
        }

        public Task UnsubscribeAsync(string roomCode, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(roomCode))
            {
                UnsubscribedRoomCodes.Add(roomCode);
            }

            ConnectionState = RealtimeConnectionState.Disconnected;
            ConnectionStateChanged?.Invoke(ConnectionState);

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

        public void NavigateToEntry()
        {
        }

        public void NavigateToCreateRoom()
        {
            NavigateToCreateRoomCallCount++;
        }

        public void NavigateToJoinRoom()
        {
        }

        public void NavigateToRoomLobby()
        {
        }
    }

    private sealed class FakeClientSessionService : IClientSessionService
    {
        public string? CurrentRoomCode { get; private set; }

        public string? CurrentPlayerId { get; private set; }

        public string? DisplayName { get; private set; }

        public bool HasActiveSession =>
            !string.IsNullOrWhiteSpace(CurrentRoomCode)
            && !string.IsNullOrWhiteSpace(CurrentPlayerId);

        public void SetDisplayName(string displayName)
        {
            DisplayName = displayName;
        }

        public void SetSession(string roomCode, string playerId, string displayName)
        {
            CurrentRoomCode = roomCode;
            CurrentPlayerId = playerId;
            DisplayName = displayName;
        }

        public void ClearSession()
        {
            CurrentRoomCode = null;
            CurrentPlayerId = null;
            DisplayName = null;
        }
    }
}
