using BOTC.Presentation.Desktop.Entry;
using BOTC.Presentation.Desktop.Navigation;
using BOTC.Presentation.Desktop.Session;

namespace BOTC.Presentation.Desktop.Tests.Entry;

public sealed class EntryViewModelTests
{
    [Fact]
    public void UserName_WhenMissing_ShowsValidationAndDisablesCommands()
    {
        var viewModel = new EntryViewModel(new FakeNavigationService(), new FakeClientSessionService())
        {
            UserName = "   "
        };

        Assert.Equal("User name is required.", viewModel.ValidationMessage);
        Assert.True(viewModel.HasValidationMessage);
        Assert.False(viewModel.HostGameCommand.CanExecute(null));
        Assert.False(viewModel.JoinGameCommand.CanExecute(null));
    }

    [Fact]
    public void UserName_WhenValid_ClearsValidationAndEnablesCommands()
    {
        var viewModel = new EntryViewModel(new FakeNavigationService(), new FakeClientSessionService())
        {
            UserName = "  Alice  "
        };

        Assert.False(viewModel.HasValidationMessage);
        Assert.Equal(string.Empty, viewModel.ValidationMessage);
        Assert.True(viewModel.HostGameCommand.CanExecute(null));
        Assert.True(viewModel.JoinGameCommand.CanExecute(null));
    }

    [Fact]
    public void HostGame_WhenExecuted_StoresTrimmedDisplayNameAndNavigatesToCreateRoom()
    {
        var navigationService = new FakeNavigationService();
        var sessionService = new FakeClientSessionService();
        var viewModel = new EntryViewModel(navigationService, sessionService)
        {
            UserName = "  Alice  "
        };

        viewModel.HostGameCommand.Execute(null);

        Assert.Equal("Alice", sessionService.DisplayName);
        Assert.Equal(1, navigationService.NavigateToCreateRoomCallCount);
        Assert.Equal(0, navigationService.NavigateToJoinRoomCallCount);
    }

    [Fact]
    public void JoinGame_WhenExecuted_StoresTrimmedDisplayNameAndNavigatesToJoinRoom()
    {
        var navigationService = new FakeNavigationService();
        var sessionService = new FakeClientSessionService();
        var viewModel = new EntryViewModel(navigationService, sessionService)
        {
            UserName = "  Bob  "
        };

        viewModel.JoinGameCommand.Execute(null);

        Assert.Equal("Bob", sessionService.DisplayName);
        Assert.Equal(1, navigationService.NavigateToJoinRoomCallCount);
        Assert.Equal(0, navigationService.NavigateToCreateRoomCallCount);
    }

    private sealed class FakeNavigationService : INavigationService
    {
        public int NavigateToCreateRoomCallCount { get; private set; }

        public int NavigateToJoinRoomCallCount { get; private set; }

        public void NavigateToCreateRoom()
        {
            NavigateToCreateRoomCallCount++;
        }

        public void NavigateToJoinRoom()
        {
            NavigateToJoinRoomCallCount++;
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
            !string.IsNullOrWhiteSpace(CurrentRoomCode) &&
            !string.IsNullOrWhiteSpace(CurrentPlayerId);

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

