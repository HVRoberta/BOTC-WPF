using BOTC.Presentation.Desktop.Navigation;
using BOTC.Presentation.Desktop.Session;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BOTC.Presentation.Desktop.Entry;

public partial class EntryViewModel(
    INavigationService navigationService,
    IClientSessionService clientSessionService) : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasValidationMessage))]
    [NotifyCanExecuteChangedFor(nameof(HostGameCommand))]
    [NotifyCanExecuteChangedFor(nameof(JoinGameCommand))]
    private string _username = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasValidationMessage))]
    private string _validationMessage = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public bool HasValidationMessage => !string.IsNullOrWhiteSpace(ValidationMessage);

    partial void OnUsernameChanged(string value)
    {
        ValidateUsername(value);
        StatusMessage = string.Empty;
    }

    private bool CanStartFlow() => IsValidUsername(Username);

    [RelayCommand(CanExecute = nameof(CanStartFlow))]
    private void HostGame()
    {
        if (!TryPrepareUsername(out var normalizedUsername))
        {
            return;
        }

        clientSessionService.SetName(normalizedUsername);
        StatusMessage = "Starting host flow...";
        navigationService.NavigateToCreateRoom();
    }

    [RelayCommand(CanExecute = nameof(CanStartFlow))]
    private void JoinGame()
    {
        if (!TryPrepareUsername(out var normalizedUsername))
        {
            return;
        }

        clientSessionService.SetName(normalizedUsername);
        StatusMessage = "Starting join flow...";
        navigationService.NavigateToJoinRoom();
    }

    private bool TryPrepareUsername(out string normalizedUsername)
    {
        normalizedUsername = Username.Trim();
        if (!IsValidUsername(normalizedUsername))
        {
            ValidateUsername(normalizedUsername);
            return false;
        }

        ValidationMessage = string.Empty;
        return true;
    }

    private void ValidateUsername(string username)
    {
        var normalizedUsername = username.Trim();

        if (normalizedUsername.Length == 0)
        {
            ValidationMessage = "User name is required.";
            return;
        }

        if (normalizedUsername.Length < 2)
        {
            ValidationMessage = "User name must be at least 2 characters.";
            return;
        }

        ValidationMessage = string.Empty;
    }

    private static bool IsValidUsername(string username)
    {
        return !string.IsNullOrWhiteSpace(username) && username.Trim().Length >= 2;
    }
}